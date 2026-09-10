using System.Data;
using Microsoft.Data.SqlClient;
using TeamBalance.BE.Entidades;
using TeamBalance.DAL;

namespace TeamBalance.MPP;

public class MPPRecursos
{
    private readonly Conexion _conexion;
    public MPPRecursos(Conexion conexion) { _conexion = conexion; }
    public List<Skill> ConsultarSkills() { DataTable tabla = _conexion.Leer("dbo.usp_Skill_Consultar"); return CrearSkills(tabla); }
    public Skill RegistrarSkill(Skill skill) { List<SqlParameter> parametros = new List<SqlParameter>() { new SqlParameter("@Nombre", skill.Nombre), new SqlParameter("@Categoria", (object?)skill.Categoria ?? DBNull.Value) }; DataTable tabla = _conexion.Leer("dbo.usp_Skill_Registrar", parametros); List<Skill> skills = CrearSkills(tabla); return skills.Single(); }
    public bool CambiarEstadoSkill(int idSkill, bool activo) { List<SqlParameter> parametros = new List<SqlParameter>() { new SqlParameter("@ID", idSkill), new SqlParameter("@Activo", activo) }; return _conexion.Escribir("dbo.usp_Skill_CambiarEstado", parametros); }
    public Empleado ConsultarFichaEmpleado(int idUsuario, int idAgencia)
    {
        int idEmpleado = ConsultarIdEmpleado(idUsuario, idAgencia);
        Empleado empleado = new Empleado(); empleado.ID = idEmpleado;
        List<SqlParameter> parametrosSkills = new List<SqlParameter>() { new SqlParameter("@IdEmpleado", idEmpleado) }; DataTable tablaSkills = _conexion.Leer("dbo.usp_EmpleadoSkill_Consultar", parametrosSkills); empleado.EmpleadoSkills = CrearEmpleadoSkills(tablaSkills);
        List<SqlParameter> parametrosDisponibilidad = new List<SqlParameter>() { new SqlParameter("@IdEmpleado", idEmpleado) }; DataTable tablaDisponibilidad = _conexion.Leer("dbo.usp_DisponibilidadBase_Consultar", parametrosDisponibilidad);
        if (tablaDisponibilidad.Rows.Count > 0) { DataRow fila = tablaDisponibilidad.Rows[0]; TimeSpan horaInicio = (TimeSpan)fila["HoraInicio"]; TimeSpan horaFin = (TimeSpan)fila["HoraFin"]; DisponibilidadBase disponibilidad = new DisponibilidadBase(Convert.ToInt32(fila["ID"]), idEmpleado, TimeOnly.FromTimeSpan(horaInicio), TimeOnly.FromTimeSpan(horaFin), Convert.ToDecimal(fila["HorasSemanales"]), Convert.ToString(fila["Observacion"]), Convert.ToBoolean(fila["Activo"])); empleado.DisponibilidadBase = disponibilidad; }
        return empleado;
    }
    public void GuardarFichaEmpleado(int idUsuario, int idAgencia, Empleado empleado)
    {
        int idEmpleado = ConsultarIdEmpleado(idUsuario, idAgencia);
        List<SqlParameter> parametrosLimpiar = new List<SqlParameter>() { new SqlParameter("@IdEmpleado", idEmpleado) }; _conexion.Escribir("dbo.usp_EmpleadoSkill_Limpiar", parametrosLimpiar);
        foreach (EmpleadoSkill empleadoSkill in empleado.EmpleadoSkills)
        {
            List<SqlParameter> parametrosSkill = new List<SqlParameter>() { new SqlParameter("@IdEmpleado", idEmpleado), new SqlParameter("@IdSkill", empleadoSkill.IdSkill), new SqlParameter("@Nivel", (object?)empleadoSkill.Nivel ?? DBNull.Value) }; _conexion.Escribir("dbo.usp_EmpleadoSkill_Registrar", parametrosSkill);
        }
        if (empleado.DisponibilidadBase is null) { throw new ArgumentException("Completá la disponibilidad base del empleado."); }
        DisponibilidadBase disponibilidad = empleado.DisponibilidadBase;
        List<SqlParameter> parametrosDisponibilidad = new List<SqlParameter>() { new SqlParameter("@IdEmpleado", idEmpleado), new SqlParameter("@HoraInicio", disponibilidad.HoraInicio.ToTimeSpan()), new SqlParameter("@HoraFin", disponibilidad.HoraFin.ToTimeSpan()), new SqlParameter("@HorasSemanales", disponibilidad.HorasSemanales), new SqlParameter("@Observacion", (object?)disponibilidad.Observacion ?? DBNull.Value) }; _conexion.Escribir("dbo.usp_DisponibilidadBase_RegistrarActualizar", parametrosDisponibilidad);
    }
    private int ConsultarIdEmpleado(int idUsuario, int idAgencia) { List<SqlParameter> parametros = new List<SqlParameter>() { new SqlParameter("@IdUsuario", idUsuario), new SqlParameter("@IdAgencia", idAgencia) }; DataTable tabla = _conexion.Leer("dbo.usp_Empleado_ConsultarIdPorUsuarioAgencia", parametros); if (tabla.Rows.Count == 0) { throw new ArgumentException("El usuario seleccionado no es un empleado de tu agencia."); } return Convert.ToInt32(tabla.Rows[0]["ID"]); }
    private static List<Skill> CrearSkills(DataTable tabla) { List<Skill> skills = new List<Skill>(); foreach (DataRow fila in tabla.Rows) { Skill skill = new Skill(Convert.ToInt32(fila["ID"]), Convert.ToString(fila["Nombre"]) ?? string.Empty, Convert.ToString(fila["Categoria"]), Convert.ToBoolean(fila["Activo"])); skills.Add(skill); } return skills; }
    private static List<EmpleadoSkill> CrearEmpleadoSkills(DataTable tabla) { List<EmpleadoSkill> habilidades = new List<EmpleadoSkill>(); foreach (DataRow fila in tabla.Rows) { EmpleadoSkill habilidad = new EmpleadoSkill(Convert.ToInt32(fila["ID"]), Convert.ToInt32(fila["IdEmpleado"]), Convert.ToInt32(fila["IdSkill"]), Convert.ToString(fila["Nivel"]), Convert.ToBoolean(fila["Activo"])); habilidades.Add(habilidad); } return habilidades; }
}
