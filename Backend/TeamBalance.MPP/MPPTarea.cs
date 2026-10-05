using System.Data;
using Microsoft.Data.SqlClient;
using TeamBalance.BE.Entidades;
using TeamBalance.DAL;

namespace TeamBalance.MPP;

public class MPPTarea
{
    private readonly Conexion _conexion;
    public MPPTarea(Conexion conexion) { _conexion = conexion; }
    public List<Tarea> Consultar(Usuario usuario) {
        FiltroTarea filtro = new FiltroTarea();
        return Consultar(usuario, filtro);
    }
    public List<Tarea> Consultar(Usuario usuario, FiltroTarea filtro) {
        try
        {
            List<SqlParameter> parametros = CrearParametrosFiltro(usuario, filtro);
            List<Tarea> tareas = CrearTareas(_conexion.Leer("dbo.usp_Tarea_Consultar", parametros));
            return tareas;
        }
        catch (Exception ex) { throw new Exception(ex.Message); }
    }
    public List<Tarea> ConsultarPorEmpleado(Usuario usuario)
    {
        try
        {
            List<SqlParameter> parametros = new List<SqlParameter>() { new SqlParameter("@IdUsuario", usuario.ID) };
            return CrearTareas(_conexion.Leer("dbo.usp_Tarea_ConsultarPorEmpleado", parametros));
        }
        catch (Exception ex) { throw new Exception(ex.Message); }
    }
    public List<Tarea> ConsultarPorPM(Usuario usuario)
    {
        try
        {
            List<SqlParameter> parametros = new List<SqlParameter>() { new SqlParameter("@IdUsuario", usuario.ID) };
            List<Tarea> tareas = CrearTareas(_conexion.Leer("dbo.usp_Tarea_ConsultarPorPM", parametros));
            return tareas;
        }
        catch (Exception ex) { throw new Exception(ex.Message); }
    }
    public List<Tarea> ConsultarTableroProyectoPropio(Tarea tarea, Usuario usuario)
    {
        try
        {
            List<SqlParameter> parametros = new List<SqlParameter>() { new SqlParameter("@IdProyecto", tarea.IdProyecto), new SqlParameter("@IdUsuario", usuario.ID) };
            return CrearTareas(_conexion.Leer("dbo.usp_Tarea_ConsultarTableroProyectoEmpleado", parametros));
        }
        catch (Exception ex) { throw new Exception(ex.Message); }
    }
    public bool GuardarComentarios(Tarea tarea, Usuario usuario) {
        try
        {
            List<SqlParameter> parametros = new List<SqlParameter>() { new SqlParameter("@ID", tarea.ID), new SqlParameter("@IdUsuario", usuario.ID), new SqlParameter("@ComentariosJson", tarea.ComentariosJson ?? "[]") };
            return _conexion.Escribir("dbo.usp_Tarea_ActualizarComentarios", parametros);
        }
        catch (Exception ex) { throw new Exception(ex.Message); }
    }
    public Tarea Guardar(Tarea tarea, Usuario usuario)
    {
        try
        {
            List<SqlParameter> parametros = CrearParametros(tarea, usuario);
            DataTable tabla = _conexion.Leer(tarea.ID == 0 ? "dbo.usp_Tarea_Registrar" : "dbo.usp_Tarea_Modificar", parametros);
            return CrearTareas(tabla).Single();
        }
        catch (Exception ex) { throw new Exception(ex.Message); }
    }
    public bool CambiarEstado(Tarea tarea, Usuario usuario)
    {
        try
        {
            List<SqlParameter> parametros = new List<SqlParameter>() { new SqlParameter("@ID", tarea.ID), new SqlParameter("@IdAgencia", usuario.IdAgencia), new SqlParameter("@Activo", tarea.Activo) };
            return _conexion.Escribir("dbo.usp_Tarea_CambiarEstado", parametros);
        }
        catch (Exception ex) { throw new Exception(ex.Message); }
    }

    public bool CambiarEstadoPropio(Tarea tarea, Usuario usuario)
    {
        try
        {
            List<SqlParameter> parametros = new List<SqlParameter>() { new SqlParameter("@ID", tarea.ID), new SqlParameter("@IdUsuario", usuario.ID), new SqlParameter("@Estado", tarea.Estado ?? string.Empty) };
            bool resultado = _conexion.Escribir("dbo.usp_Tarea_ActualizarEstadoEmpleado", parametros);
            return resultado;
        }
        catch (Exception ex) { throw new Exception(ex.Message); }
    }

    public Tarea ActualizarAvancePropio(Tarea tarea, Usuario usuario)
    {
        try
        {
            List<SqlParameter> parametros = new List<SqlParameter>() {
                new SqlParameter("@ID", tarea.ID),
                new SqlParameter("@IdUsuario", usuario.ID),
                new SqlParameter("@Estado", tarea.Estado ?? string.Empty),
                new SqlParameter("@ChecklistJson", (object?)tarea.ChecklistJson ?? DBNull.Value),
                new SqlParameter("@PorcentajeAvance", tarea.PorcentajeAvance),
                new SqlParameter("@MotivoBloqueo", (object?)tarea.MotivoBloqueo ?? DBNull.Value)
            };
            return CrearTareas(_conexion.Leer("dbo.usp_Tarea_ActualizarAvanceEmpleado", parametros)).Single();
        }
        catch (Exception ex) { throw new Exception(ex.Message); }
    }

    public GestionTareaOpciones ConsultarOpciones(Usuario usuario)
    {
        try
        {
            List<SqlParameter> parametrosProyectos = new List<SqlParameter>() { new SqlParameter("@IdAgencia", usuario.IdAgencia) };
            List<SqlParameter> parametrosEstados = new List<SqlParameter>() { new SqlParameter("@IdAgencia", usuario.IdAgencia) };
            List<SqlParameter> parametrosEmpleados = new List<SqlParameter>() { new SqlParameter("@IdAgencia", usuario.IdAgencia) };
            List<Proyecto> proyectos = CrearProyectos(_conexion.Leer("dbo.usp_Proyecto_Consultar", parametrosProyectos));
            List<EstadoTarea> estados = CrearEstados(_conexion.Leer("dbo.usp_EstadoTarea_Consultar", parametrosEstados));
            List<Usuario> empleados = CrearUsuarios(_conexion.Leer("dbo.usp_Tarea_ConsultarEmpleados", parametrosEmpleados));
            List<Skill> skills = CrearSkills(_conexion.Leer("dbo.usp_Skill_Consultar"));
            GestionTareaOpciones opciones = new GestionTareaOpciones(proyectos, estados, empleados, skills); return opciones;
        }
        catch (Exception ex) { throw new Exception(ex.Message); }
    }
    private static List<SqlParameter> CrearParametros(Tarea tarea, Usuario usuario)
    {
        try
        {
            List<SqlParameter> parametros = new List<SqlParameter>() { new SqlParameter("@ID", tarea.ID), new SqlParameter("@IdAgencia", usuario.IdAgencia), new SqlParameter("@IdProyecto", tarea.IdProyecto), new SqlParameter("@IdTareaPredecesora", (object?)tarea.IdTareaPredecesora ?? DBNull.Value), new SqlParameter("@IdEmpleadoAsignado", (object?)tarea.IdEmpleadoAsignado ?? DBNull.Value), new SqlParameter("@IdSkillRequerido", (object?)tarea.IdSkillRequerido ?? DBNull.Value), new SqlParameter("@IdEstadoTarea", tarea.IdEstadoTarea), new SqlParameter("@Titulo", tarea.Titulo), new SqlParameter("@Descripcion", (object?)tarea.Descripcion ?? DBNull.Value), new SqlParameter("@Prioridad", (object?)tarea.Prioridad ?? DBNull.Value), new SqlParameter("@Complejidad", (object?)tarea.Complejidad ?? DBNull.Value), new SqlParameter("@FechaInicio", (object?)tarea.FechaInicio ?? DBNull.Value), new SqlParameter("@Deadline", (object?)tarea.Deadline ?? DBNull.Value), new SqlParameter("@SeniorityRequerido", (object?)tarea.SeniorityRequerido ?? DBNull.Value), new SqlParameter("@PorcentajeAvance", tarea.PorcentajeAvance), new SqlParameter("@HorasEstimadas", tarea.HorasEstimadas), new SqlParameter("@ChecklistJson", (object?)tarea.ChecklistJson ?? DBNull.Value), new SqlParameter("@ArchivosAdjuntosJson", (object?)tarea.ArchivosAdjuntosJson ?? DBNull.Value) };
            return parametros;
        }
        catch (Exception ex) { throw new Exception(ex.Message); }
    }
    private static List<Tarea> CrearTareas(DataTable tabla) {
        try
        {
            List<Tarea> tareas = new List<Tarea>(); foreach (DataRow fila in tabla.Rows) { Tarea tarea = new Tarea(Convert.ToInt32(fila["ID"]), Convert.ToInt32(fila["IdProyecto"]), fila["IdEmpleadoAsignado"] == DBNull.Value ? null : Convert.ToInt32(fila["IdEmpleadoAsignado"]), fila["IdSkillRequerido"] == DBNull.Value ? null : Convert.ToInt32(fila["IdSkillRequerido"]), Convert.ToInt32(fila["IdEstadoTarea"]), fila["IdTareaPredecesora"] == DBNull.Value ? null : Convert.ToInt32(fila["IdTareaPredecesora"]), Convert.ToString(fila["Titulo"]) ?? string.Empty, Convert.ToString(fila["Descripcion"]), Convert.ToString(fila["Estado"]), Convert.ToString(fila["Prioridad"]), Convert.ToString(fila["Complejidad"]), fila["FechaInicio"] == DBNull.Value ? null : Convert.ToDateTime(fila["FechaInicio"]), fila["Deadline"] == DBNull.Value ? null : Convert.ToDateTime(fila["Deadline"]), fila["FechaFinReal"] == DBNull.Value ? null : Convert.ToDateTime(fila["FechaFinReal"]), Convert.ToString(fila["SeniorityRequerido"]), Convert.ToString(fila["ChecklistJson"]), Convert.ToString(fila["ComentariosJson"]), Convert.ToString(fila["ArchivosAdjuntosJson"]), Convert.ToBoolean(fila["Bloqueada"]), Convert.ToString(fila["MotivoBloqueo"]), Convert.ToDecimal(fila["PorcentajeAvance"]), Convert.ToDecimal(fila["HorasEstimadas"]), Convert.ToBoolean(fila["Activo"]), fila["FechaBaja"] == DBNull.Value ? null : Convert.ToDateTime(fila["FechaBaja"])); tareas.Add(tarea); }
            return tareas;
        }
        catch (Exception ex) { throw new Exception(ex.Message); }
    }
    private static List<SqlParameter> CrearParametrosFiltro(Usuario usuario, FiltroTarea filtro)
    {
        try
        {
            List<SqlParameter> parametros = new List<SqlParameter>() { new SqlParameter("@IdAgencia", usuario.IdAgencia), new SqlParameter("@IdProyecto", (object?)filtro.IdProyecto ?? DBNull.Value), new SqlParameter("@Estado", (object?)filtro.Estado ?? DBNull.Value), new SqlParameter("@Prioridad", (object?)filtro.Prioridad ?? DBNull.Value), new SqlParameter("@IdEmpleadoAsignado", (object?)filtro.IdEmpleadoAsignado ?? DBNull.Value), new SqlParameter("@IdSkillRequerido", (object?)filtro.IdSkillRequerido ?? DBNull.Value), new SqlParameter("@DeadlineDesde", (object?)filtro.DeadlineDesde ?? DBNull.Value), new SqlParameter("@DeadlineHasta", (object?)filtro.DeadlineHasta ?? DBNull.Value) };
            return parametros;
        }
        catch (Exception ex) { throw new Exception(ex.Message); }
    }
    private static List<Proyecto> CrearProyectos(DataTable tabla) {
        List<Proyecto> proyectos = new List<Proyecto>();
        foreach (DataRow fila in tabla.Rows) {
            Proyecto proyecto = new Proyecto(Convert.ToInt32(fila["ID"]), Convert.ToInt32(fila["IdAgencia"]), Convert.ToInt32(fila["IdCliente"]), Convert.ToInt32(fila["IdPMResponsable"]), Convert.ToString(fila["Nombre"]) ?? string.Empty, Convert.ToString(fila["Descripcion"]), null, null, Convert.ToDecimal(fila["HorasEstimadasTotales"]), Convert.ToString(fila["Estado"]) ?? string.Empty, Convert.ToBoolean(fila["Activo"]), Convert.ToDateTime(fila["FechaAlta"]), null);
            proyectos.Add(proyecto);
        }
        return proyectos;
    }
    private static List<EstadoTarea> CrearEstados(DataTable tabla)
    {
        try
        {
            List<EstadoTarea> estados = new List<EstadoTarea>(); 
            
            foreach (DataRow fila in tabla.Rows) { 
                
                EstadoTarea estado = new EstadoTarea(Convert.ToInt32(fila["ID"]), Convert.ToInt32(fila["IdAgencia"]), Convert.ToString(fila["Nombre"]) ?? string.Empty, Convert.ToInt32(fila["Orden"]), Convert.ToBoolean(fila["EsBase"]), Convert.ToBoolean(fila["EsFinal"]), Convert.ToBoolean(fila["Activo"])); 
                estados.Add(estado); 
            
            }
            return estados;
        }
        catch (Exception ex) { throw new Exception(ex.Message); }
    }

    private static List<Usuario> CrearUsuarios(DataTable tabla)
    {
        try
        {
            List<Usuario> usuarios = new List<Usuario>();
            foreach (DataRow fila in tabla.Rows) {
                Usuario usuario = new Usuario(Convert.ToInt32(fila["ID"]), Convert.ToInt32(fila["IdAgencia"]), new Rol(), Convert.ToString(fila["Nombre"]) ?? string.Empty, Convert.ToString(fila["Apellido"]) ?? string.Empty, Convert.ToString(fila["Email"]) ?? string.Empty, string.Empty, string.Empty, DateTime.MinValue, true);
                usuarios.Add(usuario);  
            
            
            }
            return usuarios;
        }
        catch (Exception ex) { throw new Exception(ex.Message); }
    }

    private static List<Skill> CrearSkills(DataTable tabla)
    {
        try
        {
            List<Skill> skills = new List<Skill>(); 
            foreach (DataRow fila in tabla.Rows) { 
                Skill skill = new Skill(Convert.ToInt32(fila["ID"]), Convert.ToString(fila["Nombre"]) ?? string.Empty, Convert.ToString(fila["Categoria"]), Convert.ToBoolean(fila["Activo"])); 
                skills.Add(skill); 
            }
            return skills;
        }
        catch (Exception ex) { throw new Exception(ex.Message); }
    } 
}
