using TeamBalance.BE.Entidades;
using TeamBalance.MPP;

namespace TeamBalance.BLL;

public class BLLRecursos
{
    private readonly MPPRecursos _recursosMPP;
    private readonly BLLRol _rolBLL;

    public BLLRecursos(MPPRecursos recursosMPP, BLLRol rolBLL)
    {
        _recursosMPP = recursosMPP;
        _rolBLL = rolBLL;
    }

    public List<Skill> ConsultarSkills(Usuario solicitante)
    {
        try
        {
            ValidarGestionUsuarios(solicitante);
            List<Skill> skills = _recursosMPP.ConsultarSkills();
            return skills;
        }
        catch (Exception ex) { throw new Exception(ex.Message); }
    }

    public Skill RegistrarSkill(Skill skill, Usuario solicitante)
    {
        try
        {
            ValidarGestionUsuarios(solicitante);
            if (string.IsNullOrWhiteSpace(skill.Nombre)) { throw new ArgumentException("Ingresá el nombre de la skill."); }
            skill.Nombre = skill.Nombre.Trim();
            Skill resultado = _recursosMPP.RegistrarSkill(skill);
            return resultado;
        }
        catch (Exception ex) { throw new Exception(ex.Message); }
    }

    public bool CambiarEstadoSkill(Skill skill, Usuario solicitante)
    {
        try
        {
            ValidarGestionUsuarios(solicitante);
            bool resultado = _recursosMPP.CambiarEstadoSkill(skill);
            return resultado;
        }
        catch (Exception ex) { throw new Exception(ex.Message); }
    }

    public Empleado ConsultarFichaEmpleado(Usuario usuario, Usuario solicitante)
    {
        try
        {
            ValidarGestionUsuarios(solicitante);
            ObtenerAgencia(solicitante);
            Empleado empleado = _recursosMPP.ConsultarFichaEmpleado(usuario, solicitante);
            return empleado;
        }
        catch (Exception ex) { throw new Exception(ex.Message); }
    }

    public void GuardarFichaEmpleado(Empleado empleado, Usuario solicitante)
    {
        try
        {
            ValidarGestionUsuarios(solicitante);
            ObtenerAgencia(solicitante);
            if (empleado.EmpleadoSkills.Count == 0) { throw new ArgumentException("Seleccioná al menos una skill para el empleado."); }
            _recursosMPP.GuardarFichaEmpleado(empleado, solicitante);
        }
        catch (Exception ex) { throw new Exception(ex.Message); }
    }

    public Empleado ConsultarMiDisponibilidad(Usuario usuario)
    {
        try
        {
            ValidarDisponibilidad(usuario);
            Empleado empleado = _recursosMPP.ConsultarMiDisponibilidad(usuario);
            return empleado;
        }
        catch (Exception ex) { throw new Exception(ex.Message); }
    }

    public void GuardarMiDisponibilidad(DisponibilidadBase disponibilidad, Usuario usuario)
    {
        try
        {
            ValidarDisponibilidad(usuario);
            if (disponibilidad.HorasSemanales <= 0) { throw new ArgumentException("Ingresá una disponibilidad semanal válida."); }
            if (disponibilidad.HoraFin <= disponibilidad.HoraInicio) { throw new ArgumentException("La hora de finalización debe ser posterior a la de inicio."); }
            _recursosMPP.GuardarMiDisponibilidad(disponibilidad, usuario);
        }
        catch (Exception ex) { throw new Exception(ex.Message); }
    }

    public List<AusenciaEmpleado> ConsultarMisAusencias(Usuario usuario)
    {
        try
        {
            ValidarDisponibilidad(usuario);
            List<AusenciaEmpleado> ausencias = _recursosMPP.ConsultarMisAusencias(usuario);
            return ausencias;
        }
        catch (Exception ex) { throw new Exception(ex.Message); }
    }

    public AusenciaEmpleado RegistrarAusencia(AusenciaEmpleado ausencia, Usuario usuario)
    {
        try
        {
            ValidarDisponibilidad(usuario);
            if (string.IsNullOrWhiteSpace(ausencia.TipoPeriodo) || ausencia.FechaInicioSolicitada == DateTime.MinValue || ausencia.FechaFinSolicitada == DateTime.MinValue) { throw new ArgumentException("Completá tipo y fechas de la ausencia."); }
            if (ausencia.FechaFinSolicitada.Date < ausencia.FechaInicioSolicitada.Date) { throw new ArgumentException("La fecha final no puede ser anterior a la fecha inicial."); }
            ausencia.TipoPeriodo = ausencia.TipoPeriodo.Trim();
            AusenciaEmpleado resultado = _recursosMPP.RegistrarAusencia(ausencia, usuario);
            return resultado;
        }
        catch (Exception ex) { throw new Exception(ex.Message); }
    }

    private void ValidarGestionUsuarios(Usuario solicitante)
    {
        try
        {
            if (!_rolBLL.TienePermiso(solicitante, "GestionarUsuarios")) { throw new UnauthorizedAccessException("No tenés permiso para gestionar empleados."); }
        }
        catch (Exception ex) { throw new Exception(ex.Message); }
    }

    private void ValidarDisponibilidad(Usuario usuario)
    {
        try
        {
            if (!usuario.IdAgencia.HasValue) { throw new UnauthorizedAccessException("Tu usuario no pertenece a una agencia."); }
            if (!_rolBLL.TienePermiso(usuario, "GestionarDisponibilidad")) { throw new UnauthorizedAccessException("No tenés permiso para gestionar disponibilidad."); }
        }
        catch (Exception ex) { throw new Exception(ex.Message); }
    }

    private static int ObtenerAgencia(Usuario usuario)
    {
        try
        {
            if (!usuario.IdAgencia.HasValue) { throw new UnauthorizedAccessException("Tu usuario no pertenece a una agencia."); }
            int idAgencia = usuario.IdAgencia.Value;
            return idAgencia;
        }
        catch (Exception ex) { throw new Exception(ex.Message); }
    }
}
