namespace TeamBalance.BE.Entidades;
public sealed class ConfiguracionRenovacionAutomatica
{
    public ConfiguracionRenovacionAutomatica(string publicKey, decimal importePrueba, string monedaPrueba, PlanComercial planComercial)
    {
        PublicKey = publicKey;
        ImportePrueba = importePrueba;
        MonedaPrueba = monedaPrueba;
        PlanComercial = planComercial;
    }

    public string PublicKey { get; set; } = string.Empty;
    public decimal ImportePrueba { get; set; }
    public string MonedaPrueba { get; set; } = "ARS";
    [System.Text.Json.Serialization.JsonIgnore]
    public PlanComercial PlanComercial { get; }

    public string NombrePlan => PlanComercial.Nombre;
}
