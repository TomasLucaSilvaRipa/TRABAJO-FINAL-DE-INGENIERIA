namespace TeamBalance.BE.Entidades;

/// <summary>Movimiento de cobro confirmado utilizado sólo por el BackOffice financiero.</summary>
public sealed class CobroPlataformaConfirmado
{
    public int IdAgencia { get; set; }
    public string Agencia { get; set; } = string.Empty;
    public decimal Importe { get; set; }
    public string Moneda { get; set; } = string.Empty;
    public string? Proveedor { get; set; }
    public DateTime FechaCobro { get; set; }
}

public sealed class ResumenIngresosPlataforma
{
    public DateTime FechaCorte { get; set; }
    public List<ResumenIngresosMoneda> Monedas { get; set; } = new();
}

public sealed class ResumenIngresosMoneda
{
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
    public int IdAgencia { get; set; }
    public string Agencia { get; set; } = string.Empty;
    public decimal Hoy { get; set; }
    public decimal Semana { get; set; }
    public decimal Mes { get; set; }
    public decimal Anio { get; set; }
    public decimal TotalHistorico { get; set; }
    public int CobrosHistoricos { get; set; }
}
