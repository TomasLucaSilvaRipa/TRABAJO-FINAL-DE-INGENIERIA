using Microsoft.AspNetCore.Mvc;
using TeamBalance.BE.Entidades;
using TeamBalance.BLL;

namespace Teambalance.API.Controllers;

[ApiController]
[Route("api/registro-horas")]
public class RegistroHorasController : ControllerBase
{
    private readonly BLLRegistroHora _registroHoraBLL;
    private readonly BLLUsuario _usuarioBLL;

    public RegistroHorasController(BLLRegistroHora registroHoraBLL, BLLUsuario usuarioBLL)
    {
        _registroHoraBLL = registroHoraBLL;
        _usuarioBLL = usuarioBLL;
    }

    [HttpGet]
    public IActionResult ConsultarPropios()
    {
        try
        {
            List<RegistroHora> registros = _registroHoraBLL.ConsultarPropios(ValidarSesion());
            return Ok(registros);
        }
        catch (UnauthorizedAccessException ex) { return Unauthorized(ex.Message); }
        catch (Exception ex) { return StatusCode(500, ex.Message); }
    }

    [HttpPost]
    public IActionResult Registrar([FromBody] RegistroHora registro)
    {
        try
        {
            RegistroHora resultado = _registroHoraBLL.Registrar(registro, ValidarSesion());
            return Ok(resultado);
        }
        catch (UnauthorizedAccessException ex) { return Unauthorized(ex.Message); }
        catch (ArgumentException ex) { return BadRequest(ex.Message); }
        catch (Exception ex) { return StatusCode(500, ex.Message); }
    }

    [HttpPost("previsualizar")]
    public IActionResult Previsualizar([FromBody] List<RegistroHora> registros)
    {
        try
        {
            List<RegistroHora> resultado = _registroHoraBLL.Previsualizar(registros, ValidarSesion());
            return Ok(resultado);
        }
        catch (UnauthorizedAccessException ex) { return Unauthorized(ex.Message); }
        catch (ArgumentException ex) { return BadRequest(ex.Message); }
        catch (Exception ex) { return StatusCode(500, ex.Message); }
    }

    [HttpPost("imputaciones")]
    public IActionResult RegistrarImputaciones([FromBody] List<RegistroHora> registros)
    {
        try
        {
            List<RegistroHora> resultado = _registroHoraBLL.RegistrarImputaciones(registros, ValidarSesion());
            return Ok(resultado);
        }
        catch (UnauthorizedAccessException ex) { return Unauthorized(ex.Message); }
        catch (ArgumentException ex) { return BadRequest(ex.Message); }
        catch (Exception ex) { return StatusCode(500, ex.Message); }
    }

    private Usuario ValidarSesion()
    {
        try
        {
            string token = Request.Headers.Authorization.ToString().Replace("Bearer ", string.Empty, StringComparison.OrdinalIgnoreCase).Trim();
            Usuario? usuario = _usuarioBLL.ConsultarUsuarioSesion(token);
            if (usuario is null) { throw new UnauthorizedAccessException("Tu sesión ya no es válida."); }
            return usuario;
        }
        catch (UnauthorizedAccessException ex) { throw new UnauthorizedAccessException(ex.Message); }
        catch (Exception ex) { throw new Exception(ex.Message); }
    }
}
