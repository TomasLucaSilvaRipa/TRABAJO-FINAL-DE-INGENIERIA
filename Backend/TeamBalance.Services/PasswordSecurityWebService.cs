using System.Net.Http.Json;
using TeamBalance.BE.Entidades;

namespace TeamBalance.Services;
public class PasswordSecurityWebService
{
    private readonly HttpClient _httpClient;
    public PasswordSecurityWebService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<PasswordEvaluationResponse> EvaluarPassword(string password)
    {
        SolicitudEvaluacionPassword solicitud = new SolicitudEvaluacionPassword(password);
        HttpResponseMessage response = await _httpClient.PostAsJsonAsync("api/password-security/evaluar", solicitud);
        response.EnsureSuccessStatusCode();
        PasswordEvaluationResponse? resultado = await response.Content.ReadFromJsonAsync<PasswordEvaluationResponse>();
        if (resultado is null)
        {
            throw new InvalidOperationException("El Web Service no devolvió una respuesta válida.");
        }

        return resultado;
    }
}
