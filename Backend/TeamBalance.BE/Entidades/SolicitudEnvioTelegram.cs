using System.Text.Json.Serialization;

namespace TeamBalance.BE.Entidades;
public class SolicitudEnvioTelegram
{
    public SolicitudEnvioTelegram(long chatId, string texto)
    {
        ChatId = chatId;
        Texto = texto;
    }

    [JsonPropertyName("chat_id")]
    public long ChatId { get; set; }

    [JsonPropertyName("text")]
    public string Texto { get; set; }
}
