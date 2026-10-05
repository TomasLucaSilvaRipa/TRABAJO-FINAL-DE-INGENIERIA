using TeamBalance.BE.Entidades;
using TeamBalance.MPP;

namespace TeamBalance.BLL;
public class BLLTarea
{
    private readonly MPPTarea _tareaMPP;
    private readonly MPPRecursos _recursosMPP;
    private readonly BLLRol _rolBLL;
    private readonly BLLBitacora _bitacoraBLL;
    public BLLTarea(MPPTarea tareaMPP, MPPRecursos recursosMPP, BLLRol rolBLL, BLLBitacora bitacoraBLL) { _tareaMPP = tareaMPP; _recursosMPP = recursosMPP; _rolBLL = rolBLL; _bitacoraBLL = bitacoraBLL; }
    public List<Tarea> Consultar(Usuario usuario)
    {
        try
        {
            ValidarGestionTareas(usuario);
            if (ObtenerAgencia(usuario))
            {
                return _tareaMPP.Consultar(usuario);
            }
            else{ throw new UnauthorizedAccessException("Tu usuario no pertenece a una agencia.");}
        }
        catch(Exception ex) { throw new Exception(ex.Message); }
    }
    public List<Tarea> ConsultarAsignadas(Usuario usuario) {
        try
        {
            if (!_rolBLL.TienePermiso(usuario, "VerDashboard")) { throw new UnauthorizedAccessException("No tenés permiso para consultar tus tareas."); }
            return _tareaMPP.ConsultarPorEmpleado(usuario);
        }
        catch (UnauthorizedAccessException) { throw; }
        catch (Exception ex) { throw new Exception(ex.Message); }
    }
    public List<Tarea> Consultar(Usuario usuario, FiltroTarea filtro)
    {
        try
        {
            ValidarGestionTareas(usuario);
            List<Tarea> tareas = _tareaMPP.Consultar(usuario, filtro);
            return tareas;
        }
        catch (Exception ex) { throw new Exception(ex.Message); }
    }
    public List<Tarea> ConsultarPorPM(Usuario usuario)
    {
        try
        {
            ValidarGestionTareas(usuario);
            List<Tarea> tareas = _tareaMPP.ConsultarPorPM(usuario);
            return tareas;
        }
        catch (Exception ex) { throw new Exception(ex.Message); }
    }

    public bool GuardarComentarios(Tarea tarea, Usuario usuario)
    {
        try
        {
            List<Tarea> tareas = ConsultarAsignadas(usuario);
            if (!tareas.Any(item => item.ID == tarea.ID))
            {
                throw new UnauthorizedAccessException("No tenés acceso a esta tarea.");
            }
            return _tareaMPP.GuardarComentarios(tarea,usuario);
        }
        catch (UnauthorizedAccessException) { throw; }
        catch (Exception ex) { throw new Exception(ex.Message); }
    }
    public GestionTareaOpciones ConsultarOpciones(Usuario usuario)
    {
        try
        {
            ValidarGestionTareas(usuario);
            if (ObtenerAgencia(usuario))
            {
                return _tareaMPP.ConsultarOpciones(usuario);
            }
            else { throw new UnauthorizedAccessException("Tu usuario no pertenece a una agencia."); }
        }
        catch (Exception ex) { throw new Exception(ex.Message); }
    }

    public Tarea Guardar(Tarea tarea, Usuario usuario)
    {
        try
        {
            ValidarGestionTareas(usuario);
            if (string.IsNullOrWhiteSpace(tarea.Titulo)) { throw new ArgumentException("Completá el título de la tarea."); }
            if (tarea.IdProyecto <= 0) { throw new ArgumentException("Seleccioná el proyecto de la tarea."); }
            if (tarea.IdEstadoTarea <= 0) { throw new ArgumentException("Seleccioná el estado inicial de la tarea."); }
            if (!tarea.IdSkillRequerido.HasValue || tarea.IdSkillRequerido.Value <= 0) { throw new ArgumentException("Seleccioná la skill requerida para la tarea."); }
            tarea.Titulo = tarea.Titulo.Trim();
            if (ObtenerAgencia(usuario))
            {
                return _tareaMPP.Guardar(tarea, usuario);
            }
            else { throw new UnauthorizedAccessException("Tu usuario no pertenece a una agencia."); }
        }
        catch (Exception ex) { throw new Exception(ex.Message); }
    }
    public bool CambiarEstado(Tarea tarea, Usuario usuario)
    {
        try
        {
            ValidarGestionTareas(usuario);
            if (ObtenerAgencia(usuario))
            {
                return _tareaMPP.CambiarEstado(tarea, usuario);
            }
            else { throw new UnauthorizedAccessException("Tu usuario no pertenece a una agencia."); }
        }catch (Exception ex) { throw new Exception(ex.Message); }
    }

    public bool CambiarEstadoPropio(Tarea tarea, Usuario usuario)
    {
        try
        {
            if (!_rolBLL.TienePermiso(usuario, "VerKanban")) { throw new UnauthorizedAccessException("No tenés permiso para actualizar tareas desde el tablero."); }
            if (tarea.ID <= 0 || string.IsNullOrWhiteSpace(tarea.Estado)) { throw new ArgumentException("Indicá la tarea y el estado a actualizar."); }
            List<Tarea> tareas = ConsultarAsignadas(usuario);
            if (!tareas.Any(item => item.ID == tarea.ID)) { throw new UnauthorizedAccessException("No tenés acceso a esta tarea."); }
            bool resultado = _tareaMPP.CambiarEstadoPropio(tarea, usuario);
            return resultado;
        }
        catch (Exception ex) { throw new Exception(ex.Message); }
    }

    public List<Tarea> ConsultarTableroProyectoPropio(Tarea tarea, Usuario usuario)
    {
        try
        {
            if (!_rolBLL.TienePermiso(usuario, "VerKanban")) { throw new UnauthorizedAccessException("No tenés permiso para consultar el tablero."); }
            if (tarea.IdProyecto <= 0) { throw new ArgumentException("Indicá el proyecto a consultar."); }
            return _tareaMPP.ConsultarTableroProyectoPropio(tarea, usuario);
        }
        catch (UnauthorizedAccessException) { throw; }
        catch (ArgumentException) { throw; }
        catch (Exception ex) { throw new Exception(ex.Message); }
    }

    public Tarea ActualizarAvancePropio(Tarea tarea, Usuario usuario)
    {
        try
        {
            if (!_rolBLL.TienePermiso(usuario, "VerKanban")) { throw new UnauthorizedAccessException("No tenés permiso para actualizar tareas desde el tablero."); }
            if (tarea.ID <= 0 || string.IsNullOrWhiteSpace(tarea.Estado)) { throw new ArgumentException("Indicá la tarea y el estado a actualizar."); }
            if (tarea.PorcentajeAvance < 0 || tarea.PorcentajeAvance > 100) { throw new ArgumentException("El avance debe estar entre 0 y 100."); }
            if (string.Equals(tarea.Estado, "Bloqueada", StringComparison.OrdinalIgnoreCase) && string.IsNullOrWhiteSpace(tarea.MotivoBloqueo)) { throw new ArgumentException("Indicá el motivo del bloqueo."); }
            List<Tarea> tareas = ConsultarAsignadas(usuario);
            if (!tareas.Any(item => item.ID == tarea.ID)) { throw new UnauthorizedAccessException("No tenés acceso a esta tarea."); }
            Tarea resultado = _tareaMPP.ActualizarAvancePropio(tarea, usuario);
            _bitacoraBLL.Add(new Bitacora(usuario.ID, usuario.IdAgencia, "Tarea", tarea.ID, "Actualizar avance de tarea", "Se actualizó el avance de la tarea " + resultado.Titulo + ".", "Exitoso", "Informacion", "Kanban"));
            return resultado;
        }
        catch (UnauthorizedAccessException) { throw; }
        catch (ArgumentException) { throw; }
        catch (Exception ex) { throw new Exception(ex.Message); }
    }

    public Skill RegistrarSkill(Skill skill, Usuario usuario) {
        try
        {
            ValidarGestionTareas(usuario);
            if (string.IsNullOrWhiteSpace(skill.Nombre)) { throw new ArgumentException("Ingresá el nombre de la skill."); }
            skill.Nombre = skill.Nombre.Trim();
            return _recursosMPP.RegistrarSkill(skill);
        }catch(Exception ex) { throw new Exception(ex.Message); }
        
    }
    public bool CambiarEstadoSkill(Skill skill, Usuario usuario) { 
        ValidarGestionTareas(usuario); 
        return _recursosMPP.CambiarEstadoSkill(skill); 
    }
    private void ValidarGestionTareas(Usuario usuario) { 
        if (!_rolBLL.TienePermiso(usuario, "GestionarTareas")) { throw new UnauthorizedAccessException("No tenés permiso para gestionar tareas."); } 
    }
    private bool ObtenerAgencia(Usuario usuario) { 
        if (!usuario.IdAgencia.HasValue) { throw new UnauthorizedAccessException("Tu usuario no pertenece a una agencia."); } 
        return true; 
    }
}
