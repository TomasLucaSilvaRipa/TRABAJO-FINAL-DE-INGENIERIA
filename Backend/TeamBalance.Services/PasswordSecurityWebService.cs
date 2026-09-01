using System.Net.Http.Json;

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
        HttpResponseMessage response = await _httpClient.PostAsJsonAsync("api/password-security/evaluar", new { Password = password });
        response.EnsureSuccessStatusCode();

        PasswordEvaluationResponse? resultado = await response.Content.ReadFromJsonAsync<PasswordEvaluationResponse>();

        if (resultado is null)
        {
            throw new InvalidOperationException("El Web Service no devolvió una respuesta válida.");
        }

        return resultado;
    }
}

public class PasswordEvaluationResponse
{
    public PasswordEvaluationResponse(bool valida, int puntaje, string nivel, PasswordRequirements requisitos)
    {
        Valida = valida;
        Puntaje = puntaje;
        Nivel = nivel;
        Requisitos = requisitos;
    }

    public bool Valida { get; set; }

    public int Puntaje { get; set; }

    public string Nivel { get; set; } = string.Empty;

    public PasswordRequirements Requisitos { get; set; }
}

public class PasswordRequirements
{
    public PasswordRequirements(bool longitud, bool mayuscula, bool minuscula, bool numero, bool caracterEspecial)
    {
        Longitud = longitud;
        Mayuscula = mayuscula;
        Minuscula = minuscula;
        Numero = numero;
        CaracterEspecial = caracterEspecial;
    }

    public bool Longitud { get; set; }

    public bool Mayuscula { get; set; }

    public bool Minuscula { get; set; }

    public bool Numero { get; set; }

    public bool CaracterEspecial { get; set; }
}