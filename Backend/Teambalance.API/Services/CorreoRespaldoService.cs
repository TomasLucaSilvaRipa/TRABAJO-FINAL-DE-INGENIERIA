using System.Collections.Concurrent;
using System.Data;
using System.Net.Mail;
using System.Security.Cryptography;
using Microsoft.Data.SqlClient;
using TeamBalance.BE.Entidades;
using TeamBalance.BLL;
using TeamBalance.DAL;
using TeamBalance.MPP;
using TeamBalance.Services;

namespace Teambalance.API.Services;

public sealed record SolicitudCorreoRespaldo(string Email, string Password);
public sealed record ConfirmacionCorreoRespaldo(string Token);
public sealed record SolicitudRecuperoRespaldo(string Email);

public sealed class CorreoRespaldoService(
    Conexion conexion,
    IConfiguration configuracion,
    BLLUsuario usuarios,
    MPPUsuario usuariosMPP,
    Seguridad seguridad,
    EmailService correo)
{
    private static readonly ConcurrentDictionary<string, DateTimeOffset> UltimaSolicitud = new(StringComparer.OrdinalIgnoreCase);
    private readonly string _baseDatos = new SqlConnectionStringBuilder(configuracion.GetConnectionString("TeamBalanceDB")).InitialCatalog;
    private readonly string? _emisor = configuracion["Email:Emisor"];
    private readonly string? _claveAplicacion = configuracion["Email:ClaveAplicacion"];
    private readonly string? _urlFrontend = configuracion["Frontend:PublicBaseUrl"];

    public static void Registrar(WebApplication app)
    {
        var rutas = app.MapGroup("/api/seguridad/correo-respaldo");
        rutas.MapGet("", (HttpRequest request, CorreoRespaldoService servicio) => servicio.Consultar(request));
        rutas.MapPost("", (SolicitudCorreoRespaldo solicitud, HttpRequest request, CorreoRespaldoService servicio) => servicio.SolicitarVerificacion(solicitud, request));
        rutas.MapPost("/confirmar", (ConfirmacionCorreoRespaldo solicitud, CorreoRespaldoService servicio) => servicio.Confirmar(solicitud));
        rutas.MapPost("/recuperar", (SolicitudRecuperoRespaldo solicitud, HttpRequest request, CorreoRespaldoService servicio) => servicio.SolicitarRecupero(solicitud, request));
    }

    private Usuario? Identificar(HttpRequest request)
    {
        var cabecera = request.Headers.Authorization.ToString();
        if (!cabecera.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)) return null;
        return usuarios.ConsultarUsuarioSesion(cabecera[7..].Trim());
    }

    private async Task AsegurarTabla()
    {
        const string sql = """
            IF OBJECT_ID(N'dbo.CorreoRespaldoUsuario', N'U') IS NULL
            BEGIN
                CREATE TABLE dbo.CorreoRespaldoUsuario
                (
                    IdUsuario INT NOT NULL CONSTRAINT PK_CorreoRespaldoUsuario PRIMARY KEY,
                    CorreoVerificado NVARCHAR(320) NULL,
                    CorreoPendiente NVARCHAR(320) NULL,
                    TokenVerificacionHash NVARCHAR(500) NULL,
                    FechaExpiracion DATETIME2 NULL,
                    FechaActualizacion DATETIME2 NOT NULL CONSTRAINT DF_CorreoRespaldoUsuario_Fecha DEFAULT(SYSUTCDATETIME())
                );
            END;
            IF EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.CorreoRespaldoUsuario') AND name = N'UX_CorreoRespaldoUsuario_CorreoVerificado')
                DROP INDEX UX_CorreoRespaldoUsuario_CorreoVerificado ON dbo.CorreoRespaldoUsuario;
            """;
        await conexion.EjecutarAdministracionAsync(sql, [], _baseDatos, 10);
    }

    private async Task<IResult> Consultar(HttpRequest request)
    {
        var usuario = Identificar(request);
        if (usuario is null) return Results.Unauthorized();
        await AsegurarTabla();
        const string sql = "SELECT CorreoVerificado FROM dbo.CorreoRespaldoUsuario WHERE IdUsuario = @IdUsuario;";
        var filas = await conexion.LeerAdministracionAsync(sql, [new("@IdUsuario", usuario.ID)], _baseDatos, 10);
        var email = filas.Rows.Count == 0 || filas.Rows[0].IsNull("CorreoVerificado") ? null : (string)filas.Rows[0]["CorreoVerificado"];
        return Results.Ok(new { emailVerificado = email is not null, email });
    }

    private async Task<IResult> SolicitarVerificacion(SolicitudCorreoRespaldo solicitud, HttpRequest request)
    {
        var usuarioSesion = Identificar(request);
        if (usuarioSesion is null) return Results.Unauthorized();

        var email = NormalizarEmail(solicitud.Email);
        if (email is null || string.IsNullOrWhiteSpace(solicitud.Password)) return Results.BadRequest(new { codigo = "backupEmail.invalidRequest" });

        var usuario = usuariosMPP.ConsultarUsuarioPorId(new Usuario(usuarioSesion.ID));
        if (usuario is null || !seguridad.VerificarPassword(solicitud.Password, usuario.PasswordHash))
            return Results.BadRequest(new { codigo = "backupEmail.passwordIncorrect" });
        if (string.Equals(email, usuario.Email, StringComparison.OrdinalIgnoreCase))
            return Results.BadRequest(new { codigo = "backupEmail.sameAsAccount" });
        await AsegurarTabla();
        var token = NuevoToken();
        var hash = seguridad.GenerarHashToken(token);
        var vence = DateTime.UtcNow.AddMinutes(20);
        const string guardar = """
            UPDATE dbo.CorreoRespaldoUsuario WITH (UPDLOCK, SERIALIZABLE)
            SET CorreoPendiente = @Email, TokenVerificacionHash = @Hash,
                FechaExpiracion = @Expira, FechaActualizacion = SYSUTCDATETIME()
            WHERE IdUsuario = @IdUsuario;
            IF @@ROWCOUNT = 0
                INSERT INTO dbo.CorreoRespaldoUsuario(IdUsuario, CorreoPendiente, TokenVerificacionHash, FechaExpiracion)
                VALUES(@IdUsuario, @Email, @Hash, @Expira);
            """;
        await conexion.EjecutarAdministracionAsync(guardar,
            [new("@IdUsuario", usuario.ID), new("@Email", email), new("@Hash", hash), new("@Expira", vence)], _baseDatos, 10);

        var enviado = await EnviarVerificacion(usuario, email, token);
        if (!enviado)
        {
            const string limpiar = "UPDATE dbo.CorreoRespaldoUsuario SET CorreoPendiente = NULL, TokenVerificacionHash = NULL, FechaExpiracion = NULL WHERE IdUsuario = @IdUsuario AND TokenVerificacionHash = @Hash;";
            await conexion.EjecutarAdministracionAsync(limpiar, [new("@IdUsuario", usuario.ID), new("@Hash", hash)], _baseDatos, 10);
            return Results.Problem(statusCode: 503, extensions: new Dictionary<string, object?> { ["codigo"] = "backupEmail.sendError" });
        }
        return Results.Ok(new { codigo = "backupEmail.verificationSent" });
    }

    private async Task<bool> EnviarVerificacion(Usuario usuario, string email, string token)
    {
        if (string.IsNullOrWhiteSpace(_emisor) || string.IsNullOrWhiteSpace(_claveAplicacion) ||
            !Uri.TryCreate(_urlFrontend, UriKind.Absolute, out var urlBase) ||
            (urlBase.Scheme != Uri.UriSchemeHttps && urlBase.Scheme != Uri.UriSchemeHttp))
            return false;

        var enlace = $"{_urlFrontend!.TrimEnd('/')}/verificar-correo-respaldo?token={Uri.EscapeDataString(token)}";
        var nombre = System.Net.WebUtility.HtmlEncode(string.IsNullOrWhiteSpace(usuario.Nombre) ? "Usuario" : usuario.Nombre.Trim());
        var enlaceHtml = System.Net.WebUtility.HtmlEncode(enlace);
        var emailHtml = System.Net.WebUtility.HtmlEncode(email);
        var html = $"<!doctype html><html lang='es'><meta charset='utf-8'><body style='font-family:Arial,sans-serif;color:#163243'><h2>TeamBalance</h2><p>Hola {nombre},</p><p>Se solicitó agregar {emailHtml} como correo de respaldo de tu cuenta.</p><p><a href='{enlaceHtml}'>Verificar correo de respaldo</a></p><p>El enlace vence en 20 minutos. El correo actual sigue activo hasta que confirmes este cambio.</p><p>Si no realizaste esta solicitud, podés ignorar este mensaje.</p></body></html>";

        try
        {
            using var smtp = new SmtpClient("smtp.gmail.com")
            {
                Port = 587,
                Credentials = new System.Net.NetworkCredential(_emisor, _claveAplicacion),
                EnableSsl = true
            };
            using var mensaje = new MailMessage
            {
                From = new MailAddress(_emisor, "TeamBalance"),
                To = { new MailAddress(email) },
                Subject = "Verificá tu correo de respaldo de TeamBalance",
                Body = html,
                IsBodyHtml = true
            };
            await smtp.SendMailAsync(mensaje);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private async Task<IResult> Confirmar(ConfirmacionCorreoRespaldo solicitud)
    {
        if (string.IsNullOrWhiteSpace(solicitud.Token)) return Results.BadRequest(new { codigo = "backupEmail.invalidLink" });
        await AsegurarTabla();
        const string sql = """
            SET XACT_ABORT ON;
            SET NOCOUNT ON;
            BEGIN TRANSACTION;
            DECLARE @IdUsuario INT, @Email NVARCHAR(320);
            SELECT TOP (1) @IdUsuario = IdUsuario, @Email = CorreoPendiente
            FROM dbo.CorreoRespaldoUsuario WITH (UPDLOCK, HOLDLOCK)
            WHERE TokenVerificacionHash = @Hash AND FechaExpiracion >= SYSUTCDATETIME()
              AND CorreoPendiente IS NOT NULL;
            IF @IdUsuario IS NULL
            BEGIN
                ROLLBACK TRANSACTION;
                SELECT CAST(0 AS BIT) AS Confirmado;
                RETURN;
            END;
            UPDATE dbo.CorreoRespaldoUsuario
            SET CorreoVerificado = @Email, CorreoPendiente = NULL,
                TokenVerificacionHash = NULL, FechaExpiracion = NULL,
                FechaActualizacion = SYSUTCDATETIME()
            WHERE IdUsuario = @IdUsuario;
            COMMIT TRANSACTION;
            SELECT CAST(1 AS BIT) AS Confirmado;
            """;
        var filas = await conexion.LeerAdministracionAsync(sql,
            [new("@Hash", seguridad.GenerarHashToken(solicitud.Token))], _baseDatos, 10);
        var resultado = filas.Rows.Count > 0 ? Convert.ToInt32(filas.Rows[0]["Confirmado"]) : 0;
        return resultado switch
        {
            1 => Results.Ok(new { codigo = "backupEmail.confirmed" }),
            2 => Results.BadRequest(new { codigo = "backupEmail.emailInUse" }),
            _ => Results.BadRequest(new { codigo = "backupEmail.invalidLink" })
        };
    }

    private async Task<IResult> SolicitarRecupero(SolicitudRecuperoRespaldo solicitud, HttpRequest request)
    {
        const string respuesta = "Si la cuenta tiene un correo de respaldo verificado, enviaremos allí un enlace de recuperación.";
        var emailCuenta = NormalizarEmail(solicitud.Email);
        var clave = $"{request.HttpContext.Connection.RemoteIpAddress}:{emailCuenta}";
        var ahora = DateTimeOffset.UtcNow;
        if (UltimaSolicitud.Count > 4000)
        {
            foreach (var entrada in UltimaSolicitud)
                if (ahora - entrada.Value > TimeSpan.FromMinutes(1)) UltimaSolicitud.TryRemove(entrada.Key, out _);
        }
        if (!UltimaSolicitud.TryGetValue(clave, out var ultima) || ahora - ultima >= TimeSpan.FromMinutes(1))
        {
            UltimaSolicitud[clave] = ahora;
            try
            {
                if (emailCuenta is not null)
                {
                    var usuario = usuariosMPP.ConsultarUsuarioPorEmail(new Usuario { Email = emailCuenta });
                    if (usuario is { Activo: true } && string.Equals(usuario.Estado, "Activo", StringComparison.OrdinalIgnoreCase))
                    {
                        await AsegurarTabla();
                        const string consulta = "SELECT CorreoVerificado FROM dbo.CorreoRespaldoUsuario WHERE IdUsuario = @IdUsuario;";
                        var filas = await conexion.LeerAdministracionAsync(consulta, [new("@IdUsuario", usuario.ID)], _baseDatos, 10);
                        if (filas.Rows.Count > 0 && !filas.Rows[0].IsNull("CorreoVerificado"))
                        {
                            var correoRespaldo = (string)filas.Rows[0]["CorreoVerificado"];
                            var token = NuevoToken();
                            var fecha = DateTime.Now;
                            var validacion = new ValidacionCuenta(0, usuario.ID, "RecuperacionPassword",
                                seguridad.GenerarHashToken(token), fecha, fecha.AddMinutes(30), false, null, true);
                            usuariosMPP.ReemplazarRecuperacionPassword(usuario, validacion);
                            usuario.Email = correoRespaldo;
                            await correo.EnviarCorreoRecuperoPassword(usuario, token);
                        }
                    }
                }
            }
            catch
            {
                // La respuesta pública no revela si la cuenta existe ni si el envío tuvo éxito.
            }
        }
        return Results.Ok(new { mensaje = respuesta });
    }

    private static string? NormalizarEmail(string? email)
    {
        if (string.IsNullOrWhiteSpace(email) || email.Length > 320) return null;
        try
        {
            var direccion = new MailAddress(email.Trim());
            return string.Equals(direccion.Address, email.Trim(), StringComparison.OrdinalIgnoreCase)
                ? direccion.Address.ToLowerInvariant()
                : null;
        }
        catch (FormatException) { return null; }
    }

    private static string NuevoToken() => Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))
        .Replace('+', '-').Replace('/', '_').TrimEnd('=');
}
