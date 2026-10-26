namespace TeamBalance.BE.Entidades;
public class RecomendacionSkill
{
    public int ID { get; set; }
    public int IdSkill { get; set; }
    public string TipoAccion { get; set; } = string.Empty;
    public string Observacion { get; set; } = string.Empty;
}
