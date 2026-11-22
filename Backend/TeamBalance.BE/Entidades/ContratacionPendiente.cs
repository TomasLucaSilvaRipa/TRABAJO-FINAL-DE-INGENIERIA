namespace TeamBalance.BE.Entidades;

public sealed class ContratacionPendiente
{
    public ContratacionPendiente(ContratacionServicio contratacion, PlanComercial planComercial, string referenciaContratacion, string referenciaOperacion, decimal importe, string moneda)
    {
        Contratacion = contratacion;
        PlanComercial = planComercial;
        ReferenciaContratacion = referenciaContratacion;
        ReferenciaOperacion = referenciaOperacion;
        Importe = importe;
        Moneda = moneda;
    }

    public ContratacionServicio Contratacion { get; }
    public PlanComercial PlanComercial { get; }
    public string ReferenciaContratacion { get; set; }
    public string ReferenciaOperacion { get; set; }
    public decimal Importe { get; set; }
    public string Moneda { get; set; }
}
