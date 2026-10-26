using System.Text.Json.Serialization;

namespace TeamBalance.BE.Entidades;
public class SolicitudEvaluacionPassword
{
    public SolicitudEvaluacionPassword(string password) => Password = password;
    public string Password { get; set; }
}
