namespace TeamBalance.BE.Entidades;

public class Idioma
{
    public Idioma()
    {
    }

    public Idioma(int id)
    {
        ID = id;
    }

    public Idioma(int id, string codigo, string nombre, bool activo, Dictionary<string, string> traducciones)
    {
        ID = id;
        Codigo = codigo;
        Nombre = nombre;
        Activo = activo;
        Traducciones = traducciones;
    }

    public int ID { get; set; }
    public string Codigo { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public bool Activo { get; set; } = true;
    public Dictionary<string, string> Traducciones { get; set; } = new(StringComparer.Ordinal);
}
