using TeamBalance.BE.Entidades;
using TeamBalance.MPP;

namespace TeamBalance.BLL;

public class BLLBestFit
{
    private readonly MPPBestFit _bestFitMPP; private readonly BLLRol _rolBLL;
    public BLLBestFit(MPPBestFit bestFitMPP, BLLRol rolBLL) { _bestFitMPP = bestFitMPP; _rolBLL = rolBLL; }

    public List<RecomendacionBestFit> Sugerir(RecomendacionBestFit recomendacion, Usuario usuario)
    {
        try
        {
            if (!_rolBLL.TienePermiso(usuario, "UsarBestFit")) { throw new UnauthorizedAccessException("No tenés permiso para usar Best Fit."); }
            if (recomendacion.IdTarea <= 0) { throw new ArgumentException("Seleccioná una tarea para generar recomendaciones."); }
            List<RecomendacionBestFit> resultados = _bestFitMPP.Sugerir(recomendacion, usuario);
            return resultados;
        }
        catch (Exception ex) { throw new Exception(ex.Message); }
    }
    public bool ConfirmarAsignacion(RecomendacionBestFit recomendacion, Usuario usuario)
    {
        try
        {
            if (!_rolBLL.TienePermiso(usuario, "UsarBestFit")) { throw new UnauthorizedAccessException("No tenés permiso para confirmar una recomendación."); }
            if (recomendacion.ID <= 0) { throw new ArgumentException("Seleccioná una recomendación válida."); }
            bool resultado = _bestFitMPP.ConfirmarAsignacion(recomendacion, usuario);
            return resultado;
        }
        catch (Exception ex) { throw new Exception(ex.Message); }
    }
}
