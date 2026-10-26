using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using TeamBalance.BE.Entidades;
using Microsoft.Extensions.Configuration;

public sealed class MercadoPagoPreferenceRequest
{
    public MercadoPagoPreferenceRequest(string titulo, decimal importe, string moneda, string referenciaExterna, string? codigoPlan = null, bool usarImporteExacto = false)
    {
        Titulo = titulo;
        Importe = importe;
        Moneda = moneda;
        ReferenciaExterna = referenciaExterna;
        CodigoPlan = codigoPlan;
        UsarImporteExacto = usarImporteExacto;
    }

    public string Titulo { get; }
    public decimal Importe { get; }
    public string Moneda { get; }
    public string ReferenciaExterna { get; }
    public string? CodigoPlan { get; }
    public bool UsarImporteExacto { get; }
}
