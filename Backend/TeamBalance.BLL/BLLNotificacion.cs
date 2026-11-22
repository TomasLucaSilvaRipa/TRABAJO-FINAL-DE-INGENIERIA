using TeamBalance.BE.Entidades;
using TeamBalance.MPP;

namespace TeamBalance.BLL;

public class BLLNotificacion
{
    private readonly MPPNotificacion _notificacionMPP;
    private readonly BLLBitacora _bitacoraBLL;

    public BLLNotificacion(MPPNotificacion notificacionMPP, BLLBitacora bitacoraBLL) { _notificacionMPP = notificacionMPP; _bitacoraBLL = bitacoraBLL; }

    public List<NotificacionUsuario> ConsultarPropias(Usuario solicitante)
    {
        ValidarAcceso(solicitante);
        return _notificacionMPP.ConsultarPropias(solicitante);
    }

    public void MarcarLeida(Usuario solicitante, NotificacionUsuario notificacion)
    {
        ValidarAcceso(solicitante);
        _notificacionMPP.MarcarLeida(solicitante, notificacion);
        _bitacoraBLL.Add(new Bitacora(solicitante.ID, solicitante.IdAgencia, "Notificacion", notificacion.ID, "MarcarNotificacionLeida", "El usuario marcó una notificación como leída.", "Exitoso", "Informacion", "General"));
    }

    public void MarcarTodasLeidas(Usuario solicitante)
    {
        ValidarAcceso(solicitante);
        _notificacionMPP.MarcarTodasLeidas(solicitante);
        _bitacoraBLL.Add(new Bitacora(solicitante.ID, solicitante.IdAgencia, "Notificacion", null, "MarcarNotificacionesLeidas", "El usuario marcó todas sus notificaciones como leídas.", "Exitoso", "Informacion", "General"));
    }

    private void ValidarAcceso(Usuario solicitante)
    {
        // Cada usuario autenticado accede únicamente a sus propias notificaciones.
        // No depende de un permiso configurable porque también se usa para validar
        // el acceso de la agencia inmediatamente después de iniciar sesión.
        if (solicitante.ID <= 0)
            throw new UnauthorizedAccessException("Tu sesión ya no es válida.");
    }
}
