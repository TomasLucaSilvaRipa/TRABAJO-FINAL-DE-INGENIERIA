using System.Text.Json.Serialization;

namespace TeamBalance.BE.Entidades;
public sealed class IngresosPorAgencia
{
    public IngresosPorAgencia(int idAgencia, string agencia, decimal hoy, decimal semana, decimal mes, decimal anio, decimal totalHistorico, int cobrosHistoricos)
    {
        IdAgencia = idAgencia;
        Agencia = agencia;
        Hoy = hoy;
        Semana = semana;
        Mes = mes;
        Anio = anio;
        TotalHistorico = totalHistorico;
        CobrosHistoricos = cobrosHistoricos;
    }

    [JsonIgnore, ReferenciaInterna]
    public Agencia AgenciaRelacionada { get; set; } = new() { NombreComercial = string.Empty };

    public int IdAgencia
    {
        get => AgenciaRelacionada.ID;
        set => AgenciaRelacionada = AgenciaRelacionada.ID == value ? AgenciaRelacionada : new() { ID = value, NombreComercial = AgenciaRelacionada.NombreComercial };
    }
    public string Agencia
    {
        get => AgenciaRelacionada.NombreComercial;
        set => AgenciaRelacionada.NombreComercial = value;
    }
    public decimal Hoy { get; set; }
    public decimal Semana { get; set; }
    public decimal Mes { get; set; }
    public decimal Anio { get; set; }
    public decimal TotalHistorico { get; set; }
    public int CobrosHistoricos { get; set; }
}
