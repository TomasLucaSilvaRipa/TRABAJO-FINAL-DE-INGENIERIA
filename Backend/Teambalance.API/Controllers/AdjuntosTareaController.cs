using Teambalance.API.Models;
using Microsoft.AspNetCore.Mvc;
using TeamBalance.BE.Entidades;
using TeamBalance.BLL;

namespace Teambalance.API.Controllers;

[ApiController]
[Route("api/tareas/{idTarea:int}/adjuntos")]
public class AdjuntosTareaController : ControllerBase
{
    private readonly BLLAdjuntoTarea _adjuntos;
    private readonly BLLUsuario _usuarios;

    public AdjuntosTareaController(BLLAdjuntoTarea adjuntos, BLLUsuario usuarios)
    {
        _adjuntos = adjuntos;
        _usuarios = usuarios;
    }

    [HttpGet]
    public IActionResult Consultar([ReferenciaHttp("idTarea")] Tarea tarea)
    {
        try { return Ok(_adjuntos.Consultar(tarea, Sesion())); }
        catch (UnauthorizedAccessException ex) { return StatusCode(403, ex.Message); }
    }

    [HttpGet("{id:int}/contenido")]
    public IActionResult Descargar([ReferenciaHttp("idTarea")] Tarea tarea, [ReferenciaHttp("id")] ArchivoAdjuntoTarea adjunto)
    {
        try
        {
            adjunto.Tarea = tarea;
            ArchivoAdjuntoTarea archivo = _adjuntos.ConsultarDetalle(adjunto, Sesion());
            string ruta = _adjuntos.RutaFisica(archivo);
            if (!System.IO.File.Exists(ruta))
                return NotFound("El archivo físico ya no está disponible.");
            return PhysicalFile(ruta, archivo.TipoContenido, archivo.NombreOriginal, enableRangeProcessing: true);
        }
        catch (UnauthorizedAccessException ex) { return StatusCode(403, ex.Message); }
        catch (FileNotFoundException ex) { return NotFound(ex.Message); }
    }

    [HttpPost, RequestSizeLimit(26_214_400)]
    public async Task<IActionResult> Registrar([ReferenciaHttp("idTarea")] Tarea tarea, IFormFile archivo)
    {
        try
        {
            if (archivo is null)
                return BadRequest("Seleccioná un archivo.");
            CargaAdjuntoTarea carga = new()
            {
                Tarea = tarea,
                Archivo = new ArchivoAdjuntoTarea
                {
                    NombreOriginal = archivo.FileName,
                    TipoContenido = archivo.ContentType,
                    TamanoBytes = archivo.Length
                },
                Contenido = archivo.OpenReadStream()
            };
            return Ok(await _adjuntos.Registrar(carga, Sesion()));
        }
        catch (UnauthorizedAccessException ex) { return StatusCode(403, ex.Message); }
        catch (ArgumentException ex) { return BadRequest(ex.Message); }
        catch (Exception ex) { return BadRequest(ex.Message); }
    }

    [HttpDelete("{id:int}")]
    public IActionResult Eliminar([ReferenciaHttp("idTarea")] Tarea tarea, [ReferenciaHttp("id")] ArchivoAdjuntoTarea adjunto)
    {
        try
        {
            adjunto.Tarea = tarea;
            return Ok(_adjuntos.Eliminar(adjunto, Sesion()));
        }
        catch (UnauthorizedAccessException ex) { return StatusCode(403, ex.Message); }
        catch (Exception ex) { return BadRequest(ex.Message); }
    }

    private Usuario Sesion()
    {
        string token = Request.Headers.Authorization.ToString().Replace("Bearer ", string.Empty, StringComparison.OrdinalIgnoreCase).Trim();
        return _usuarios.ConsultarUsuarioSesion(token) ?? throw new UnauthorizedAccessException("Tu sesión ya no es válida.");
    }
}
