namespace TeamBalance.BE.Entidades;
public class ConfiguracionAlertaAgencia
{
    [System.Text.Json.Serialization.JsonIgnore, ReferenciaInterna]
    public Agencia Agencia { get; set; } = new();

    public int IdAgencia
    {
        get => Agencia.ID;
        set => Agencia = Agencia.ID == value ? Agencia : new() { ID = value };
    }
    public decimal UmbralConsumoHoras { get; set; } = 80;
    public bool NotificacionInternaActiva { get; set; } = true;
    public bool EmailActivo { get; set; } = true;
    public bool SlackActivo { get; set; } = true;
}
