namespace TeamBalance.BE.Entidades;
public class ConfiguracionAlertaAgencia
{
    public int IdAgencia { get; set; }
    public decimal UmbralConsumoHoras { get; set; } = 80;
    public bool NotificacionInternaActiva { get; set; } = true;
    public bool EmailActivo { get; set; } = true;
    public bool SlackActivo { get; set; } = true;
}
