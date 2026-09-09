namespace TeamBalance.BE.Entidades;

public sealed class ContratacionPendiente
{
    public ContratacionPendiente(int idContratacion, int idPlanComercial, string referenciaContratacion, string referenciaOperacion, decimal importe, string moneda)
    {
        IdContratacion = idContratacion;
        IdPlanComercial = idPlanComercial;
        ReferenciaContratacion = referenciaContratacion;
        ReferenciaOperacion = referenciaOperacion;
        Importe = importe;
        Moneda = moneda;
    }

    public int IdContratacion { get; set; }
    public int IdPlanComercial { get; set; }
    public string ReferenciaContratacion { get; set; }
    public string ReferenciaOperacion { get; set; }
    public decimal Importe { get; set; }
    public string Moneda { get; set; }
}
