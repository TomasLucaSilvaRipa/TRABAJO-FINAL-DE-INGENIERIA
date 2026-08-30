using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace TeamBalance.Services
{
    public class EncryptionService : IDisposable
    {
        private readonly RSA _rsa;

        public EncryptionService()
        {
            _rsa = RSA.Create(2048);
        }

        public string ObtenerClavePublica()
        {
            return _rsa.ExportSubjectPublicKeyInfoPem();
        }

        public LoginDecryptedData DesencriptarLogin( string encryptedDataBase64,string encryptedKeyBase64, string ivBase64)
        {
            // 1. Recuperar los bytes enviados por Angular.
            byte[] encryptedKey = Convert.FromBase64String(encryptedKeyBase64);

            byte[] encryptedData = Convert.FromBase64String(encryptedDataBase64);

            byte[] iv = Convert.FromBase64String(ivBase64);

            // 2. Recuperar la clave AES usando la clave privada RSA.
            byte[] aesKey = _rsa.Decrypt( encryptedKey, RSAEncryptionPadding.OaepSHA256);

            // Web Crypto AES-GCM devuelve:
            //
            // ciphertext + authentication tag
            //
            // El tag por defecto es de 16 bytes.
            const int tagLength = 16;

            if (encryptedData.Length <= tagLength)
            {
                throw new CryptographicException(
                    "Los datos cifrados no son válidos."
                );
            }

            byte[] cipherText = encryptedData[..^tagLength];

            byte[] tag = encryptedData[^tagLength..];

            byte[] plainText = new byte[cipherText.Length];

            // 3. Descifrar email/password con AES-GCM.
            using AesGcm aes = new AesGcm(aesKey, tagLength);

            aes.Decrypt( iv, cipherText, tag, plainText);

            // 4. Bytes -> JSON.
            string json = Encoding.UTF8.GetString(plainText);

            LoginDecryptedData? resultado = JsonSerializer.Deserialize<LoginDecryptedData>( json, new JsonSerializerOptions(){ PropertyNameCaseInsensitive = true} );

            if (resultado is null)
            {
                throw new CryptographicException( "No fue posible recuperar las credenciales." );
            }

            return resultado;
        }

        public void Dispose()
        {
            _rsa.Dispose();
        }

        public class LoginDecryptedData
        {
            public string Email { get; set; } = string.Empty;

            public string Password { get; set; } = string.Empty;
        }
    }
}
