using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using TeamBalance.BE.Entidades;
using Microsoft.Extensions.Configuration;

namespace TeamBalance.Services
{
    public sealed record MercadoPagoPreapproval(string Id, string Status, string InitPoint, string ExternalReference);
}
