using TeamBalance.BE.Entidades;
using TeamBalance.MPP;

namespace TeamBalance.BLL;
public class BLLTarea
{
    private readonly MPPTarea _tareaMPP;
    private readonly MPPRecursos _recursosMPP;
    private readonly BLLRol _rolBLL;
    public BLLTarea(MPPTarea tareaMPP, MPPRecursos recursosMPP, BLLRol rolBLL) { _tareaMPP = tareaMPP; _recursosMPP = recursosMPP; _rolBLL = rolBLL; }
    public List<Tarea> Consultar(Usuario usuario) { ValidarGestionTareas(usuario); return _tareaMPP.Consultar(ObtenerAgencia(usuario)); }
    public GestionTareaOpciones ConsultarOpciones(Usuario usuario) { ValidarGestionTareas(usuario); return _tareaMPP.ConsultarOpciones(ObtenerAgencia(usuario)); }
    public Tarea Guardar(Tarea tarea, Usuario usuario) { ValidarGestionTareas(usuario); if (string.IsNullOrWhiteSpace(tarea.Titulo) || tarea.IdProyecto <= 0 || tarea.IdEstadoTarea <= 0 || !tarea.IdSkillRequerido.HasValue || tarea.IdSkillRequerido.Value <= 0) { throw new ArgumentException("Completá título, proyecto, skill requerida y estado de la tarea."); } tarea.Titulo = tarea.Titulo.Trim(); return _tareaMPP.Guardar(tarea, ObtenerAgencia(usuario)); }
    public bool CambiarEstado(int id, bool activo, Usuario usuario) { ValidarGestionTareas(usuario); return _tareaMPP.CambiarEstado(id, ObtenerAgencia(usuario), activo); }
    public Skill RegistrarSkill(Skill skill, Usuario usuario) { ValidarGestionTareas(usuario); if (string.IsNullOrWhiteSpace(skill.Nombre)) { throw new ArgumentException("Ingresá el nombre de la skill."); } skill.Nombre = skill.Nombre.Trim(); return _recursosMPP.RegistrarSkill(skill); }
    public bool CambiarEstadoSkill(int idSkill, bool activo, Usuario usuario) { ValidarGestionTareas(usuario); return _recursosMPP.CambiarEstadoSkill(idSkill, activo); }
    private void ValidarGestionTareas(Usuario usuario) { if (!_rolBLL.TienePermiso(usuario, "GestionarTareas")) { throw new UnauthorizedAccessException("No tenés permiso para gestionar tareas."); } }
    private static int ObtenerAgencia(Usuario usuario) { if (!usuario.IdAgencia.HasValue) { throw new UnauthorizedAccessException("Tu usuario no pertenece a una agencia."); } return usuario.IdAgencia.Value; }
}
