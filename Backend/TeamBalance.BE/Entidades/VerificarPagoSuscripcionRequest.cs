namespace TeamBalance.BE.Entidades;
public sealed class VerificarPagoSuscripcionRequest
{
    public string ReferenciaOperacion { get; set; } = string.Empty;
    public string PaymentId { get; set; } = string.Empty;
}
