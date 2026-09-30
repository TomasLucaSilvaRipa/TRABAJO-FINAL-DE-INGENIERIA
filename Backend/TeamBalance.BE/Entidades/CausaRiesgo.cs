namespace TeamBalance.BE.Entidades;

public class CausaRiesgo
{
    public CausaRiesgo()
    {
    }

    public CausaRiesgo(int idElemento, string tipoElemento, string descripcion, string nivel)
    {
        IdElemento = idElemento;
        TipoElemento = tipoElemento;
        Descripcion = descripcion;
        Nivel = nivel;
    }

    public int IdElemento { get; set; }

    public string TipoElemento { get; set; } = string.Empty;

    public string Descripcion { get; set; } = string.Empty;

    public string Nivel { get; set; } = string.Empty;
}
