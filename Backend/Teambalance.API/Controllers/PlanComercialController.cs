using Microsoft.AspNetCore.Mvc;
using TeamBalance.BE.Entidades;
using TeamBalance.BLL;

namespace Teambalance.API.Controllers;

[ApiController]
[Route("api/planes")]
public class PlanComercialController : ControllerBase
{
    private readonly BLLPlanComercial _planBLL;
    private readonly BLLUsuario _usuarioBLL;

    public PlanComercialController(BLLPlanComercial planBLL, BLLUsuario usuarioBLL)
    {
        _planBLL = planBLL;
        _usuarioBLL = usuarioBLL;
    }

    [HttpGet]
    public IActionResult ConsultarPlanesActivos()
    {
        try { return Ok(_planBLL.ConsultarPlanesActivos()); }
        catch (Exception ex) { return BadRequest(ex.Message); }
    }

    [HttpGet("gestion")]
    public IActionResult ConsultarPlanes()
    {
        try { ValidarSesion(); return Ok(_planBLL.ConsultarPlanes()); }
        catch (UnauthorizedAccessException ex) { return Unauthorized(ex.Message); }
        catch (Exception ex) { return BadRequest(ex.Message); }
    }

    [HttpGet("{id:int}")]
    public IActionResult ConsultarPlan(int id)
    {
        try { return Ok(_planBLL.ConsultarPlanDisponible(id)); }
        catch (KeyNotFoundException ex) { return NotFound(ex.Message); }
        catch (Exception ex) { return BadRequest(ex.Message); }
    }

    [HttpPost]
    public IActionResult RegistrarPlan([FromBody] PlanComercial plan)
    {
        try { Usuario usuario = ValidarSesion(); return Ok(_planBLL.RegistrarPlan(plan, usuario)); }
        catch (UnauthorizedAccessException ex) { return Unauthorized(ex.Message); }
        catch (Exception ex) { return BadRequest(ex.Message); }
    }

    [HttpPut("{id:int}")]
    public IActionResult ModificarPlan(int id, [FromBody] PlanComercial plan)
    {
        try { Usuario usuario = ValidarSesion(); plan.ID = id; return Ok(_planBLL.ModificarPlan(plan, usuario)); }
        catch (UnauthorizedAccessException ex) { return Unauthorized(ex.Message); }
        catch (KeyNotFoundException ex) { return NotFound(ex.Message); }
        catch (Exception ex) { return BadRequest(ex.Message); }
    }

    [HttpPatch("{id:int}/estado")]
    public IActionResult CambiarEstado(int id, [FromBody] PlanComercial plan)
    {
        try { Usuario usuario = ValidarSesion(); return Ok(_planBLL.CambiarEstado(id, plan.Activo, usuario)); }
        catch (UnauthorizedAccessException ex) { return Unauthorized(ex.Message); }
        catch (KeyNotFoundException ex) { return NotFound(ex.Message); }
        catch (Exception ex) { return BadRequest(ex.Message); }
    }

    private Usuario ValidarSesion()
    {
        string accessToken = Request.Headers.Authorization.ToString().Replace("Bearer ", string.Empty, StringComparison.OrdinalIgnoreCase).Trim();
        Usuario? usuario = _usuarioBLL.ConsultarUsuarioSesion(accessToken);
        if (usuario is null){ throw new UnauthorizedAccessException("Tu sesión ya no es válida. Volvé a iniciar sesión."); }
        return usuario;
    }
}
