namespace TeamBalance.BE.Entidades;
public sealed class ResumenIngresosMoneda
{
    public ResumenIngresosMoneda(string moneda, decimal hoy, decimal semana, decimal mes, decimal anio, decimal totalHistorico, int cobrosHistoricos, List<IngresosPorAgencia> agencias)
    {
        Moneda = moneda;
        Hoy = hoy;
        Semana = semana;
        Mes = mes;
        Anio = anio;
        TotalHistorico = totalHistorico;
        CobrosHistoricos = cobrosHistoricos;
        Agencias = agencias;
    }

    public string Moneda { get; set; } = string.Empty;
    public decimal Hoy { get; set; }
    public decimal Semana { get; set; }
    public decimal Mes { get; set; }
    public decimal Anio { get; set; }
    public decimal TotalHistorico { get; set; }
    public int CobrosHistoricos { get; set; }
    public List<IngresosPorAgencia> Agencias { get; set; } = new();
}
