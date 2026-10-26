using System.Net.Http.Headers;using System.Net.Http.Json;using TeamBalance.BE.Entidades;
namespace TeamBalance.Services;
public class SlackPushService
{
 private readonly HttpClient _http;private readonly string? _token;public SlackPushService(HttpClient http,string? token){_http=http;_token=token;}
 public async Task<bool> Enviar(MensajeSlack mensaje){if(string.IsNullOrWhiteSpace(_token)||string.IsNullOrWhiteSpace(mensaje.IdUsuarioSlack))return false;_http.DefaultRequestHeaders.Authorization=new AuthenticationHeaderValue("Bearer",_token);HttpResponseMessage apertura=await _http.PostAsJsonAsync("https://slack.com/api/conversations.open",new{users=mensaje.IdUsuarioSlack});using System.Text.Json.JsonDocument datos=System.Text.Json.JsonDocument.Parse(await apertura.Content.ReadAsStringAsync());if(!datos.RootElement.TryGetProperty("channel",out var channel)||!channel.TryGetProperty("id",out var idCanal))return false;string? canal=idCanal.GetString();if(string.IsNullOrWhiteSpace(canal))return false;HttpResponseMessage envio=await _http.PostAsJsonAsync("https://slack.com/api/chat.postMessage",new{channel=canal,text=mensaje.Contenido});return envio.IsSuccessStatusCode;}
}
