using System.Net.Http.Json;
using System.Text.Json.Serialization;
using TeamBalance.BE.Entidades;
using TeamBalance.BLL;

namespace Teambalance.API.Services;
public sealed class TelegramUser
{
    [JsonPropertyName("id")]
    public long Id { get; set; }

    [JsonPropertyName("username")]
    public string? Username { get; set; }
}
