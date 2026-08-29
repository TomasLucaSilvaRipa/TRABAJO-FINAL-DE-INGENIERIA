using System.Net.Mail;
using TeamBalance.BE.Entidades;
using TeamBalance.MPP;
using TeamBalance.Services;

namespace TeamBalance.BLL;

public class BLLAgencia
{
    private readonly MPPAgencia _agenciaMPP;
    private readonly ContratacionBLL _contratacionBLL;
    private readonly BLLUsuario _usuarioBLL;
    private readonly BLLRol _rolBLL;
    private readonly BLLBitacora _bitacoraBLL;
    private readonly EmailService _emailService;

    public BLLAgencia(MPPAgencia agenciaMPP, ContratacionBLL contratacionBLL, BLLUsuario usuarioBLL, BLLRol rolBLL, BLLBitacora bitacoraBLL, EmailService emailService){
        _agenciaMPP = agenciaMPP;
        _contratacionBLL = contratacionBLL;
        _usuarioBLL = usuarioBLL;
        _rolBLL = rolBLL;
        _bitacoraBLL = bitacoraBLL;
        _emailService = emailService;
    }

    public async Task<bool> RegistrarAgencia( Agencia agencia, Usuario usuario, string referenciaContratacion)
    {
        ValidarDatosRegistro(usuario, referenciaContratacion);
        ContratacionServicio contratacion = _contratacionBLL.ConsultarContratacionParaRegistro(referenciaContratacion);

        agencia.NombreComercial = contratacion.NombreComercialAgencia;
        agencia.RazonSocial = contratacion.RazonSocial;
        agencia.CUIT = contratacion.CUIT;
        agencia.CondicionFiscal = contratacion.CondicionFiscal;
        agencia.EmailContacto = contratacion.EmailFacturacion ?? contratacion.EmailLaboralResponsable;
        agencia.TelefonoContacto = string.IsNullOrWhiteSpace(agencia.TelefonoContacto) ? contratacion.TelefonoContacto : agencia.TelefonoContacto.Trim();
        agencia.FechaAlta = DateTime.Now;
        agencia.Estado = "Activa";
        agencia.Activo = true;

        if (_agenciaMPP.ExisteAgencia(agencia.CUIT, agencia.EmailContacto))
        {
            throw new InvalidOperationException("Ya existe una agencia registrada con el CUIT o email de contacto indicado.");
        }

        string email = usuario.Email.Trim().ToLowerInvariant();

        if (!_usuarioBLL.EmailDisponible(email))
        {
            throw new InvalidOperationException("Ya existe un usuario registrado con ese email laboral.");
        }

        Rol rolDueño = _rolBLL.ConsultarRolPorNombre("Dueño");
        _usuarioBLL.PrepararUsuarioDueño(usuario, rolDueño.ID);
        Dueño dueño = _usuarioBLL.CrearDueño();
        ValidacionCuentum validacion = _usuarioBLL.CrearValidacionEmail(out string token);

        var registro = _agenciaMPP.RegistrarAgencia(
            agencia,
            usuario,
            dueño,
            validacion,
            contratacion.ReferenciaContratacion);

        agencia.ID = registro.IdAgencia;
        usuario.ID = registro.IdUsuario;
        usuario.IdAgencia = registro.IdAgencia;

        _bitacoraBLL.Add(new Bitacora()
        {
            IdUsuario = usuario.ID,
            IdAgencia = agencia.ID,
            Entidad = "Agencia",
            IdEntidad = agencia.ID,
            Accion = "RegistrarAgencia",
            Mensaje = "Se registró la agencia y el usuario Dueño inicial.",
            Resultado = "Exitoso",
            Criticidad = "Informacion",
            Modulo = "Registro",
            FechaHora = DateTime.Now,
        });

        return await _emailService.EnviarCorreoValidacion(usuario.Email, usuario.Nombre, token);
    }

    public bool ValidarCuenta(string token)
    {
        return _usuarioBLL.ValidarCuenta(token);
    }

    public async Task ReenviarValidacion(string email)
    {
        if (string.IsNullOrWhiteSpace(email) || !MailAddress.TryCreate(email.Trim(), out _))
        {
            return;
        }

        Usuario? usuario = _usuarioBLL.ConsultarUsuarioPendienteValidacion(email.Trim().ToLowerInvariant());

        if (usuario is null)
        {
            return;
        }

        ValidacionCuentum validacion = _usuarioBLL.CrearValidacionEmail(out string token);
        _usuarioBLL.ReemplazarValidacionEmail(usuario, validacion);

        await _emailService.EnviarCorreoValidacion(usuario.Email, usuario.Nombre, token);

        _bitacoraBLL.Add(new Bitacora()
        {
            IdUsuario = usuario.ID,
            IdAgencia = usuario.IdAgencia,
            Entidad = "ValidacionCuenta",
            IdEntidad = usuario.ID,
            Accion = "ReenviarValidacion",
            Mensaje = "Se generó un nuevo enlace de validación de correo.",
            Resultado = "Exitoso",
            Criticidad = "Informacion",
            Modulo = "Registro",
            FechaHora = DateTime.Now,
        });
    }

    private static void ValidarDatosRegistro(Usuario usuario, string referenciaContratacion)
    {
        if (string.IsNullOrWhiteSpace(referenciaContratacion) || string.IsNullOrWhiteSpace(usuario.Nombre) || string.IsNullOrWhiteSpace(usuario.Apellido) || string.IsNullOrWhiteSpace(usuario.Email) || string.IsNullOrWhiteSpace(usuario.PasswordHash))
        {
            throw new ArgumentException("Completá todos los datos obligatorios del registro.");
        }

        if (!MailAddress.TryCreate(usuario.Email.Trim(), out _))
        {
            throw new ArgumentException("Ingresá un email laboral válido.");
        }

        if (usuario.PasswordHash.Length < 8 || !usuario.PasswordHash.Any(char.IsLetter) || !usuario.PasswordHash.Any(char.IsDigit))
        {
            throw new ArgumentException("La contraseña debe tener al menos 8 caracteres e incluir letras y números.");
        }
    }
}
