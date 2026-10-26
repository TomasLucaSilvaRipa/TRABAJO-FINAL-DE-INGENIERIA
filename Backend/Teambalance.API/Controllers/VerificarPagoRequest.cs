using Microsoft.AspNetCore.Mvc;
using TeamBalance.BE.Entidades;
using TeamBalance.BLL;

namespace Teambalance.API.Controllers
{
    public sealed class VerificarPagoRequest
    {
        public string PaymentId { get; init; } = string.Empty;
    }
}
