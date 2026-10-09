namespace TeamBalance.BE.Entidades;

public class ConsultaPlan
{
    public ConsultaPlan() { }
    public ConsultaPlan(int id, int idPlanComercial, string nombre, string email, string consulta, DateTime fechaAlta, bool activo, string estado, string? respuesta, DateTime? fechaRespuesta, int? idUsuarioSoporte, string? nombrePlan, string? nombreRespondedor)
    {
        ID = id;
        IdPlanComercial = idPlanComercial;
        Nombre = nombre;
        Email = email;
        Consulta = consulta;
        FechaAlta = fechaAlta;
        Activo = activo;
        Estado = estado;
        Respuesta = respuesta;
        FechaRespuesta = fechaRespuesta;
        IdUsuarioSoporte = idUsuarioSoporte;
        NombrePlan = nombrePlan;
        NombreRespondedor = nombreRespondedor;
    }

    public int ID { get; set; }
    public int IdPlanComercial { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Consulta { get; set; } = string.Empty;
    public DateTime FechaAlta { get; set; }
    public bool Activo { get; set; }
    public string Estado { get; set; } = string.Empty;
    public string? Respuesta { get; set; }
    public DateTime? FechaRespuesta { get; set; }
    public int? IdUsuarioSoporte { get; set; }
    public string? NombrePlan { get; set; }
    public string? NombreRespondedor { get; set; }
}
