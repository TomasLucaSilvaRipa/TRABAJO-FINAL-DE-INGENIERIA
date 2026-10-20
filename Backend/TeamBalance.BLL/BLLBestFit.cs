using TeamBalance.BE.Entidades;
using TeamBalance.MPP;

namespace TeamBalance.BLL;

public class BLLBestFit
{
    private readonly MPPBestFit _bestFitMPP;
    private readonly MPPTarea _tareaMPP;
    private readonly MotorBestFit _motorBestFit;
    private readonly BLLRol _rolBLL;
    private readonly BLLBitacora _bitacoraBLL;

    public BLLBestFit(MPPBestFit bestFitMPP, MPPTarea tareaMPP, MotorBestFit motorBestFit, BLLRol rolBLL, BLLBitacora bitacoraBLL)
    {
        _bestFitMPP = bestFitMPP;
        _tareaMPP = tareaMPP;
        _motorBestFit = motorBestFit;
        _rolBLL = rolBLL;
        _bitacoraBLL = bitacoraBLL;
    }

    public List<RecomendacionBestFit> Sugerir(RecomendacionBestFit recomendacion, Usuario usuario)
    {
        try
        {
            if (!_rolBLL.TienePermiso(usuario, "UsarBestFit")) { throw new UnauthorizedAccessException("No tenés permiso para usar Best Fit."); }
            if (recomendacion.IdTarea <= 0) { throw new ArgumentException("Seleccioná una tarea para generar recomendaciones."); }
            Tarea tarea = _tareaMPP.ConsultarPorPM(usuario).SingleOrDefault(item => item.ID == recomendacion.IdTarea && item.Activo)
                ?? throw new UnauthorizedAccessException("La tarea no existe o no pertenece a uno de tus proyectos.");
            ValidarTareaParaBestFit(tarea);
            List<Empleado> empleados = _bestFitMPP.ConsultarCandidatosParaBestFit(tarea, usuario);
            List<RecomendacionBestFit> resultados = _motorBestFit.GenerarRanking(tarea, empleados);
            foreach (RecomendacionBestFit resultado in resultados) { resultado.IdUsuarioSolicitante = usuario.ID; }
            _bitacoraBLL.Add(new Bitacora(usuario.ID, usuario.IdAgencia, "Tarea", tarea.ID, "Generar recomendación Best Fit", "Se generó el ranking de recursos para la tarea " + tarea.Titulo + ".", "Exitoso", "Informacion", "Planificación"));
            return resultados;
        }
        catch (UnauthorizedAccessException) { throw; }
        catch (ArgumentException) { throw; }
        catch (Exception ex) { throw new Exception(ex.Message); }
    }
    public bool GuardarRecomendacion(RecomendacionBestFit recomendacion, Usuario usuario)
    {
        try
        {
            if (!_rolBLL.TienePermiso(usuario, "UsarBestFit")) { throw new UnauthorizedAccessException("No tenés permiso para conservar una recomendación."); }
            if (recomendacion.IdTarea <= 0 || recomendacion.IdEmpleadoSugerido <= 0) { throw new ArgumentException("Seleccioná una recomendación válida."); }
            Tarea tarea = _tareaMPP.ConsultarPorPM(usuario).SingleOrDefault(item => item.ID == recomendacion.IdTarea && item.Activo)
                ?? throw new UnauthorizedAccessException("La tarea no existe o no pertenece a uno de tus proyectos.");
            ValidarTareaParaBestFit(tarea);
            List<Empleado> empleados = _bestFitMPP.ConsultarCandidatosParaBestFit(tarea, usuario);
            RecomendacionBestFit recomendacionValidada = _motorBestFit.GenerarRanking(tarea, empleados).SingleOrDefault(item => item.IdEmpleadoSugerido == recomendacion.IdEmpleadoSugerido)
                ?? throw new ArgumentException("El empleado seleccionado ya no cumple las condiciones operativas para esta recomendación.");
            recomendacionValidada.IdUsuarioSolicitante = usuario.ID;
            bool resultado = _bestFitMPP.GuardarRecomendacion(recomendacionValidada, usuario);
            if (resultado) { _bitacoraBLL.Add(new Bitacora(usuario.ID, usuario.IdAgencia, "RecomendacionBestFit", null, "Conservar recomendación Best Fit", "Se conservó una recomendación de recurso para la tarea " + tarea.Titulo + ".", "Exitoso", "Informacion", "Planificación")); }
            return resultado;
        }
        catch (UnauthorizedAccessException) { throw; }
        catch (ArgumentException) { throw; }
        catch (Exception ex) { throw new Exception(ex.Message); }
    }

    public List<RecomendacionBestFit> ConsultarRecomendaciones(int idTarea, Usuario usuario)
    {
        try
        {
            if (!_rolBLL.TienePermiso(usuario, "UsarBestFit")) { throw new UnauthorizedAccessException("No tenés permiso para consultar recomendaciones de Best Fit."); }
            if (idTarea <= 0) { throw new ArgumentException("Seleccioná una tarea válida."); }
            bool perteneceAlPM = _tareaMPP.ConsultarPorPM(usuario).Any(item => item.ID == idTarea && item.Activo);
            if (!perteneceAlPM) { throw new UnauthorizedAccessException("La tarea no existe o no pertenece a uno de tus proyectos."); }
            return _bestFitMPP.ConsultarRecomendaciones(idTarea, usuario);
        }
        catch (UnauthorizedAccessException) { throw; }
        catch (ArgumentException) { throw; }
        catch (Exception ex) { throw new Exception(ex.Message); }
    }

    private static void ValidarTareaParaBestFit(Tarea tarea)
    {
        if (tarea.IdEmpleadoAsignado.HasValue) { throw new ArgumentException("La tarea ya tiene un responsable asignado. Gestioná la asignación desde la planificación del proyecto."); }
        if (!tarea.IdSkillRequerido.HasValue || tarea.IdSkillRequerido.Value <= 0) { throw new ArgumentException("La tarea debe tener una skill requerida para usar Best Fit."); }
        if (string.IsNullOrWhiteSpace(tarea.Complejidad)) { throw new ArgumentException("La tarea debe tener una complejidad definida para usar Best Fit."); }
        if (!tarea.FechaInicio.HasValue) { throw new ArgumentException("La tarea debe tener una fecha de inicio para usar Best Fit."); }
        if (tarea.HorasEstimadas <= 0) { throw new ArgumentException("La tarea debe tener horas estimadas para usar Best Fit."); }
        if (!tarea.Deadline.HasValue) { throw new ArgumentException("La tarea debe tener un deadline para usar Best Fit."); }
        if (tarea.Deadline.Value.Date < DateTime.Today) { throw new ArgumentException("El deadline de la tarea ya venció."); }
    }
}
