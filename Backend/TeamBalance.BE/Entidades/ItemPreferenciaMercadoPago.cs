using System.Text.Json.Serialization;

namespace TeamBalance.BE.Entidades;
public class ItemPreferenciaMercadoPago
{
    public ItemPreferenciaMercadoPago(string titulo, int cantidad, string moneda, decimal importeUnitario)
    {
        Titulo = titulo;
        Cantidad = cantidad;
        Moneda = moneda;
        ImporteUnitario = importeUnitario;
    }

    [JsonPropertyName("title")]
    public string Titulo { get; set; }

    [JsonPropertyName("quantity")]
    public int Cantidad { get; set; }

    [JsonPropertyName("currency_id")]
    public string Moneda { get; set; }

    [JsonPropertyName("unit_price")]
    public decimal ImporteUnitario { get; set; }
}
