using TeamBalance.BE.Entidades;
using TeamBalance.MPP;

namespace TeamBalance.BLL;

public class BLLRecursos
{
    private readonly MPPRecursos _recursosMPP;
    private readonly BLLRol _rolBLL;
    public BLLRecursos(MPPRecursos recursosMPP, BLLRol rolBLL) { _recursosMPP = recursosMPP; _rolBLL = rolBLL; }
    public List<Skill> ConsultarSkills(Usuario solicitante) { ValidarGestionUsuarios(solicitante); return _recursosMPP.ConsultarSkills(); }
    public Skill RegistrarSkill(Skill skill, Usuario solicitante) { ValidarGestionUsuarios(solicitante); if (string.IsNullOrWhiteSpace(skill.Nombre)) { throw new ArgumentException("Ingresá el nombre de la skill."); } skill.Nombre = skill.Nombre.Trim(); return _recursosMPP.RegistrarSkill(skill); }
    public bool CambiarEstadoSkill(int idSkill, bool activo, Usuario solicitante) { ValidarGestionUsuarios(solicitante); return _recursosMPP.CambiarEstadoSkill(idSkill, activo); }
    public Empleado ConsultarFichaEmpleado(int idUsuario, Usuario solicitante) { ValidarGestionUsuarios(solicitante); return _recursosMPP.ConsultarFichaEmpleado(idUsuario, ObtenerAgencia(solicitante)); }
    public void GuardarFichaEmpleado(int idUsuario, Empleado empleado, Usuario solicitante) { ValidarGestionUsuarios(solicitante); if (empleado.EmpleadoSkills.Count == 0) { throw new ArgumentException("Seleccioná al menos una skill para el empleado."); } _recursosMPP.GuardarFichaEmpleado(idUsuario, ObtenerAgencia(solicitante), empleado); }
    private void ValidarGestionUsuarios(Usuario solicitante) { if (!_rolBLL.TienePermiso(solicitante, "GestionarUsuarios")) { throw new UnauthorizedAccessException("No tenés permiso para gestionar empleados."); } }
    private static int ObtenerAgencia(Usuario usuario) { if (!usuario.IdAgencia.HasValue) { throw new UnauthorizedAccessException("Tu usuario no pertenece a una agencia."); } return usuario.IdAgencia.Value; }
}
