namespace TeamBalance.BE.Entidades;

public sealed record ContratacionPendiente(
    int IdContratacion,
    int IdPlanComercial,
    string ReferenciaContratacion,
    string ReferenciaOperacion,
    decimal Importe,
    string Moneda);
