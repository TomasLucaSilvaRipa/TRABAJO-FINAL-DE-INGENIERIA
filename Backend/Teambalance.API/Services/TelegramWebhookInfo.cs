using System.Net.Http.Json;
using System.Text.Json.Serialization;
using TeamBalance.BE.Entidades;
using TeamBalance.BLL;

namespace Teambalance.API.Services;
public sealed class TelegramWebhookInfo
{
    [JsonPropertyName("url")]
    public string? Url { get; set; }
}
