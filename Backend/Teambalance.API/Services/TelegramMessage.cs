using System.Net.Http.Json;
using System.Text.Json.Serialization;
using TeamBalance.BE.Entidades;
using TeamBalance.BLL;

namespace Teambalance.API.Services;
public sealed class TelegramMessage
{
    [JsonPropertyName("text")]
    public string? Text { get; set; }

    [JsonPropertyName("chat")]
    public TelegramChat? Chat { get; set; }

    [JsonPropertyName("from")]
    public TelegramUser? From { get; set; }
}
