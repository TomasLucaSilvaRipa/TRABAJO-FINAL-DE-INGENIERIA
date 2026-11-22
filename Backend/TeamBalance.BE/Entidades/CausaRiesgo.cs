namespace TeamBalance.BE.Entidades;

public class CausaRiesgo
{
    public CausaRiesgo(Proyecto proyecto, string tipoElemento, string descripcion, string nivel)
    {
        Proyecto = proyecto;
        TipoElemento = tipoElemento;
        Descripcion = descripcion;
        Nivel = nivel;
    }

    [System.Text.Json.Serialization.JsonIgnore]
    public Proyecto Proyecto { get; }

    public int IdElemento => Proyecto.ID;

    public string TipoElemento { get; set; } = string.Empty;

    public string Descripcion { get; set; } = string.Empty;

    public string Nivel { get; set; } = string.Empty;
}
