using TeamBalance.PasswordSecurity.WebService.Models;

namespace TeamBalance.PasswordSecurity.WebService.Services
{
    public class PasswordEvaluationService
    {
        public PasswordEvaluationResponse Evaluar(string password)
        {
            password ??= string.Empty;

            bool longitud = password.Length >= 8;
            bool mayuscula = password.Any(char.IsUpper);
            bool minuscula = password.Any(char.IsLower);
            bool numero = password.Any(char.IsDigit);
            bool caracterEspecial = password.Any(c => !char.IsLetterOrDigit(c));

            int puntaje = 0;

            if (longitud) puntaje++;
            if (mayuscula) puntaje++;
            if (minuscula) puntaje++;
            if (numero) puntaje++;
            if (caracterEspecial) puntaje++;

            string nivel = puntaje switch
            {
                <= 2 => "Débil",
                3 or 4 => "Media",
                5 => "Fuerte",
                _ => "Débil"
            };

            bool valida = longitud && mayuscula && minuscula && numero && caracterEspecial;

            PasswordRequirements requisitos = new PasswordRequirements(longitud, mayuscula, minuscula, numero, caracterEspecial);

            PasswordEvaluationResponse response = new PasswordEvaluationResponse(valida, puntaje,nivel, requisitos);

            return response;
        }
    }
}
