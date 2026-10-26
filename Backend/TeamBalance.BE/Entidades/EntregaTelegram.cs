namespace TeamBalance.BE.Entidades;
public class EntregaTelegram
{
    public EntregaTelegram()
    {
    }

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
