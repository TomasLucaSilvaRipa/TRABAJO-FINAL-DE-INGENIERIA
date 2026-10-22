namespace TeamBalance.BE.Entidades;

public class EstadoTelegram
{
    public EstadoTelegram() { }

    public EstadoTelegram(bool vinculado, string estadoSolicitud, DateTime? fechaExpiracionSolicitud, DateTime? fechaVinculacion)
    {
        Vinculado = vinculado;
        EstadoSolicitud = estadoSolicitud;
        FechaExpiracionSolicitud = fechaExpiracionSolicitud;
        FechaVinculacion = fechaVinculacion;
    }
    public bool Vinculado { get; set; }
    public string EstadoSolicitud { get; set; } = string.Empty;
    public DateTime? FechaExpiracionSolicitud { get; set; }
    public DateTime? FechaVinculacion { get; set; }
}

public class PreparacionTelegram
{
    public PreparacionTelegram() { }
    public PreparacionTelegram(string enlaceProfundo, string comandoInicio, DateTime fechaExpiracion)
    {
        EnlaceProfundo = enlaceProfundo;
        ComandoInicio = comandoInicio;
        FechaExpiracion = fechaExpiracion;
    }

    public string EnlaceProfundo { get; set; } = string.Empty;
    public string ComandoInicio { get; set; } = string.Empty;
    public DateTime FechaExpiracion { get; set; }
}

public class EntregaTelegram
{
    public EntregaTelegram() { }
    public EntregaTelegram(bool valida, long chatId, string codigo, string tipo)
    {
        Valida = valida;
        ChatId = chatId;
        Codigo = codigo;
        Tipo = tipo;
    }

    public bool Valida { get; set; }
    public long ChatId { get; set; }
    public string Codigo { get; set; } = string.Empty;
    public string Tipo { get; set; } = string.Empty;
}
