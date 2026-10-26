using Microsoft.AspNetCore.Mvc;
using TeamBalance.BE.Entidades;
using TeamBalance.BLL;
using Teambalance.API.Models;
using Teambalance.API.Services;

namespace Teambalance.API.Controllers;
public class TokenGestionValidacionRequest
{
    public string? TokenGestion { get; set; }
}
