using TeamBalance.BE.Entidades;
using TeamBalance.MPP;

namespace TeamBalance.BLL;
public class BLLTarea
{
    private readonly MPPTarea _tareaMPP;
    private readonly MPPRecursos _recursosMPP;
    private readonly BLLRol _rolBLL;
    public BLLTarea(MPPTarea tareaMPP, MPPRecursos recursosMPP, BLLRol rolBLL) { _tareaMPP = tareaMPP; _recursosMPP = recursosMPP; _rolBLL = rolBLL; }
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
        if (!_rolBLL.TienePermiso(usuario, "VerDashboard")) { throw new UnauthorizedAccessException("No tenés permiso para consultar tus tareas."); } 
        return _tareaMPP.ConsultarPorEmpleado(usuario); 
    }

    public bool GuardarComentarios(Tarea tarea, Usuario usuario)
    {
        try
        {
            List<Tarea> tareas = ConsultarAsignadas(usuario);
            if (!tareas.Any(tarea => tarea.ID == tarea.ID))
            {
                throw new UnauthorizedAccessException("No tenés acceso a esta tarea.");
            }
            return _tareaMPP.GuardarComentarios(tarea,usuario);
        }catch(Exception ex) { throw new Exception(ex.Message); }
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
            if (string.IsNullOrWhiteSpace(tarea.Titulo) || tarea.IdProyecto <= 0 || tarea.IdEstadoTarea <= 0 || !tarea.IdSkillRequerido.HasValue || tarea.IdSkillRequerido.Value <= 0) { throw new ArgumentException("Completá título, proyecto, skill requerida y estado de la tarea."); }
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
