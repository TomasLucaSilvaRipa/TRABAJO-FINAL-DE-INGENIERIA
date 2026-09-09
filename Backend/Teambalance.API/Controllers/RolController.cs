using Microsoft.AspNetCore.Mvc;
using TeamBalance.BE.Entidades;
using TeamBalance.BLL;

namespace Teambalance.API.Controllers;

[Route("api/roles")]
public class RolController : ControllerBase
{
    private readonly BLLRol _rolBLL;
    private readonly BLLUsuario _usuarioBLL;

    public RolController(BLLRol rolBLL, BLLUsuario usuarioBLL)
    {
        _rolBLL = rolBLL;
        _usuarioBLL = usuarioBLL;
    }

    [HttpGet("asignables-agencia")]
    public IActionResult ConsultarRolesAsignablesAgencia()
    {
        try
        {
            Usuario solicitante = ObtenerSolicitante();
            if (!_rolBLL.TienePermiso(solicitante, "GestionarUsuarios")){ return Unauthorized("No tenés permiso para gestionar usuarios."); }
            return Ok(_rolBLL.ConsultarRolesActivos().Where(rol => !string.Equals(rol.Nombre, "Soporte", StringComparison.OrdinalIgnoreCase)));
        }
        catch (UnauthorizedAccessException ex){ return Unauthorized(ex.Message); }
    }

    [HttpGet]
    public IActionResult ConsultarRoles()
    {
        try { ValidarGestionRoles(); return Ok(_rolBLL.ConsultarRolesActivos()); }
        catch (UnauthorizedAccessException ex){ return Unauthorized(ex.Message); }
    }

    [HttpGet("permisos")]
    public IActionResult ConsultarPermisos()
    {
        try { ValidarGestionRoles(); return Ok(_rolBLL.ConsultarPermisosActivos()); }
        catch (UnauthorizedAccessException ex){ return Unauthorized(ex.Message); }
    }

    [HttpPost]
    public IActionResult RegistrarRol([FromBody] Rol rol)
    {
        try { ValidarGestionRoles(); return Ok(_rolBLL.RegistrarRol(rol)); }
        catch (UnauthorizedAccessException ex){ return Unauthorized(ex.Message); }
        catch (ArgumentException ex){ return BadRequest(ex.Message); }
        catch (Exception){ return StatusCode(500, "No fue posible registrar el rol."); }
    }

    [HttpPut("{idRol:int}")]
    public IActionResult ModificarRol(int idRol, [FromBody] Rol rol)
    {
        try { ValidarGestionRoles(); rol.ID = idRol; _rolBLL.ModificarRol(rol); return NoContent(); }
        catch (UnauthorizedAccessException ex){ return Unauthorized(ex.Message); }
        catch (ArgumentException ex){ return BadRequest(ex.Message); }
        catch (Exception){ return StatusCode(500, "No fue posible modificar el rol."); }
    }

    [HttpPatch("{idRol:int}/estado")]
    public IActionResult CambiarEstado(int idRol, [FromQuery] bool activo)
    {
        try { ValidarGestionRoles(); _rolBLL.CambiarEstado(new Rol() { ID = idRol }, activo); return NoContent(); }
        catch (UnauthorizedAccessException ex){ return Unauthorized(ex.Message); }
        catch (Exception){ return StatusCode(500, "No fue posible actualizar el rol."); }
    }

    private void ValidarGestionRoles()
    {
        Usuario solicitante = ObtenerSolicitante();
        if (!_rolBLL.TienePermiso(solicitante, "GestionarRoles"))
        {
            throw new UnauthorizedAccessException("No tenés permiso para gestionar roles.");
        }
    }

    private Usuario ObtenerSolicitante()
    {
        string? authorization = Request.Headers.Authorization;
        string accessToken = !string.IsNullOrWhiteSpace(authorization) && authorization.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase) ? authorization[7..].Trim() : string.Empty;
        return _usuarioBLL.ConsultarUsuarioSesion(accessToken) ?? throw new UnauthorizedAccessException("Tu sesión ya no es válida. Volvé a iniciar sesión.");
    }
}
