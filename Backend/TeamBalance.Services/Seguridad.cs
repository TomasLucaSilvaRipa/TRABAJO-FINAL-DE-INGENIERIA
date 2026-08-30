namespace TeamBalance.Services
{
    using System.Security.Cryptography;
    using System.Text;

    public class Seguridad
    {
        private const int IteracionesPassword = 120000;
        private const int TamanoSalt = 16;
        private const int TamanoHash = 32;
        private const int TamanoToken = 48;

        public string GenerarHashPassword(string password)
        {
            ValidarPassword(password);
            byte[] salt = RandomNumberGenerator.GetBytes(TamanoSalt);
            byte[] hash = Rfc2898DeriveBytes.Pbkdf2(password,salt,IteracionesPassword,HashAlgorithmName.SHA256,TamanoHash);
            return $"PBKDF2-SHA256${IteracionesPassword}${Convert.ToBase64String(salt)}${Convert.ToBase64String(hash)}";
        }

        public bool VerificarPassword(string password, string passwordHash)
        {
            if (string.IsNullOrWhiteSpace(password) || string.IsNullOrWhiteSpace(passwordHash))
            {
                return false;
            }

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
            catch (FormatException){ return false; }
            catch (ArgumentException){ return false; }
        }

        public string GenerarTokenSeguro()
        {
            return Convert.ToBase64String(RandomNumberGenerator.GetBytes(TamanoToken));
        }

        public string GenerarTokenRecuperacion()
        {
            return Convert.ToHexString(RandomNumberGenerator.GetBytes(TamanoToken));
        }

        public void ValidarPassword(string password)
        {
            if (string.IsNullOrWhiteSpace(password) || password.Length < 8 || !password.Any(char.IsLetter) || !password.Any(char.IsDigit))
            {
                throw new ArgumentException("La contraseña debe tener al menos 8 caracteres e incluir letras y números.");
            }
        }

        public string GenerarHashToken(string token)
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                throw new ArgumentException("El token no puede estar vacío.");
            }

            return Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
        }
    }
}
