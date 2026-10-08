using System.Security.Cryptography;
using System.Text;
using TeamBalance.BE.Entidades;

namespace TeamBalance.Services;

/// <summary>
/// Protege campos sensibles persistidos con AES-256-GCM. La clave se obtiene de
/// configuración segura (por ejemplo, User Secrets durante desarrollo), nunca de la base de datos.
/// </summary>
public sealed class FieldEncryptionService
{
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
}
