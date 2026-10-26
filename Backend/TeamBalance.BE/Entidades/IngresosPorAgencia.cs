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

    public int IdAgencia { get; set; }
    public string Agencia { get; set; } = string.Empty;
    public decimal Hoy { get; set; }
    public decimal Semana { get; set; }
    public decimal Mes { get; set; }
    public decimal Anio { get; set; }
    public decimal TotalHistorico { get; set; }
    public int CobrosHistoricos { get; set; }
}
