namespace TeamBalance.BE.Entidades;

public class Idioma
{
    public int ID { get; set; }
    public string Codigo { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public bool Activo { get; set; } = true;
    public Dictionary<string, string> Traducciones { get; set; } = new(StringComparer.Ordinal);
}
