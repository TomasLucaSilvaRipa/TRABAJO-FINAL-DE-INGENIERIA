using TeamBalance.BLL;

namespace Teambalance.API.Middleware;

public sealed class SubscriptionAccessMiddleware
{
    private readonly RequestDelegate _next;

    public SubscriptionAccessMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext context, BLLUsuario usuarioBLL, BLLSuscripcion suscripcionBLL)
    {
        PathString path = context.Request.Path;
        string authorization = context.Request.Headers.Authorization.ToString();
        if (!path.StartsWithSegments("/api") || Excluido(path) || !authorization.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            await _next(context);
            return;
        }

        string token = authorization[7..].Trim();
        TeamBalance.BE.Entidades.Usuario? usuario = usuarioBLL.ConsultarUsuarioSesion(token);
        if (usuario is not null && usuario.IdAgencia.HasValue && !suscripcionBLL.PuedeUsarAgencia(usuario))
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            await context.Response.WriteAsJsonAsync(new
            {
                code = "SUBSCRIPTION_EXPIRED",
                message = "La suscripción de esta agencia no está vigente. Contactá al Dueño para regularizar el servicio.",
            });
            return;
        }

        await _next(context);
    }

    private static bool Excluido(PathString path) =>
        path.StartsWithSegments("/api/auth") ||
        path.StartsWithSegments("/api/contratacion") ||
        path.StartsWithSegments("/api/suscripciones") ||
        path.StartsWithSegments("/api/agencias/validar-cuenta") ||
        path.StartsWithSegments("/api/agencias/reenvio-validacion");
}
