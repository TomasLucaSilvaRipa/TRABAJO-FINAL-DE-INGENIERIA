using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using TeamBalance.BE.Entidades;
using TeamBalance.MPP;
using TeamBalance.Services;

namespace TeamBalance.BLL;

public class BLLCorreoOperativo
{
    private readonly MPPCorreoOperativo _correoMPP;
    private readonly MPPUsuario _usuarioMPP;
    private readonly BLLRol _rolBLL;
    private readonly BLLTarea _tareaBLL;
    private readonly BLLBitacora _bitacoraBLL;
    private readonly EmailService _emailService;

    public BLLCorreoOperativo(MPPCorreoOperativo correoMPP, MPPUsuario usuarioMPP, BLLRol rolBLL, BLLTarea tareaBLL, BLLBitacora bitacoraBLL, EmailService emailService)
    {
        _correoMPP = correoMPP; _usuarioMPP = usuarioMPP; _rolBLL = rolBLL; _tareaBLL = tareaBLL; _bitacoraBLL = bitacoraBLL; _emailService = emailService;
    }

    public PreferenciaCorreoOperativo ConsultarPreferencia(Usuario usuario) => _correoMPP.ConsultarPreferencia(usuario);

    public PreferenciaCorreoOperativo GuardarPreferencia(PreferenciaCorreoOperativo preferencia, Usuario usuario)
    {
        if (preferencia.HoraResumenDiario < TimeSpan.Zero || preferencia.HoraResumenDiario >= TimeSpan.FromDays(1)) throw new ArgumentException("Indicá una hora válida para el resumen diario.");
        preferencia.IdUsuario = usuario.ID;
        _correoMPP.GuardarPreferencia(preferencia);
        _bitacoraBLL.Add(new Bitacora(usuario.ID, usuario.IdAgencia, "PreferenciaCorreoOperativo", usuario.ID, "ModificarPreferenciasCorreo", "Se actualizaron las preferencias de correos operativos.", "Exitoso", "Informacion", "Seguridad"));
        return _correoMPP.ConsultarPreferencia(usuario);
    }

    public AccionCorreoTarea ConsultarAccion(string token)
    {
        AccionCorreoTarea? accion = _correoMPP.ConsultarAccion(Hash(token));
        if (accion is null) throw new ArgumentException("El enlace no existe o ya no está disponible.");
        return accion;
    }

    public AccionCorreoTarea EjecutarAccion(string token)
    {
        string hash = Hash(token);
        AccionCorreoTarea accion = _correoMPP.ConsultarAccion(hash) ?? throw new ArgumentException("El enlace no existe o ya no está disponible.");
        if (!accion.Disponible || accion.FechaExpiracion < DateTime.Now) throw new ArgumentException("El enlace venció o ya fue utilizado.");
        Usuario usuario = _usuarioMPP.ConsultarUsuarioPorId(new Usuario { ID = accion.IdUsuario }) ?? throw new ArgumentException("La cuenta asociada al enlace ya no está disponible.");
        _rolBLL.CargarAutorizacion(usuario);
        _tareaBLL.CambiarEstadoPropio(new Tarea { ID = accion.IdTarea, Estado = accion.Accion }, usuario);
        if (!_correoMPP.MarcarAccionUsada(hash)) throw new InvalidOperationException("El estado se actualizó, pero no se pudo cerrar el enlace. Recargá la página antes de volver a intentarlo.");
        accion.Disponible = false;
        return accion;
    }

    public async Task<int> ProcesarPendientes()
    {
        DateTime ahora = HoraArgentina();
        int enviados = 0;
        foreach (DestinatarioCorreoOperativo destinatario in _correoMPP.ConsultarDestinatarios())
        {
            if (destinatario.ResumenDiarioActivo && string.Equals(destinatario.TipoUsuario, "Empleado", StringComparison.OrdinalIgnoreCase) && ahora.TimeOfDay >= destinatario.HoraResumenDiario)
            {
                enviados += await EnviarResumenDiario(destinatario, ahora);
            }
            if (destinatario.AlertasDeadlineActivo && string.Equals(destinatario.TipoUsuario, "Empleado", StringComparison.OrdinalIgnoreCase) && ahora.Hour >= 9)
            {
                enviados += await EnviarAlertasDeadline(destinatario, ahora);
            }
            if (destinatario.ResumenSemanalActivo && string.Equals(destinatario.TipoUsuario, "Dueno", StringComparison.OrdinalIgnoreCase) && ahora.DayOfWeek == DayOfWeek.Monday && ahora.Hour >= 9 && destinatario.IdAgencia.HasValue)
            {
                enviados += await EnviarResumenSemanal(destinatario, ahora);
            }
        }
        return enviados;
    }

    private async Task<int> EnviarResumenDiario(DestinatarioCorreoOperativo destinatario, DateTime ahora)
    {
        string clave = $"resumen-diario:{destinatario.IdUsuario}:{ahora:yyyyMMdd}";
        EnvioCorreoOperativo envio = new() { Clave = clave, Destinatario = destinatario, Tipo = "ResumenDiario" };
        if (!_correoMPP.ReservarEnvio(envio)) return 0;
        try
        {
            List<TareaResumenCorreo> tareas = _correoMPP.ConsultarTareas(destinatario);
            if (tareas.Count == 0) { envio.Enviado = true; _correoMPP.CompletarEnvio(envio); return 0; }
            foreach (TareaResumenCorreo tarea in tareas)
            {
                tarea.TokenEnProgreso = CrearToken(destinatario.IdUsuario, tarea.IdTarea, "En progreso", ahora.AddHours(36));
                tarea.TokenFinalizada = CrearToken(destinatario.IdUsuario, tarea.IdTarea, "Finalizada", ahora.AddHours(36));
            }
            bool enviado = await _emailService.EnviarResumenDiarioTareas(destinatario, tareas);
            envio.Enviado = enviado;
            envio.DetalleError = enviado ? null : "El proveedor SMTP no confirmó el envío.";
            _correoMPP.CompletarEnvio(envio);
            return enviado ? 1 : 0;
        }
        catch (Exception ex) { envio.Enviado = false; envio.DetalleError = ex.Message; _correoMPP.CompletarEnvio(envio); return 0; }
    }

    private async Task<int> EnviarAlertasDeadline(DestinatarioCorreoOperativo destinatario, DateTime ahora)
    {
        List<TareaResumenCorreo> tareas = _correoMPP.ConsultarTareas(destinatario)
            .Where(tarea => tarea.Deadline.HasValue && tarea.Deadline.Value.Date >= ahora.Date && tarea.Deadline.Value.Date <= ahora.Date.AddDays(2)).ToList();
        if (tareas.Count == 0) return 0;
        string clave = $"alerta-deadline:{destinatario.IdUsuario}:{ahora:yyyyMMdd}";
        EnvioCorreoOperativo envio = new() { Clave = clave, Destinatario = destinatario, Tipo = "AlertaDeadline" };
        if (!_correoMPP.ReservarEnvio(envio)) return 0;
        bool enviado = await _emailService.EnviarAlertaDeadline(destinatario, tareas);
        envio.Enviado = enviado;
        envio.DetalleError = enviado ? null : "El proveedor SMTP no confirmó el envío.";
        _correoMPP.CompletarEnvio(envio);
        return enviado ? 1 : 0;
    }

    private async Task<int> EnviarResumenSemanal(DestinatarioCorreoOperativo destinatario, DateTime ahora)
    {
        int semana = ISOWeek.GetWeekOfYear(ahora);
        string clave = $"resumen-semanal:{destinatario.IdUsuario}:{ISOWeek.GetYear(ahora)}-{semana:00}";
        EnvioCorreoOperativo envio = new() { Clave = clave, Destinatario = destinatario, Tipo = "ResumenSemanal" };
        if (!_correoMPP.ReservarEnvio(envio)) return 0;
        try
        {
            ResumenSemanalCorreo resumen = _correoMPP.ConsultarResumenSemanal(new Agencia { ID = destinatario.IdAgencia!.Value });
            bool enviado = await _emailService.EnviarResumenSemanalAgencia(destinatario, resumen);
            envio.Enviado = enviado;
            envio.DetalleError = enviado ? null : "El proveedor SMTP no confirmó el envío.";
            _correoMPP.CompletarEnvio(envio);
            return enviado ? 1 : 0;
        }
        catch (Exception ex) { envio.Enviado = false; envio.DetalleError = ex.Message; _correoMPP.CompletarEnvio(envio); return 0; }
    }

    private string CrearToken(int idUsuario, int idTarea, string accion, DateTime expiracion)
    {
        string token = Convert.ToHexString(RandomNumberGenerator.GetBytes(32)).ToLowerInvariant();
        _correoMPP.CrearToken(new AccionCorreoTarea
        {
            TokenHash = Hash(token),
            IdUsuario = idUsuario,
            IdTarea = idTarea,
            Accion = accion,
            FechaExpiracion = expiracion
        });
        return token;
    }

    private static string Hash(string token)
    {
        if (string.IsNullOrWhiteSpace(token)) throw new ArgumentException("El enlace no contiene un token válido.");
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token.Trim()))).ToLowerInvariant();
    }

    private static DateTime HoraArgentina()
    {
        foreach (string id in new[] { "America/Argentina/Buenos_Aires", "Argentina Standard Time" })
        {
            try { return TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, TimeZoneInfo.FindSystemTimeZoneById(id)); } catch (TimeZoneNotFoundException) { }
        }
        return DateTime.UtcNow.AddHours(-3);
    }
}
