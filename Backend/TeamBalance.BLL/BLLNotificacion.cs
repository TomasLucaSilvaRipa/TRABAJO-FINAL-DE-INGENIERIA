using TeamBalance.BE.Entidades;
using TeamBalance.MPP;

namespace TeamBalance.BLL;

public class BLLNotificacion
{
    private readonly MPPNotificacion _notificacionMPP;
    private readonly BLLRol _rolBLL;

    public BLLNotificacion(MPPNotificacion notificacionMPP, BLLRol rolBLL) { _notificacionMPP = notificacionMPP; _rolBLL = rolBLL; }

    public List<NotificacionUsuario> ConsultarPropias(Usuario solicitante)
    {
        ValidarAcceso(solicitante);
        return _notificacionMPP.ConsultarPropias(solicitante.ID);
    }

    public void MarcarLeida(Usuario solicitante, int idNotificacion)
    {
        ValidarAcceso(solicitante);
        _notificacionMPP.MarcarLeida(solicitante.ID, idNotificacion);
    }

    public void MarcarTodasLeidas(Usuario solicitante)
    {
        ValidarAcceso(solicitante);
        _notificacionMPP.MarcarTodasLeidas(solicitante.ID);
    }

    private void ValidarAcceso(Usuario solicitante)
    {
        if (solicitante.ID <= 0 || !_rolBLL.TienePermiso(solicitante, "ConsultarNotificaciones"))
            throw new UnauthorizedAccessException("No tenés permiso para consultar notificaciones.");
    }
}
