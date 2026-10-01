using System.Data;
using Microsoft.Data.SqlClient;
using TeamBalance.BE.Entidades;
using TeamBalance.DAL;

namespace TeamBalance.MPP;

public class MPPRecursos
{
    private readonly Conexion _conexion;
    public MPPRecursos(Conexion conexion) { _conexion = conexion; }
    public List<Skill> ConsultarSkills()
    {
        try
        {
            DataTable tabla = _conexion.Leer("dbo.usp_Skill_Consultar");
            List<Skill> skills = CrearSkills(tabla);
            return skills;
        }
        catch (Exception ex) { throw new Exception(ex.Message); }
    }

    public Skill RegistrarSkill(Skill skill)
    {
        try
        {
            List<SqlParameter> parametros = new List<SqlParameter>() { new SqlParameter("@Nombre", skill.Nombre), new SqlParameter("@Categoria", (object?)skill.Categoria ?? DBNull.Value) };
            DataTable tabla = _conexion.Leer("dbo.usp_Skill_Registrar", parametros);
            List<Skill> skills = CrearSkills(tabla);
            Skill resultado = skills.Single();
            return resultado;
        }
        catch (Exception ex) { throw new Exception(ex.Message); }
    }

    public bool CambiarEstadoSkill(Skill skill)
    {
        try
        {
            List<SqlParameter> parametros = new List<SqlParameter>() { new SqlParameter("@ID", skill.ID), new SqlParameter("@Activo", skill.Activo) };
            bool resultado = _conexion.Escribir("dbo.usp_Skill_CambiarEstado", parametros);
            return resultado;
        }
        catch (Exception ex) { throw new Exception(ex.Message); }
    }
    public Empleado ConsultarFichaEmpleado(Usuario usuario, Usuario solicitante)
    {
        try
        {
            int idAgencia = solicitante.IdAgencia ?? throw new UnauthorizedAccessException("Tu usuario no pertenece a una agencia.");
            int idEmpleado = ConsultarIdEmpleado(usuario.ID, idAgencia);
            Empleado empleado = new Empleado();
            empleado.ID = idEmpleado;
            List<SqlParameter> parametrosSkills = new List<SqlParameter>() { new SqlParameter("@IdEmpleado", idEmpleado) };
            DataTable tablaSkills = _conexion.Leer("dbo.usp_EmpleadoSkill_Consultar", parametrosSkills);
            empleado.EmpleadoSkills = CrearEmpleadoSkills(tablaSkills);
            List<SqlParameter> parametrosDisponibilidad = new List<SqlParameter>() { new SqlParameter("@IdEmpleado", idEmpleado) };
            DataTable tablaDisponibilidad = _conexion.Leer("dbo.usp_DisponibilidadBase_Consultar", parametrosDisponibilidad);
            if (tablaDisponibilidad.Rows.Count > 0)
            {
                DataRow fila = tablaDisponibilidad.Rows[0];
                TimeSpan horaInicio = (TimeSpan)fila["HoraInicio"];
                TimeSpan horaFin = (TimeSpan)fila["HoraFin"];
                DisponibilidadBase disponibilidad = new DisponibilidadBase(Convert.ToInt32(fila["ID"]), idEmpleado, TimeOnly.FromTimeSpan(horaInicio), TimeOnly.FromTimeSpan(horaFin), Convert.ToDecimal(fila["HorasSemanales"]), Convert.ToString(fila["Observacion"]), Convert.ToBoolean(fila["Activo"]));
                empleado.DisponibilidadBase = disponibilidad;
            }
            return empleado;
        }
        catch (Exception ex) { throw new Exception(ex.Message); }
    }
    public void GuardarFichaEmpleado(Empleado empleado, Usuario solicitante)
    {
        try
        {
            int idAgencia = solicitante.IdAgencia ?? throw new UnauthorizedAccessException("Tu usuario no pertenece a una agencia.");
            int idEmpleado = ConsultarIdEmpleado(empleado.ID, idAgencia);
            List<SqlParameter> parametrosLimpiar = new List<SqlParameter>() { new SqlParameter("@IdEmpleado", idEmpleado) };
            _conexion.Escribir("dbo.usp_EmpleadoSkill_Limpiar", parametrosLimpiar);
            foreach (EmpleadoSkill empleadoSkill in empleado.EmpleadoSkills)
            {
                List<SqlParameter> parametrosSkill = new List<SqlParameter>() { new SqlParameter("@IdEmpleado", idEmpleado), new SqlParameter("@IdSkill", empleadoSkill.IdSkill), new SqlParameter("@Nivel", (object?)empleadoSkill.Nivel ?? DBNull.Value) };
                _conexion.Escribir("dbo.usp_EmpleadoSkill_Registrar", parametrosSkill);
            }
            if (empleado.DisponibilidadBase is null) { throw new ArgumentException("Completá la disponibilidad base del empleado."); }
            DisponibilidadBase disponibilidad = empleado.DisponibilidadBase;
            List<SqlParameter> parametrosDisponibilidad = new List<SqlParameter>() { new SqlParameter("@IdEmpleado", idEmpleado), new SqlParameter("@HoraInicio", disponibilidad.HoraInicio.ToTimeSpan()), new SqlParameter("@HoraFin", disponibilidad.HoraFin.ToTimeSpan()), new SqlParameter("@HorasSemanales", disponibilidad.HorasSemanales), new SqlParameter("@Observacion", (object?)disponibilidad.Observacion ?? DBNull.Value) };
            _conexion.Escribir("dbo.usp_DisponibilidadBase_RegistrarActualizar", parametrosDisponibilidad);
        }
        catch (Exception ex) { throw new Exception(ex.Message); }
    }

    public Empleado ConsultarMiDisponibilidad(Usuario usuario)
    {
        try
        {
            Empleado empleado = ConsultarFichaEmpleado(usuario, usuario);
            return empleado;
        }
        catch (Exception ex) { throw new Exception(ex.Message); }
    }

    public void GuardarMiDisponibilidad(DisponibilidadBase disponibilidad, Usuario usuario)
    {
        try
        {
            int idAgencia = usuario.IdAgencia ?? throw new UnauthorizedAccessException("Tu usuario no pertenece a una agencia.");
            int idEmpleado = ConsultarIdEmpleado(usuario.ID, idAgencia);
            List<SqlParameter> parametros = new List<SqlParameter>() { new SqlParameter("@IdEmpleado", idEmpleado), new SqlParameter("@HoraInicio", disponibilidad.HoraInicio.ToTimeSpan()), new SqlParameter("@HoraFin", disponibilidad.HoraFin.ToTimeSpan()), new SqlParameter("@HorasSemanales", disponibilidad.HorasSemanales), new SqlParameter("@Observacion", (object?)disponibilidad.Observacion ?? DBNull.Value) };
            _conexion.Escribir("dbo.usp_DisponibilidadBase_RegistrarActualizar", parametros);
        }
        catch (Exception ex) { throw new Exception(ex.Message); }
    }

    public List<AusenciaEmpleado> ConsultarMisAusencias(Usuario usuario)
    {
        try
        {
            List<SqlParameter> parametros = new List<SqlParameter>() { new SqlParameter("@IdUsuario", usuario.ID) };
            DataTable tabla = _conexion.Leer("dbo.usp_AusenciaEmpleado_ConsultarPropias", parametros);
            List<AusenciaEmpleado> ausencias = CrearAusencias(tabla);
            return ausencias;
        }
        catch (Exception ex) { throw new Exception(ex.Message); }
    }

    public AusenciaEmpleado RegistrarAusencia(AusenciaEmpleado ausencia, Usuario usuario)
    {
        try
        {
            List<SqlParameter> parametros = new List<SqlParameter>() { new SqlParameter("@IdUsuario", usuario.ID), new SqlParameter("@TipoPeriodo", ausencia.TipoPeriodo), new SqlParameter("@FechaInicio", ausencia.FechaInicioSolicitada), new SqlParameter("@FechaFin", ausencia.FechaFinSolicitada), new SqlParameter("@Motivo", (object?)ausencia.Motivo ?? DBNull.Value) };
            DataTable tabla = _conexion.Leer("dbo.usp_AusenciaEmpleado_Registrar", parametros);
            AusenciaEmpleado resultado = CrearAusencias(tabla).Single();
            return resultado;
        }
        catch (Exception ex) { throw new Exception(ex.Message); }
    }
    private int ConsultarIdEmpleado(int idUsuario, int idAgencia)
    {
        try
        {
            List<SqlParameter> parametros = new List<SqlParameter>() { new SqlParameter("@IdUsuario", idUsuario), new SqlParameter("@IdAgencia", idAgencia) };
            DataTable tabla = _conexion.Leer("dbo.usp_Empleado_ConsultarIdPorUsuarioAgencia", parametros);
            if (tabla.Rows.Count == 0) { throw new ArgumentException("El usuario seleccionado no es un empleado de tu agencia."); }
            int idEmpleado = Convert.ToInt32(tabla.Rows[0]["ID"]);
            return idEmpleado;
        }
        catch (Exception ex) { throw new Exception(ex.Message); }
    }

    private static List<Skill> CrearSkills(DataTable tabla)
    {
        try
        {
            List<Skill> skills = new List<Skill>();
            foreach (DataRow fila in tabla.Rows)
            {
                Skill skill = new Skill(Convert.ToInt32(fila["ID"]), Convert.ToString(fila["Nombre"]) ?? string.Empty, Convert.ToString(fila["Categoria"]), Convert.ToBoolean(fila["Activo"]));
                skills.Add(skill);
            }
            return skills;
        }
        catch (Exception ex) { throw new Exception(ex.Message); }
    }

    private static List<EmpleadoSkill> CrearEmpleadoSkills(DataTable tabla)
    {
        try
        {
            List<EmpleadoSkill> habilidades = new List<EmpleadoSkill>();
            foreach (DataRow fila in tabla.Rows)
            {
                EmpleadoSkill habilidad = new EmpleadoSkill(Convert.ToInt32(fila["ID"]), Convert.ToInt32(fila["IdEmpleado"]), Convert.ToInt32(fila["IdSkill"]), Convert.ToString(fila["Nivel"]), Convert.ToBoolean(fila["Activo"]));
                habilidades.Add(habilidad);
            }
            return habilidades;
        }
        catch (Exception ex) { throw new Exception(ex.Message); }
    }

    private static List<AusenciaEmpleado> CrearAusencias(DataTable tabla)
    {
        try
        {
            List<AusenciaEmpleado> ausencias = new List<AusenciaEmpleado>();
            foreach (DataRow fila in tabla.Rows)
            {
                AusenciaEmpleado ausencia = new AusenciaEmpleado(Convert.ToInt32(fila["ID"]), Convert.ToInt32(fila["IdEmpleado"]), Convert.ToString(fila["TipoPeriodo"]) ?? string.Empty, Convert.ToDateTime(fila["FechaInicioSolicitada"]), Convert.ToDateTime(fila["FechaFinSolicitada"]), fila["FechaInicioAprobada"] == DBNull.Value ? null : Convert.ToDateTime(fila["FechaInicioAprobada"]), fila["FechaFinAprobada"] == DBNull.Value ? null : Convert.ToDateTime(fila["FechaFinAprobada"]), fila["HorasNoDisponiblesSolicitadas"] == DBNull.Value ? null : Convert.ToDecimal(fila["HorasNoDisponiblesSolicitadas"]), fila["HorasNoDisponiblesAprobadas"] == DBNull.Value ? null : Convert.ToDecimal(fila["HorasNoDisponiblesAprobadas"]), Convert.ToString(fila["Motivo"]), fila["IdUsuarioResolucion"] == DBNull.Value ? null : Convert.ToInt32(fila["IdUsuarioResolucion"]), Convert.ToString(fila["Estado"]) ?? string.Empty, Convert.ToDateTime(fila["FechaSolicitud"]), fila["FechaResolucion"] == DBNull.Value ? null : Convert.ToDateTime(fila["FechaResolucion"]), Convert.ToString(fila["MotivoResolucion"]), Convert.ToBoolean(fila["Activo"]));
                ausencias.Add(ausencia);
            }
            return ausencias;
        }
        catch (Exception ex) { throw new Exception(ex.Message); }
    }
}
