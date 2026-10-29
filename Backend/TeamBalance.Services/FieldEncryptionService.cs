using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.IO.Compression;
using TeamBalance.BE.Entidades;

namespace TeamBalance.Services;

/// <summary>
/// Protege campos sensibles persistidos con AES-256-GCM. La clave se obtiene de
/// configuración segura (por ejemplo, User Secrets durante desarrollo), nunca de la base de datos.
/// </summary>
public sealed class FieldEncryptionService
{
    private const string SessionTokenPrefix = "TBS1.";
    private const string SessionTokenPurpose = "TeamBalance:SesionUsuario:v1";
    private const string VersionPrefix = "ENC:v1";
    private const int NonceLength = 12;
    private const int TagLength = 16;
    private static readonly byte[] DisponibilidadAssociatedData = Encoding.UTF8.GetBytes("TeamBalance:DisponibilidadBase:Observacion:v1");
    private readonly byte[] _key;

    public FieldEncryptionService(string keyBase64)
    {
        if (string.IsNullOrWhiteSpace(keyBase64))
        {
            throw new InvalidOperationException("No se configuró la clave de cifrado de campos sensibles.");
        }

        try
        {
            _key = Convert.FromBase64String(keyBase64);
        }
        catch (FormatException ex)
        {
            throw new InvalidOperationException("La clave de cifrado de campos sensibles no posee formato Base64 válido.", ex);
        }

        if (_key.Length != 32)
        {
            throw new InvalidOperationException("La clave de cifrado de campos sensibles debe tener exactamente 256 bits.");
        }
    }

    public string? CifrarObservacionDisponibilidad(DisponibilidadBase disponibilidad)
    {
        if (string.IsNullOrWhiteSpace(disponibilidad.Observacion))
        {
            return disponibilidad.Observacion;
        }

        byte[] nonce = RandomNumberGenerator.GetBytes(NonceLength);
        byte[] textoPlano = Encoding.UTF8.GetBytes(disponibilidad.Observacion);
        byte[] textoCifrado = new byte[textoPlano.Length];
        byte[] tag = new byte[TagLength];

        using (AesGcm aes = new AesGcm(_key, TagLength))
        {
            aes.Encrypt(nonce, textoPlano, textoCifrado, tag, DisponibilidadAssociatedData);
        }

        return string.Join(':', VersionPrefix, Convert.ToBase64String(nonce), Convert.ToBase64String(tag), Convert.ToBase64String(textoCifrado));
    }

    public string? DescifrarObservacionDisponibilidad(DisponibilidadBase disponibilidad)
    {
        string? valorPersistido = disponibilidad.Observacion;
        if (string.IsNullOrWhiteSpace(valorPersistido) || !valorPersistido.StartsWith(VersionPrefix + ":", StringComparison.Ordinal))
        {
            // Registros anteriores a esta mejora permanecen disponibles y se cifran al próximo guardado.
            return valorPersistido;
        }

        string[] partes = valorPersistido.Split(':');
        if (partes.Length != 5 || !string.Equals($"{partes[0]}:{partes[1]}", VersionPrefix, StringComparison.Ordinal))
        {
            throw new CryptographicException("La observación protegida tiene un formato inválido.");
        }

        try
        {
            byte[] nonce = Convert.FromBase64String(partes[2]);
            byte[] tag = Convert.FromBase64String(partes[3]);
            byte[] textoCifrado = Convert.FromBase64String(partes[4]);

            if (nonce.Length != NonceLength || tag.Length != TagLength)
            {
                throw new CryptographicException("La observación protegida no contiene los parámetros criptográficos esperados.");
            }

            byte[] textoPlano = new byte[textoCifrado.Length];
            using (AesGcm aes = new AesGcm(_key, TagLength))
            {
                aes.Decrypt(nonce, textoCifrado, tag, textoPlano, DisponibilidadAssociatedData);
            }

            return Encoding.UTF8.GetString(textoPlano);
        }
        catch (FormatException ex)
        {
            throw new CryptographicException("La observación protegida no pudo recuperarse.", ex);
        }
    }

    public string CifrarSecretoAutenticacionDosFactores(string secreto)
    {
        return Cifrar(secreto, "TeamBalance:AutenticacionDosFactores:Totp:v1");
    }

    public string DescifrarSecretoAutenticacionDosFactores(string secretoCifrado)
    {
        return Descifrar(secretoCifrado, "TeamBalance:AutenticacionDosFactores:Totp:v1");
    }

    public string GenerarTokenSesion(Usuario usuario, SesionUsuario sesion)
    {
        var autorizacion = new
        {
            Version = 1,
            Expira = new DateTimeOffset(sesion.FechaExpiracion.ToUniversalTime()).ToUnixTimeSeconds(),
            usuario.ID, usuario.IdAgencia, usuario.Nombre, usuario.Apellido, usuario.Email,
            usuario.Estado, usuario.FechaAlta,
            Roles = usuario.Roles.Select(rol => new { rol.ID, rol.Nombre, rol.TipoUsuario }).ToArray(),
            Permisos = usuario.Permisos.Select(permiso => new { permiso.ID, permiso.Codigo, permiso.Nombre, permiso.Url }).ToArray()
        };
        byte[] contenido = JsonSerializer.SerializeToUtf8Bytes(autorizacion);
        using MemoryStream comprimido = new();
        using (BrotliStream compresor = new(comprimido, CompressionLevel.Fastest, leaveOpen: true))
        {
            compresor.Write(contenido);
        }
        string token = SessionTokenPrefix + Cifrar(Convert.ToBase64String(comprimido.ToArray()), SessionTokenPurpose);
        if (token.Length > 16000) throw new InvalidOperationException("La autorización excede el tamaño permitido para una sesión.");
        return token;
    }

    public Usuario? ConsultarUsuarioToken(string token)
    {
        if (string.IsNullOrWhiteSpace(token) || token.Length > 16000 || !token.StartsWith(SessionTokenPrefix, StringComparison.Ordinal)) return null;
        try
        {
            string protegido = token[SessionTokenPrefix.Length..];
            string[] partes = protegido.Split(':');
            if (partes.Length != 5 || partes.Skip(2).Any(parte => !string.Equals(Convert.ToBase64String(Convert.FromBase64String(parte)), parte, StringComparison.Ordinal))) return null;
            byte[] comprimido = Convert.FromBase64String(Descifrar(protegido, SessionTokenPurpose));
            using MemoryStream origen = new(comprimido);
            using BrotliStream descompresor = new(origen, CompressionMode.Decompress);
            using MemoryStream contenido = new();
            byte[] bloque = new byte[4096];
            int leidos;
            while ((leidos = descompresor.Read(bloque)) > 0)
            {
                if (contenido.Length + leidos > 131072) return null;
                contenido.Write(bloque, 0, leidos);
            }
            using JsonDocument documento = JsonDocument.Parse(contenido.ToArray());
            JsonElement datos = documento.RootElement;
            if (datos.GetProperty("Version").GetInt32() != 1 || datos.GetProperty("Expira").GetInt64() <= DateTimeOffset.UtcNow.ToUnixTimeSeconds()) return null;
            List<Rol> roles = datos.GetProperty("Roles").Deserialize<List<Rol>>() ?? new();
            List<Permiso> permisos = datos.GetProperty("Permisos").Deserialize<List<Permiso>>() ?? new();
            int id = datos.GetProperty("ID").GetInt32();
            if (id <= 0 || roles.Count == 0) return null;
            Usuario usuario = new(id, datos.GetProperty("IdAgencia").ValueKind == JsonValueKind.Null ? null : datos.GetProperty("IdAgencia").GetInt32(), roles[0],
                datos.GetProperty("Nombre").GetString() ?? string.Empty, datos.GetProperty("Apellido").GetString() ?? string.Empty,
                datos.GetProperty("Email").GetString() ?? string.Empty, string.Empty, datos.GetProperty("Estado").GetString() ?? string.Empty,
                datos.GetProperty("FechaAlta").GetDateTime(), true);
            usuario.Roles = roles;
            usuario.Permisos = permisos;
            return usuario;
        }
        catch (Exception ex) when (ex is CryptographicException or FormatException or JsonException or InvalidOperationException or KeyNotFoundException or ArgumentException or InvalidDataException or IOException or OverflowException)
        {
            return null;
        }
    }

    private string Cifrar(string valor, string contexto)
    {
        byte[] nonce = RandomNumberGenerator.GetBytes(NonceLength);
        byte[] textoPlano = Encoding.UTF8.GetBytes(valor);
        byte[] textoCifrado = new byte[textoPlano.Length];
        byte[] tag = new byte[TagLength];
        byte[] datosAsociados = Encoding.UTF8.GetBytes(contexto);
        using (AesGcm aes = new AesGcm(_key, TagLength))
        {
            aes.Encrypt(nonce, textoPlano, textoCifrado, tag, datosAsociados);
        }
        return string.Join(':', VersionPrefix, Convert.ToBase64String(nonce), Convert.ToBase64String(tag), Convert.ToBase64String(textoCifrado));
    }

    private string Descifrar(string valorCifrado, string contexto)
    {
        string[] partes = valorCifrado.Split(':');
        if (partes.Length != 5 || !string.Equals($"{partes[0]}:{partes[1]}", VersionPrefix, StringComparison.Ordinal))
        {
            throw new CryptographicException("El secreto protegido tiene un formato inválido.");
        }
        byte[] nonce = Convert.FromBase64String(partes[2]);
        byte[] tag = Convert.FromBase64String(partes[3]);
        byte[] textoCifrado = Convert.FromBase64String(partes[4]);
        if (nonce.Length != NonceLength || tag.Length != TagLength)
        {
            throw new CryptographicException("El secreto protegido no contiene los parámetros criptográficos esperados.");
        }
        byte[] textoPlano = new byte[textoCifrado.Length];
        using (AesGcm aes = new AesGcm(_key, TagLength))
        {
            aes.Decrypt(nonce, textoCifrado, tag, textoPlano, Encoding.UTF8.GetBytes(contexto));
        }
        return Encoding.UTF8.GetString(textoPlano);
    }
}
