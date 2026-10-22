namespace TeamBalance.BE.Entidades;

/// <summary>Movimiento de cobro confirmado utilizado sólo por el BackOffice financiero.</summary>
public sealed class CobroPlataformaConfirmado
{
    public CobroPlataformaConfirmado(int idAgencia, string agencia, decimal importe, string moneda, string? proveedor, DateTime fechaCobro)
    {
        IdAgencia = idAgencia;
        Agencia = agencia;
        Importe = importe;
        Moneda = moneda;
        Proveedor = proveedor;
        FechaCobro = fechaCobro;
    }

    public int IdAgencia { get; set; }
    public string Agencia { get; set; } = string.Empty;
    public decimal Importe { get; set; }
    public string Moneda { get; set; } = string.Empty;
    public string? Proveedor { get; set; }
    public DateTime FechaCobro { get; set; }
}

public sealed class ResumenIngresosPlataforma
{
    public ResumenIngresosPlataforma(DateTime fechaCorte, List<ResumenIngresosMoneda> monedas)
    {
        FechaCorte = fechaCorte;
        Monedas = monedas;
    }

    public DateTime FechaCorte { get; set; }
    public List<ResumenIngresosMoneda> Monedas { get; set; } = new();
}

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
