using Microsoft.AspNetCore.Mvc;
using TeamBalance.BLL;
using TeamBalance.Services;

namespace Teambalance.API.Controllers;
public class PasswordRequest
{
    public string Password { get; set; } = string.Empty;
}
