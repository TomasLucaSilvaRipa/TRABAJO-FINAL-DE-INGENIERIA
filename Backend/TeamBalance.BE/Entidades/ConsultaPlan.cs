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
    [System.Text.Json.Serialization.JsonIgnore]
    public PlanComercial PlanComercial { get; set; } = new();

    public int IdPlanComercial
    {
        get => PlanComercial.ID;
        set => PlanComercial = PlanComercial.ID == value ? PlanComercial : new() { ID = value, Nombre = PlanComercial.Nombre };
    }
    public string Nombre { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Consulta { get; set; } = string.Empty;
    public DateTime FechaAlta { get; set; }
    public bool Activo { get; set; }
    public string Estado { get; set; } = string.Empty;
    public string? Respuesta { get; set; }
    public DateTime? FechaRespuesta { get; set; }
    [System.Text.Json.Serialization.JsonIgnore]
    public Usuario? UsuarioSoporte { get; set; }

    public int? IdUsuarioSoporte
    {
        get => UsuarioSoporte?.ID;
        set => UsuarioSoporte = value.HasValue ? (UsuarioSoporte?.ID == value.Value ? UsuarioSoporte : new() { ID = value.Value }) : null;
    }
    public string? NombrePlan { get => PlanComercial.Nombre; set => PlanComercial.Nombre = value!; }
    public string? NombreRespondedor { get; set; }
}
