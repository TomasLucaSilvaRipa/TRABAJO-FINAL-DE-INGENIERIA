using System.Net.Http.Json;
using TeamBalance.BE.Entidades;

namespace TeamBalance.Services;
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
