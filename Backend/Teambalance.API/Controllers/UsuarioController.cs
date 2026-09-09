using Microsoft.AspNetCore.Mvc;
using TeamBalance.BE.Entidades;
using TeamBalance.BLL;

namespace Teambalance.API.Controllers;

[Route("api/usuarios")]
public class UsuarioController : ControllerBase
{
    private readonly BLLUsuario _usuarioBLL;
    private readonly BLLRol _rolBLL;

    public UsuarioController(BLLUsuario usuarioBLL, BLLRol rolBLL)
    {
        _usuarioBLL = usuarioBLL;
        _rolBLL = rolBLL;
    }

    [HttpGet("agencia")]
    public IActionResult ConsultarUsuariosAgencia()
    {
        try
        {
            Usuario solicitante = ObtenerSolicitante();
            return Ok(_usuarioBLL.ConsultarUsuariosAgencia(solicitante));
        }
        catch (UnauthorizedAccessException ex){ return Unauthorized(ex.Message); }
        catch (Exception){ return StatusCode(500, "No fue posible consultar los usuarios de la agencia."); }
    }

    [HttpGet("soporte-inicial-disponible")]
    public IActionResult ConsultarSoporteInicialDisponible()
    {
        try { return Ok(new { disponible = _usuarioBLL.SoporteInicialDisponible(ObtenerSolicitante()) }); }
        catch (UnauthorizedAccessException ex){ return Unauthorized(ex.Message); }
    }

    [HttpGet("rol-soporte-inicial")]
    public IActionResult ConsultarRolSoporteInicial()
    {
        try
        {
            Usuario solicitante = ObtenerSolicitante();
            if (!_usuarioBLL.SoporteInicialDisponible(solicitante))
            {
                return NotFound("El usuario inicial de Soporte ya fue creado.");
            }
            return Ok(_rolBLL.ConsultarRolPorNombre("Soporte"));
        }
        catch (UnauthorizedAccessException ex){ return Unauthorized(ex.Message); }
        catch (KeyNotFoundException ex){ return NotFound(ex.Message); }
    }

    [HttpPost("agencia")]
    public async Task<IActionResult> RegistrarUsuarioAgencia([FromBody] Usuario usuario)
    {
        try
        {
            Usuario solicitante = ObtenerSolicitante();
            Usuario usuarioRegistrado = await _usuarioBLL.RegistrarUsuarioAgencia(usuario, solicitante);
            return Ok(usuarioRegistrado);
        }
        catch (UnauthorizedAccessException ex){ return Unauthorized(ex.Message); }
        catch (ArgumentException ex){ return BadRequest(ex.Message); }
        catch (InvalidOperationException ex){ return BadRequest(ex.Message); }
        catch (Exception){ return StatusCode(500, "No fue posible registrar el usuario."); }
    }

    [HttpPut("agencia/{idUsuario:int}")]
    public IActionResult ModificarUsuarioAgencia(int idUsuario, [FromBody] Usuario usuario)
    {
        try
        {
            usuario.ID = idUsuario;
            _usuarioBLL.ModificarUsuarioAgencia(usuario, ObtenerSolicitante());
            return NoContent();
        }
        catch (UnauthorizedAccessException ex){ return Unauthorized(ex.Message); }
        catch (KeyNotFoundException ex){ return NotFound(ex.Message); }
        catch (ArgumentException ex){ return BadRequest(ex.Message); }
        catch (Exception){ return StatusCode(500, "No fue posible modificar el usuario."); }
    }

    [HttpPatch("agencia/{idUsuario:int}/estado")]
    public IActionResult CambiarEstadoUsuarioAgencia(int idUsuario, [FromQuery] bool activo)
    {
        try
        {
            _usuarioBLL.CambiarEstadoUsuarioAgencia(idUsuario, activo, ObtenerSolicitante());
            return NoContent();
        }
        catch (UnauthorizedAccessException ex){ return Unauthorized(ex.Message); }
        catch (KeyNotFoundException ex){ return NotFound(ex.Message); }
        catch (Exception){ return StatusCode(500, "No fue posible actualizar el usuario."); }
    }

    private Usuario ObtenerSolicitante()
    {
        string? authorization = Request.Headers.Authorization;
        string accessToken = !string.IsNullOrWhiteSpace(authorization) && authorization.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase) ? authorization[7..].Trim() : string.Empty;
        return _usuarioBLL.ConsultarUsuarioSesion(accessToken) ?? throw new UnauthorizedAccessException("Tu sesión ya no es válida. Volvé a iniciar sesión.");
    }
}
