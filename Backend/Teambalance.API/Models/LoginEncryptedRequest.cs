namespace Teambalance.API.Models
{
    public class LoginEncryptedRequest
    {
        public string? EncryptedData { get; set; }

        public string? EncryptedKey { get; set; }

        public string? Iv { get; set; }

        public string? RecaptchaToken { get; set; }
    }
}
