using System.Security.Cryptography;
using System.Text;
using TeamBalance.BE.Entidades;
using TeamBalance.MPP;

namespace TeamBalance.BLL;

public class BLLUsuario
{
    private const int IteracionesPassword = 120000;
    private const int DuracionSesionNormalHoras = 8;
    private const int DuracionSesionRecordadaDias = 30;
    private readonly MPPUsuario _usuarioMPP;
    private readonly BLLBitacora _bitacoraBLL;

    public BLLUsuario(MPPUsuario usuarioMPP, BLLBitacora bitacoraBLL)
    {
        _usuarioMPP = usuarioMPP;
        _bitacoraBLL = bitacoraBLL;
    }

    public bool EmailDisponible(string email)
    {
        return !_usuarioMPP.ExisteUsuarioPorEmail(email);
    }

    public Usuario? ConsultarUsuarioPendienteValidacion(string email)
    {
        return _usuarioMPP.ConsultarUsuarioPendienteValidacion(email);
    }

    //pasar paramtro objeto
    public void PrepararUsuarioDueño(Usuario usuario, int idRol)
    {
        string password = usuario.PasswordHash;

        usuario.IdRol = idRol;
        usuario.Nombre = usuario.Nombre.Trim();
        usuario.Apellido = usuario.Apellido.Trim();
        usuario.Email = usuario.Email.Trim().ToLowerInvariant();
        usuario.PasswordHash = GenerarHashPassword(password);
        usuario.Estado = "PendienteValidacion";
        usuario.FechaAlta = DateTime.Now;
        usuario.Activo = true;
    }

    public Dueño CrearDueño()
    {
        return new Dueño
        {
            Activo = true,
        };
    }

    public ValidacionCuentum CrearValidacionEmail(out string token)
    {
        token = Guid.NewGuid().ToString("N");

        return new ValidacionCuentum
        {
            Metodo = "Email",
            TokenHash = GenerarHashToken(token),
            FechaGeneracion = DateTime.Now,
            FechaExpiracion = DateTime.Now.AddHours(24),
            Utilizado = false,
            Activo = true,
        };
    }

    public bool ValidarCuenta(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return false;
        }

        return _usuarioMPP.ValidarCuenta(GenerarHashToken(token));
    }

    public void ReemplazarValidacionEmail(Usuario usuario, ValidacionCuentum validacion)
    {
        _usuarioMPP.ReemplazarValidacionEmail(usuario, validacion);
    }

    public (Usuario Usuario, string AccessToken, DateTime FechaExpiracion) IniciarSesion(Usuario usuarioEntrante, bool mantenerSesion)
    {
        if (string.IsNullOrWhiteSpace(usuarioEntrante.Email) || string.IsNullOrWhiteSpace(usuarioEntrante.PasswordHash))
        {
            throw new ArgumentException("Ingresá tu email y contraseña.");
        }

        Usuario? usuarioBD = _usuarioMPP.ConsultarUsuarioPorEmail(usuarioEntrante.Email.Trim().ToLowerInvariant());

        if (usuarioBD is null || !VerificarPassword(usuarioEntrante.PasswordHash, usuarioBD.PasswordHash))
        {
            _bitacoraBLL.Add(new Bitacora()
            {
                Entidad = "Usuario",
                Accion = "IniciarSesion",
                Mensaje = "Se rechazó un intento de inicio de sesión por credenciales inválidas.",
                Resultado = "Denegado",
                Criticidad = "Advertencia",
                Modulo = "Seguridad",
                FechaHora = DateTime.Now,
            });

            throw new UnauthorizedAccessException("El email o la contraseña no son correctos.");
        }

        if (!usuarioBD.Activo)
        {
            throw new InvalidOperationException("La cuenta se encuentra inactiva.");
        }

        if (!string.Equals(usuarioBD.Estado, "Activo", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Confirmá tu correo electrónico antes de iniciar sesión.");
        }

        string accessToken = Convert.ToBase64String(RandomNumberGenerator.GetBytes(48));
        DateTime fechaExpiracion = mantenerSesion
            ? DateTime.Now.AddDays(DuracionSesionRecordadaDias)
            : DateTime.Now.AddHours(DuracionSesionNormalHoras);

        SesionUsuario sesion = new SesionUsuario
        {
            IdUsuario = usuarioBD.ID,
            TokenHash = GenerarHashToken(accessToken),
            FechaInicio = DateTime.Now,
            FechaUltimaActividad = DateTime.Now,
            FechaExpiracion = fechaExpiracion,
            Activa = true,
        };

        _usuarioMPP.RegistrarSesion(sesion);

        _bitacoraBLL.Add(new Bitacora()
        {
            IdUsuario = usuarioBD.ID,
            IdAgencia = usuarioBD.IdAgencia,
            Entidad = "Usuario",
            IdEntidad = usuarioBD.ID,
            Accion = "IniciarSesion",
            Mensaje = "El usuario inició sesión en TeamBalance.",
            Resultado = "Exitoso",
            Criticidad = "Informacion",
            Modulo = "Seguridad",
            FechaHora = DateTime.Now,
        });

        return (usuarioBD, accessToken, fechaExpiracion);
    }

    public bool SesionVigente(string accessToken)
    {
        return !string.IsNullOrWhiteSpace(accessToken) && _usuarioMPP.SesionVigente(GenerarHashToken(accessToken));
    }

    public void CerrarSesion(string accessToken)
    {
        if (!string.IsNullOrWhiteSpace(accessToken))
        {
            _usuarioMPP.CerrarSesion(GenerarHashToken(accessToken));
        }
    }

    private static string GenerarHashPassword(string password)
    {
        byte[] salt = RandomNumberGenerator.GetBytes(16);
        byte[] hash = Rfc2898DeriveBytes.Pbkdf2(
            password,
            salt,
            IteracionesPassword,
            HashAlgorithmName.SHA256,
            32);

        return $"PBKDF2-SHA256${IteracionesPassword}${Convert.ToBase64String(salt)}${Convert.ToBase64String(hash)}";
    }

    private static bool VerificarPassword(string password, string passwordHash)
    {
        try
        {
            string[] partes = passwordHash.Split('$');

            if (partes.Length != 4 || partes[0] != "PBKDF2-SHA256" || !int.TryParse(partes[1], out int iteraciones))
            {
                return false;
            }

            byte[] salt = Convert.FromBase64String(partes[2]);
            byte[] hashEsperado = Convert.FromBase64String(partes[3]);
            byte[] hashActual = Rfc2898DeriveBytes.Pbkdf2(
                password,
                salt,
                iteraciones,
                HashAlgorithmName.SHA256,
                hashEsperado.Length);

            return CryptographicOperations.FixedTimeEquals(hashActual, hashEsperado);
        }
        catch
        {
            return false;
        }
    }

    private static string GenerarHashToken(string token)
    {
        return Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
    }
}
