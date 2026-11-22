namespace TeamBalance.BE.Entidades;
public class OcupacionEmpleado
{
    public OcupacionEmpleado(Empleado empleado)
    {
        Empleado = empleado;
    }

    public OcupacionEmpleado(Empleado empleado, decimal horasDisponibles, decimal horasEstimadasActivas, decimal horasRegistradas, decimal porcentajeOcupacion, string estadoCarga, string causaPrincipal, string accionSugerida, List<string> proyectos)
        : this(empleado)
    {
        HorasDisponibles = horasDisponibles;
        HorasEstimadasActivas = horasEstimadasActivas;
        HorasRegistradas = horasRegistradas;
        PorcentajeOcupacion = porcentajeOcupacion;
        EstadoCarga = estadoCarga;
        CausaPrincipal = causaPrincipal;
        AccionSugerida = accionSugerida;
        Proyectos = proyectos;
    }

    public Empleado Empleado { get; }
    public decimal HorasDisponibles { get; set; }
    public decimal HorasEstimadasActivas { get; set; }
    public decimal HorasRegistradas { get; set; }
    public decimal PorcentajeOcupacion { get; set; }
    public string EstadoCarga { get; set; } = string.Empty;
    public string CausaPrincipal { get; set; } = string.Empty;
    public string AccionSugerida { get; set; } = string.Empty;
    public List<string> Proyectos { get; set; } = new List<string>();
}
