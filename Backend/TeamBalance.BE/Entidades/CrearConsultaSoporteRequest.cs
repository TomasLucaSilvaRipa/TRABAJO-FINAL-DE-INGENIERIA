namespace TeamBalance.BE.Entidades;
public sealed class CrearConsultaSoporteRequest
{
    public int? IdSuscripcion { get; set; }
    public string Categoria { get; set; } = string.Empty;
    public string Asunto { get; set; } = string.Empty;
    public string Descripcion { get; set; } = string.Empty;
}
