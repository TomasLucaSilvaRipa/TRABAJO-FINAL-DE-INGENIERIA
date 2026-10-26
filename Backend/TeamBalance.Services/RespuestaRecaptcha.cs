namespace TeamBalance.Services;

internal class RespuestaRecaptcha
{
    public bool Success { get; set; }
    public decimal Score { get; set; }
    public string? Action { get; set; }
    public string? Hostname { get; set; }
}
