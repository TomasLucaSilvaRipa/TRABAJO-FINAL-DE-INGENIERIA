namespace TeamBalance.BE.Entidades;
public sealed class ConfiguracionRenovacionAutomatica
{
    public ConfiguracionRenovacionAutomatica()
    {
    }

    public ConfiguracionRenovacionAutomatica(string publicKey, decimal importePrueba, string monedaPrueba, string nombrePlan)
    {
        PublicKey = publicKey;
        ImportePrueba = importePrueba;
        MonedaPrueba = monedaPrueba;
        NombrePlan = nombrePlan;
    }

    public string PublicKey { get; set; } = string.Empty;
    public decimal ImportePrueba { get; set; }
    public string MonedaPrueba { get; set; } = "ARS";
    public string NombrePlan { get; set; } = string.Empty;
}
