namespace TeamBalance.BE.Entidades;
public class OcupacionEmpleado
{
    public OcupacionEmpleado()
    {
    }

    public OcupacionEmpleado(int idEmpleado, string nombre, string seniority, decimal horasDisponibles, decimal horasEstimadasActivas, decimal horasRegistradas, decimal porcentajeOcupacion, string estadoCarga, string causaPrincipal, string accionSugerida, List<string> proyectos)
    {
        IdEmpleado = idEmpleado;
        Nombre = nombre;
        Seniority = seniority;
        HorasDisponibles = horasDisponibles;
        HorasEstimadasActivas = horasEstimadasActivas;
        HorasRegistradas = horasRegistradas;
        PorcentajeOcupacion = porcentajeOcupacion;
        EstadoCarga = estadoCarga;
        CausaPrincipal = causaPrincipal;
        AccionSugerida = accionSugerida;
        Proyectos = proyectos;
    }

    public int IdEmpleado { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Seniority { get; set; } = string.Empty;
    public decimal HorasDisponibles { get; set; }
    public decimal HorasEstimadasActivas { get; set; }
    public decimal HorasRegistradas { get; set; }
    public decimal PorcentajeOcupacion { get; set; }
    public string EstadoCarga { get; set; } = string.Empty;
    public string CausaPrincipal { get; set; } = string.Empty;
    public string AccionSugerida { get; set; } = string.Empty;
    public List<string> Proyectos { get; set; } = new List<string>();
}
