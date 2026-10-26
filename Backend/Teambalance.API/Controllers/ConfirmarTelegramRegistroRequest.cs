using Microsoft.AspNetCore.Mvc;
using TeamBalance.BE.Entidades;
using TeamBalance.BLL;
using Teambalance.API.Models;
using Teambalance.API.Services;

namespace Teambalance.API.Controllers;
public sealed class ConfirmarTelegramRegistroRequest : TokenGestionValidacionRequest
{
    public string? Codigo { get; set; }
}
