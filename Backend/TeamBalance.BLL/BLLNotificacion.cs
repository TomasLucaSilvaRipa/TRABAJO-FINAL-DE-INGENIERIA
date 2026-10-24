using TeamBalance.BE.Entidades;
using TeamBalance.MPP;

namespace TeamBalance.BLL;

public class BLLNotificacion
{
    private readonly MPPNotificacion _notificacionMPP;
    private readonly BLLRol _rolBLL;
    private readonly BLLBitacora _bitacoraBLL;

    public BLLNotificacion(MPPNotificacion notificacionMPP, BLLRol rolBLL, BLLBitacora bitacoraBLL) { _notificacionMPP = notificacionMPP; _rolBLL = rolBLL; _bitacoraBLL = bitacoraBLL; }

    public List<NotificacionUsuario> ConsultarPropias(Usuario solicitante)
    {
        ValidarAcceso(solicitante);
        return _notificacionMPP.ConsultarPropias(solicitante.ID);
    }

    public void MarcarLeida(Usuario solicitante, int idNotificacion)
    {
        ValidarAcceso(solicitante);
        _notificacionMPP.MarcarLeida(solicitante.ID, idNotificacion);
        _bitacoraBLL.Add(new Bitacora(solicitante.ID, solicitante.IdAgencia, "Notificacion", idNotificacion, "MarcarNotificacionLeida", "El usuario marcó una notificación como leída.", "Exitoso", "Informacion", "General"));
    }

    public void MarcarTodasLeidas(Usuario solicitante)
    {
        ValidarAcceso(solicitante);
        _notificacionMPP.MarcarTodasLeidas(solicitante.ID);
        _bitacoraBLL.Add(new Bitacora(solicitante.ID, solicitante.IdAgencia, "Notificacion", null, "MarcarNotificacionesLeidas", "El usuario marcó todas sus notificaciones como leídas.", "Exitoso", "Informacion", "General"));
    }

    private void ValidarAcceso(Usuario solicitante)
    {
        if (solicitante.ID <= 0 || !_rolBLL.TienePermiso(solicitante, "ConsultarNotificaciones"))
            throw new UnauthorizedAccessException("No tenés permiso para consultar notificaciones.");
    }
}
