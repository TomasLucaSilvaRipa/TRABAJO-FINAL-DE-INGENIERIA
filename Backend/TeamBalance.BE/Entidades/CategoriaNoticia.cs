namespace TeamBalance.BE.Entidades;
public sealed class CategoriaNoticia
{
    public CategoriaNoticia()
    {
    }

    public CategoriaNoticia(int id, string nombre, string descripcion, bool activo)
    {
        ID = id;
        Nombre = nombre;
        Descripcion = descripcion;
        Activo = activo;
    }

    public int ID { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Descripcion { get; set; } = string.Empty;
    public bool Activo { get; set; }
}
