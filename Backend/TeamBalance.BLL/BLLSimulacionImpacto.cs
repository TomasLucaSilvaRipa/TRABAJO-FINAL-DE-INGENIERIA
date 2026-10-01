using TeamBalance.BE.Entidades;
using TeamBalance.MPP;

namespace TeamBalance.BLL;

public class BLLSimulacionImpacto
{
    private readonly MPPSimulacionImpacto _simulacionMPP; private readonly BLLRol _rolBLL;
    public BLLSimulacionImpacto(MPPSimulacionImpacto simulacionMPP, BLLRol rolBLL) { _simulacionMPP = simulacionMPP; _rolBLL = rolBLL; }
    public SimulacionImpacto Simular(SimulacionImpacto simulacion, Usuario usuario)
    {
        try
        {
            if (!_rolBLL.TienePermiso(usuario, "SimularImpacto")) { throw new UnauthorizedAccessException("No tenés permiso para simular impacto."); }
            if (simulacion.IdTarea <= 0 || simulacion.IdEmpleadoCandidato <= 0) { throw new ArgumentException("Seleccioná una tarea y un recurso candidato."); }
            SimulacionImpacto resultado = _simulacionMPP.Simular(simulacion, usuario);
            return resultado;
        }
        catch (Exception ex) { throw new Exception(ex.Message); }
    }
}
