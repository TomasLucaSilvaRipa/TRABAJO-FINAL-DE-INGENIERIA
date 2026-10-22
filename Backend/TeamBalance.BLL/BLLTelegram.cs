using System.Security.Cryptography;
using TeamBalance.BE.Entidades;
using TeamBalance.MPP;
using TeamBalance.Services;

namespace TeamBalance.BLL;

public class BLLTelegram
{
    private const string NombreBot = "TeamBalanceVerifyBot";
    private readonly MPPUsuario _usuarioMPP;
    private readonly Seguridad _seguridad;
    private readonly BLLBitacora _bitacoraBLL;

    public BLLTelegram(MPPUsuario usuarioMPP, Seguridad seguridad, BLLBitacora bitacoraBLL)
    {
        _usuarioMPP = usuarioMPP;
        _seguridad = seguridad;
        _bitacoraBLL = bitacoraBLL;
    }

    public EstadoTelegram ConsultarEstado(Usuario usuario)
    {
        return _usuarioMPP.ConsultarEstadoTelegram(usuario.ID);
    }

    public PreparacionTelegram PrepararVinculacion(Usuario usuario)
    {
        if (!string.Equals(usuario.Estado, "Activo", StringComparison.OrdinalIgnoreCase)) throw new InvalidOperationException("Confirmá tu correo electrónico antes de vincular Telegram.");
        string token = Convert.ToHexString(RandomNumberGenerator.GetBytes(24)).ToLowerInvariant();
        DateTime fechaExpiracion = DateTime.Now.AddMinutes(10);
        _usuarioMPP.CrearSolicitudTelegram(usuario.ID, _seguridad.GenerarHashToken(token), "Vinculacion", fechaExpiracion);
        _bitacoraBLL.Add(new Bitacora(usuario.ID, usuario.IdAgencia, "Usuario", usuario.ID, "PrepararVinculacionTelegram", "El usuario inició la vinculación de Telegram.", "Pendiente", "Informacion", "Seguridad"));
        return new PreparacionTelegram { EnlaceProfundo = $"https://t.me/{NombreBot}?start={token}", ComandoInicio = $"/start {token}", FechaExpiracion = fechaExpiracion };
    }

    public PreparacionTelegram PrepararValidacionRegistro(int idUsuario)
    {
        string token = Convert.ToHexString(RandomNumberGenerator.GetBytes(24)).ToLowerInvariant();
        DateTime fechaExpiracion = DateTime.Now.AddMinutes(10);
        _usuarioMPP.CrearSolicitudTelegram(idUsuario, _seguridad.GenerarHashToken(token), "Registro", fechaExpiracion);
        return new PreparacionTelegram { EnlaceProfundo = $"https://t.me/{NombreBot}?start={token}", ComandoInicio = $"/start {token}", FechaExpiracion = fechaExpiracion };
    }

    public EntregaTelegram ProcesarInicioBot(string token, long telegramUserId, long chatId, string? nombreUsuario)
    {
        if (string.IsNullOrWhiteSpace(token) || telegramUserId <= 0 || chatId <= 0) return new EntregaTelegram();
        string codigo = GenerarCodigo();
        return _usuarioMPP.ProcesarInicioTelegram(_seguridad.GenerarHashToken(token), telegramUserId, chatId, nombreUsuario, _seguridad.GenerarHashToken(codigo), DateTime.Now.AddMinutes(5), codigo);
    }

    public void ConfirmarVinculacion(Usuario usuario, string codigo)
    {
        if (!_usuarioMPP.ConfirmarCodigoTelegram(usuario.ID, "Vinculacion", _seguridad.GenerarHashToken(NormalizarCodigo(codigo)))) throw new UnauthorizedAccessException("El código no es válido, venció o ya fue utilizado.");
        _bitacoraBLL.Add(new Bitacora(usuario.ID, usuario.IdAgencia, "Usuario", usuario.ID, "VincularTelegram", "Se vinculó Telegram como canal de verificación.", "Exitoso", "Informacion", "Seguridad"));
    }

    public void ConfirmarValidacionRegistro(int idUsuario, string codigo)
    {
        if (!_usuarioMPP.ConfirmarCodigoTelegram(idUsuario, "Registro", _seguridad.GenerarHashToken(NormalizarCodigo(codigo)))) throw new UnauthorizedAccessException("El código no es válido, venció o ya fue utilizado.");
        Usuario? usuario = _usuarioMPP.ConsultarUsuarioPorId(idUsuario);
        if (usuario is not null) _bitacoraBLL.Add(new Bitacora(usuario.ID, usuario.IdAgencia, "Usuario", usuario.ID, "ValidarCuentaTelegram", "Se validó la cuenta inicial mediante Telegram.", "Exitoso", "Informacion", "Registro"));
    }

    public EntregaTelegram ReenviarCodigoVinculacion(Usuario usuario)
    {
        return PrepararReenvio(usuario.ID, "Vinculacion");
    }

    public EntregaTelegram ReenviarCodigoRegistro(int idUsuario)
    {
        return PrepararReenvio(idUsuario, "Registro");
    }

    public EntregaTelegram PrepararDesvinculacion(Usuario usuario)
    {
        EstadoTelegram estado = _usuarioMPP.ConsultarEstadoTelegram(usuario.ID);
        if (!estado.Vinculado) throw new InvalidOperationException("No hay una cuenta de Telegram vinculada.");
        string codigo = GenerarCodigo();
        return _usuarioMPP.CrearSolicitudDesvinculacionTelegram(usuario.ID, _seguridad.GenerarHashToken(codigo), DateTime.Now.AddMinutes(5), codigo);
    }

    public void ConfirmarDesvinculacion(Usuario usuario, string codigo)
    {
        if (!_usuarioMPP.ConfirmarCodigoTelegram(usuario.ID, "Desvinculacion", _seguridad.GenerarHashToken(NormalizarCodigo(codigo)))) throw new UnauthorizedAccessException("El código no es válido, venció o ya fue utilizado.");
        _bitacoraBLL.Add(new Bitacora(usuario.ID, usuario.IdAgencia, "Usuario", usuario.ID, "DesvincularTelegram", "Se desvinculó Telegram de la cuenta.", "Exitoso", "Advertencia", "Seguridad"));
    }

    private EntregaTelegram PrepararReenvio(int idUsuario, string tipo)
    {
        string codigo = GenerarCodigo();
        return _usuarioMPP.ReenviarCodigoTelegram(idUsuario, tipo, _seguridad.GenerarHashToken(codigo), DateTime.Now.AddMinutes(5), codigo);
    }

    private static string GenerarCodigo()
    {
        return RandomNumberGenerator.GetInt32(0, 1_000_000).ToString("D6");
    }

    private static string NormalizarCodigo(string? codigo)
    {
        return codigo?.Trim().Replace(" ", string.Empty) ?? string.Empty;
    }
}
