using Microsoft.AspNetCore.Mvc;
using TeamBalance.BE.Entidades;
using TeamBalance.BLL;

namespace Teambalance.API.Controllers;

[ApiController]
[Route("api/riesgos-retraso")]
public class RiesgosRetrasoController : ControllerBase
{
    private readonly BLLRiesgoRetraso _riesgoBLL;
    private readonly BLLUsuario _usuarioBLL;

    public RiesgosRetrasoController(BLLRiesgoRetraso riesgoBLL, BLLUsuario usuarioBLL)
    {
        _riesgoBLL = riesgoBLL;
        _usuarioBLL = usuarioBLL;
    }

    [HttpPost("consultar")]
    public IActionResult Consultar([FromBody] FiltroRiesgoRetraso filtro)
    {
        try
        {
            ResultadoRiesgoRetraso resultado = _riesgoBLL.ConsultarRiesgo(filtro, ValidarSesion());
            return Ok(new
            {
                Proyectos = resultado.Proyectos.Select(detalle => new
                {
                    IdProyecto = detalle.Proyecto.ID,
                    NombreProyecto = detalle.Proyecto.Nombre,
                    NombreCliente = string.IsNullOrWhiteSpace(detalle.Proyecto.NombreCliente)
                        ? "Cliente #" + detalle.Proyecto.IdCliente : detalle.Proyecto.NombreCliente,
                    NombrePM = string.IsNullOrWhiteSpace(detalle.Proyecto.NombrePMResponsable)
                        ? "PM #" + detalle.Proyecto.IdPMResponsable : detalle.Proyecto.NombrePMResponsable,
                    FechaInicio = detalle.Proyecto.FechaInicio ?? detalle.Proyecto.FechaAlta,
                    detalle.Proyecto.Deadline,
                    detalle.FechaEstimadaFinalizacion,
                    detalle.DiasPosibleRetraso,
                    detalle.HorasRestantes,
                    detalle.DisponibilidadEquipo,
                    detalle.RitmoAvance,
                    detalle.NivelRiesgo,
                    detalle.Causas,
                    TareasPendientes = detalle.TareasPendientes.Select(pendiente => new
                    {
                        IdTarea = pendiente.Tarea.ID,
                        pendiente.Tarea.Titulo,
                        Estado = pendiente.Tarea.Estado ?? "Sin estado",
                        pendiente.Tarea.Bloqueada,
                        pendiente.Tarea.FechaInicio,
                        pendiente.Tarea.Deadline,
                        pendiente.HorasEstimadas,
                        pendiente.PorcentajeAvance,
                        pendiente.HorasRestantes
                    }).ToList(),
                    HitosTareas = detalle.HitosTareas.Select(hito => new
                    {
                        IdTarea = hito.Tarea.ID,
                        hito.Tarea.Titulo,
                        hito.Tarea.FechaInicio,
                        hito.Tarea.Deadline,
                        hito.HorasEstimadas,
                        hito.PorcentajeAvance,
                        hito.HorasRestantes
                    }).ToList()
                }).ToList()
            });
        }
        catch (UnauthorizedAccessException ex) { return Unauthorized(ex.Message); }
        catch (ArgumentException ex) { return BadRequest(ex.Message); }
        catch (Exception ex) { return StatusCode(500, ex.Message); }
    }

    [HttpPost("seguimientos")]
    public async Task<IActionResult> RegistrarSeguimiento([FromBody] NuevoSeguimientoRiesgoRetraso seguimiento)
    {
        try { return Ok(await _riesgoBLL.RegistrarSeguimiento(ValidarSesion(), seguimiento)); }
        catch (UnauthorizedAccessException ex) { return Unauthorized(ex.Message); }
        catch (ArgumentException ex) { return BadRequest(ex.Message); }
        catch (KeyNotFoundException ex) { return NotFound(ex.Message); }
        catch (InvalidOperationException ex) { return BadRequest(ex.Message); }
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
