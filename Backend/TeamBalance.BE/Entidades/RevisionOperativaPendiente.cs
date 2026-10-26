namespace TeamBalance.BE.Entidades;
public class RevisionOperativaPendiente
{
    public RevisionOperativaPendiente()
    {
    }

    public RevisionOperativaPendiente(int id, string tipo, int? idCliente, string cliente, string referencia, string nombreProyecto, string causaPrincipal, string accionSugerida, string estado, DateTime fechaAlta)
    {
        ID = id;
        Tipo = tipo;
        IdCliente = idCliente;
        Cliente = cliente;
        Referencia = referencia;
        NombreProyecto = nombreProyecto;
        CausaPrincipal = causaPrincipal;
        AccionSugerida = accionSugerida;
        Estado = estado;
        FechaAlta = fechaAlta;
    }

    public int ID { get; set; }
    public string Tipo { get; set; } = string.Empty;
    public int? IdCliente { get; set; }
    public string Cliente { get; set; } = string.Empty;
    public string Referencia { get; set; } = string.Empty;
    public string NombreProyecto { get; set; } = string.Empty;
    public string CausaPrincipal { get; set; } = string.Empty;
    public string AccionSugerida { get; set; } = string.Empty;
    public string Estado { get; set; } = string.Empty;
    public DateTime FechaAlta { get; set; }
}
