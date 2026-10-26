using Microsoft.AspNetCore.Mvc;
using TeamBalance.BE.Entidades;
using TeamBalance.BLL;
using Teambalance.API.Models;
using Teambalance.API.Services;

namespace Teambalance.API.Controllers;
public sealed class RegistrarAgenciaRequest
{
    public Agencia Agencia { get; set; } = new Agencia();
    public bool RecibirNewsletter { get; set; }
    public List<int> CategoriasNewsletterIds { get; set; } = new List<int>();
}
