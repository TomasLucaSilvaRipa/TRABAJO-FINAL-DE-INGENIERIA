using System.Net;
using TeamBalance.BE.Entidades;
using TeamBalance.BLL;
using TeamBalance.MPP;
using TeamBalance.Services;

namespace Teambalance.API.Services;

public sealed class ObservacionAccesoService(
    BLLUsuario usuarios, BLLBitacora bitacora, MPPAnaliticaOperativa agencias,
    MPPNotificacion notificaciones, Seguridad seguridad)
{
    private const string Accion = "RegistrarNavegadorDeSesion";
    private static readonly object Registro = new();

    public static void Registrar(WebApplication app)
    {
        app.MapPost("/api/seguridad/observacion-acceso",
            (HttpRequest request, ObservacionAccesoService servicio) => servicio.Observar(request));
    }

    private IResult Observar(HttpRequest request)
    {
        var cabecera = request.Headers.Authorization.ToString();
        if (!cabecera.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)) return Results.Unauthorized();
        var usuario = usuarios.ConsultarUsuarioSesion(cabecera[7..].Trim());
        if (usuario is null) return Results.Unauthorized();
        if (!usuario.IdAgencia.HasValue) return Results.NoContent();
        if (!Guid.TryParseExact(request.Headers["X-TeamBalance-Browser"].ToString(), "D", out var navegador))
            return Results.BadRequest();

        var marca = seguridad.GenerarHashToken($"{usuario.ID}:{navegador:D}");
        var ipConexion = request.HttpContext.Connection.RemoteIpAddress;
        if (ipConexion?.IsIPv4MappedToIPv6 == true) ipConexion = ipConexion.MapToIPv4();

        lock (Registro)
        {
            var anteriores = bitacora.FiltrarBitacora(new FiltroBitacora(
                idAgencia: usuario.IdAgencia, idUsuario: usuario.ID, accion: Accion,
                desde: DateTime.Now.AddDays(-90)));
            if (anteriores.Any(item => item.Mensaje == marca)) return Results.NoContent();

            var registrada = bitacora.Add(new Bitacora(usuario.ID, usuario.IdAgencia, "Usuario",
                usuario.ID, Accion, marca, "Exitoso", anteriores.Count == 0 ? "Informacion" : "Advertencia", "Seguridad")
            {
                DireccionIP = ipConexion is not null && !IPAddress.IsLoopback(ipConexion) ? ipConexion.ToString() : null
            });

            if (registrada && anteriores.Count > 0)
            {
                var correos = agencias.ConsultarDuenosAgencia(usuario).Select(item => item.Usuario.Email)
                    .Where(email => !string.IsNullOrWhiteSpace(email)).Distinct(StringComparer.OrdinalIgnoreCase);
                foreach (var correo in correos)
                    notificaciones.RegistrarPorEmail(new NotificacionUsuario
                    {
                        EmailDestinatario = correo,
                        Titulo = "Acceso desde un navegador nuevo",
                        Mensaje = $"La cuenta de {usuario.Nombre} {usuario.Apellido} abrió TeamBalance desde un navegador sin registro reciente. Si no reconoce el acceso, revise las sesiones activas."
                    });
            }
        }
        return Results.NoContent();
    }
}
