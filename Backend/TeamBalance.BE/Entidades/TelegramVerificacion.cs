namespace TeamBalance.BE.Entidades;

public class EstadoTelegram
{
    public bool Vinculado { get; set; }
    public string EstadoSolicitud { get; set; } = string.Empty;
    public DateTime? FechaExpiracionSolicitud { get; set; }
    public DateTime? FechaVinculacion { get; set; }
}

public class PreparacionTelegram
{
    public string EnlaceProfundo { get; set; } = string.Empty;
    public DateTime FechaExpiracion { get; set; }
}

public class EntregaTelegram
{
    public bool Valida { get; set; }
    public long ChatId { get; set; }
    public string Codigo { get; set; } = string.Empty;
    public string Tipo { get; set; } = string.Empty;
}
