namespace TeamBalance.PasswordSecurity.WebService.Models
{
    public class PasswordRequirements
    {
        public PasswordRequirements(bool longitud, bool mayuscula, bool minuscula, bool numero, bool caracterEspecial)
        {
            Longitud = longitud;
            Mayuscula = mayuscula;
            Minuscula = minuscula;
            Numero = numero;
            CaracterEspecial = caracterEspecial;
        }

        public bool Longitud { get; set; }
        public bool Mayuscula { get; set; }
        public bool Minuscula { get; set; }
        public bool Numero { get; set; }
        public bool CaracterEspecial { get; set; }
    }
}
