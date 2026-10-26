namespace TeamBalance.BE.Entidades;
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
