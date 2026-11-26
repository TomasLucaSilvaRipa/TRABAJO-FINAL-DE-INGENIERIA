using Microsoft.Data.SqlClient;
using System.Data;
using TeamBalance.BE.Entidades;
using TeamBalance.BLL;
using TeamBalance.DAL;
using TeamBalance.Services;

namespace Teambalance.API.Services;

public sealed class SesionesRemotasService(Conexion conexion, IConfiguration configuracion, BLLUsuario usuarios, Seguridad seguridad)
{
    private readonly string _baseDatos = new SqlConnectionStringBuilder(configuracion.GetConnectionString("TeamBalanceDB")).InitialCatalog;

    public static void Registrar(WebApplication app)
    {
        var rutas = app.MapGroup("/api/seguridad/sesiones-remotas");
        rutas.MapGet("", (HttpRequest request, SesionesRemotasService servicio) => servicio.Consultar(request));
        rutas.MapDelete("{id:int:min(1)}", (int id, HttpRequest request, SesionesRemotasService servicio) => servicio.Revocar(request, id));
        rutas.MapDelete("", (HttpRequest request, SesionesRemotasService servicio) => servicio.Revocar(request, null));
    }

    private (Usuario? Usuario, string TokenHash) Identificar(HttpRequest request)
    {
        var cabecera = request.Headers.Authorization.ToString();
        if (!cabecera.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)) return (null, "");
        var token = cabecera[7..].Trim();
        var usuario = usuarios.ConsultarUsuarioSesion(token);
        return usuario is null ? (null, "") : (usuario, seguridad.GenerarHashToken(token));
    }

    private async Task<IResult> Consultar(HttpRequest request)
    {
        var (usuario, hash) = Identificar(request);
        if (usuario is null) return Results.Unauthorized();

        const string sql = """
            SELECT s.ID, s.FechaInicio, s.FechaUltimaActividad, s.FechaExpiracion,
                   CAST(CASE WHEN s.TokenHash = @TokenHash THEN 1 ELSE 0 END AS bit) AS Actual
            FROM dbo.SesionUsuario s JOIN dbo.Usuario u ON u.ID = s.IdUsuario
            WHERE s.IdUsuario = @IdUsuario AND s.Activa = 1 AND s.FechaExpiracion >= SYSDATETIME()
              AND u.Activo = 1 AND s.VersionAutorizacion = u.VersionAutorizacion
            ORDER BY s.FechaUltimaActividad DESC;
            """;
        var filas = await conexion.LeerAdministracionAsync(sql, [new("@IdUsuario", usuario.ID), new("@TokenHash", hash)], _baseDatos, 10);
        return Results.Ok(filas.Rows.Cast<DataRow>().Select(fila => new
        {
            id = (int)fila["ID"],
            inicio = (DateTime)fila["FechaInicio"],
            ultimaActividad = fila.Field<DateTime?>("FechaUltimaActividad"),
            vencimiento = (DateTime)fila["FechaExpiracion"],
            actual = (bool)fila["Actual"]
        }));
    }

    private async Task<IResult> Revocar(HttpRequest request, int? id)
    {
        var (usuario, _) = Identificar(request);
        if (usuario is null) return Results.Unauthorized();

        const string sql = """
            UPDATE dbo.SesionUsuario SET Activa = 0
            WHERE IdUsuario = @IdUsuario AND Activa = 1 AND (@Id IS NULL OR ID = @Id);
            """;
        await conexion.EjecutarAdministracionAsync(sql,
            [new("@IdUsuario", usuario.ID), new("@Id", SqlDbType.Int) { Value = (object?)id ?? DBNull.Value }], _baseDatos, 10);
        return Results.NoContent();
    }
}
