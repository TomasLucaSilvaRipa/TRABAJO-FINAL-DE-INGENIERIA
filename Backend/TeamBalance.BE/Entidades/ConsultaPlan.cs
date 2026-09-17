namespace TeamBalance.BE.Entidades;

public class ConsultaPlan
{
    public ConsultaPlan() { }
    public ConsultaPlan(int id, int idPlanComercial, string nombre, string email, string consulta, DateTime fechaAlta, bool activo)
    {
        ID = id;
        IdPlanComercial = idPlanComercial;
        Nombre = nombre;
        Email = email;
        Consulta = consulta;
        FechaAlta = fechaAlta;
        Activo = activo;
    }

    public int ID { get; set; }
    public int IdPlanComercial { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Consulta { get; set; } = string.Empty;
    public DateTime FechaAlta { get; set; }
    public bool Activo { get; set; }
}
