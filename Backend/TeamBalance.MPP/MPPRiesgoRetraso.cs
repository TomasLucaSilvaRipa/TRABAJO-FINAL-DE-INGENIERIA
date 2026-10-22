using System.Data;
using Microsoft.Data.SqlClient;
using TeamBalance.BE.Entidades;
using TeamBalance.DAL;

namespace TeamBalance.MPP;

public class MPPRiesgoRetraso
{
    private readonly Conexion _conexion;

    public MPPRiesgoRetraso(Conexion conexion)
    {
        _conexion = conexion;
    }

    public List<Proyecto> ConsultarProyectosActivos(Usuario usuario, FiltroRiesgoRetraso filtro)
    {
        try
        {
            List<SqlParameter> parametros = new List<SqlParameter>() { new SqlParameter("@IdAgencia", usuario.IdAgencia), new SqlParameter("@IdProyecto", (object?)filtro.IdProyecto ?? DBNull.Value), new SqlParameter("@IdCliente", (object?)filtro.IdCliente ?? DBNull.Value), new SqlParameter("@IdPM", (object?)filtro.IdPM ?? DBNull.Value) };
            DataTable tabla = _conexion.Leer("dbo.usp_RiesgoRetraso_ConsultarProyectosActivos", parametros);
            List<Proyecto> proyectos = CrearProyectos(tabla);
            return proyectos;
        }
        catch (Exception ex) { throw new Exception(ex.Message); }
    }

    public List<Tarea> ConsultarTareasProyecto(Proyecto proyecto)
    {
        try
        {
            List<SqlParameter> parametros = new List<SqlParameter>() { new SqlParameter("@IdProyecto", proyecto.ID) };
            DataTable tabla = _conexion.Leer("dbo.usp_RiesgoRetraso_ConsultarTareasProyecto", parametros);
            List<Tarea> tareas = CrearTareas(tabla);
            return tareas;
        }
        catch (Exception ex) { throw new Exception(ex.Message); }
    }

    public List<Empleado> ConsultarEquipoProyecto(Proyecto proyecto)
    {
        try
        {
            List<SqlParameter> parametros = new List<SqlParameter>() { new SqlParameter("@IdProyecto", proyecto.ID) };
            DataTable tabla = _conexion.Leer("dbo.usp_RiesgoRetraso_ConsultarEquipoProyecto", parametros);
            List<Empleado> empleados = CrearEmpleados(tabla);
            return empleados;
        }
        catch (Exception ex) { throw new Exception(ex.Message); }
    }

    public List<RegistroHora> ConsultarRegistrosHora(Proyecto proyecto)
    {
        try
        {
            List<SqlParameter> parametros = new List<SqlParameter>() { new SqlParameter("@IdProyecto", proyecto.ID) };
            DataTable tabla = _conexion.Leer("dbo.usp_RiesgoRetraso_ConsultarRegistrosHora", parametros);
            List<RegistroHora> registros = CrearRegistrosHora(tabla);
            return registros;
        }
        catch (Exception ex) { throw new Exception(ex.Message); }
    }

    public int RegistrarSeguimiento(Usuario usuario, NuevoSeguimientoRiesgoRetraso seguimiento)
    {
        List<SqlParameter> parametros = new List<SqlParameter>()
        {
            new SqlParameter("@IdAgencia", usuario.IdAgencia ?? throw new UnauthorizedAccessException("Tu usuario no pertenece a una agencia.")),
            new SqlParameter("@IdProyecto", seguimiento.IdProyecto), new SqlParameter("@IdTarea", (object?)seguimiento.IdTarea ?? DBNull.Value),
            new SqlParameter("@IdUsuario", usuario.ID), new SqlParameter("@Observacion", seguimiento.Observacion), new SqlParameter("@ComunicarResponsable", seguimiento.ComunicarResponsable)
        };
        DataTable tabla = _conexion.Leer("dbo.usp_RiesgoRetraso_RegistrarSeguimiento", parametros);
        if (tabla.Rows.Count == 0) { throw new InvalidOperationException("No fue posible registrar el seguimiento del riesgo."); }
        return Convert.ToInt32(tabla.Rows[0]["ID"]);
    }

    public DestinatarioDesvioHoras? ConsultarDestinatarioSeguimiento(int idProyecto, int? idTarea)
    {
        List<SqlParameter> parametros = new List<SqlParameter>() { new SqlParameter("@IdProyecto", idProyecto), new SqlParameter("@IdTarea", (object?)idTarea ?? DBNull.Value) };
        DataTable tabla = _conexion.Leer("dbo.usp_RiesgoRetraso_ConsultarDestinatarioSeguimiento", parametros);
        if (tabla.Rows.Count == 0) { return null; }
        return new DestinatarioDesvioHoras(Convert.ToInt32(tabla.Rows[0]["ID"]), Convert.ToString(tabla.Rows[0]["Nombre"]) ?? string.Empty, Convert.ToString(tabla.Rows[0]["Email"]) ?? string.Empty);
    }

    private static List<Proyecto> CrearProyectos(DataTable tabla)
    {
        try
        {
            List<Proyecto> proyectos = new List<Proyecto>();
            foreach (DataRow fila in tabla.Rows)
            {
                Proyecto proyecto = new Proyecto(Convert.ToInt32(fila["ID"]), Convert.ToInt32(fila["IdAgencia"]), Convert.ToInt32(fila["IdCliente"]), Convert.ToInt32(fila["IdPMResponsable"]), Convert.ToString(fila["Nombre"]) ?? string.Empty, Convert.ToString(fila["Descripcion"]), fila["FechaInicio"] == DBNull.Value ? null : Convert.ToDateTime(fila["FechaInicio"]), fila["Deadline"] == DBNull.Value ? null : Convert.ToDateTime(fila["Deadline"]), Convert.ToDecimal(fila["HorasEstimadasTotales"]), Convert.ToString(fila["Estado"]) ?? string.Empty, Convert.ToBoolean(fila["Activo"]), Convert.ToDateTime(fila["FechaAlta"]), fila["FechaBaja"] == DBNull.Value ? null : Convert.ToDateTime(fila["FechaBaja"]), Convert.ToString(fila["NombreCliente"]), Convert.ToString(fila["NombrePMResponsable"]));
                proyectos.Add(proyecto);
            }
            return proyectos;
        }
        catch (Exception ex) { throw new Exception(ex.Message); }
    }

    private static List<Tarea> CrearTareas(DataTable tabla)
    {
        try
        {
            List<Tarea> tareas = new List<Tarea>();
            foreach (DataRow fila in tabla.Rows)
            {
                string estadoTarea = fila.Table.Columns.Contains("NombreEstadoTarea") ? Convert.ToString(fila["NombreEstadoTarea"]) ?? string.Empty : Convert.ToString(fila["Estado"]) ?? string.Empty;
                Tarea tarea = new Tarea(Convert.ToInt32(fila["ID"]), Convert.ToInt32(fila["IdProyecto"]), fila["IdEmpleadoAsignado"] == DBNull.Value ? null : Convert.ToInt32(fila["IdEmpleadoAsignado"]), fila["IdSkillRequerido"] == DBNull.Value ? null : Convert.ToInt32(fila["IdSkillRequerido"]), Convert.ToInt32(fila["IdEstadoTarea"]), null, Convert.ToString(fila["Titulo"]) ?? string.Empty, Convert.ToString(fila["Descripcion"]), estadoTarea, Convert.ToString(fila["Prioridad"]), Convert.ToString(fila["Complejidad"]), fila["FechaInicio"] == DBNull.Value ? null : Convert.ToDateTime(fila["FechaInicio"]), fila["Deadline"] == DBNull.Value ? null : Convert.ToDateTime(fila["Deadline"]), null, Convert.ToString(fila["SeniorityRequerido"]), null, Convert.ToString(fila["ComentariosJson"]), Convert.ToString(fila["ArchivosAdjuntosJson"]), Convert.ToBoolean(fila["Bloqueada"]), Convert.ToString(fila["MotivoBloqueo"]), Convert.ToDecimal(fila["PorcentajeAvance"]), Convert.ToDecimal(fila["HorasEstimadas"]), Convert.ToBoolean(fila["Activo"]), fila["FechaBaja"] == DBNull.Value ? null : Convert.ToDateTime(fila["FechaBaja"]));
                tareas.Add(tarea);
            }
            return tareas;
        }
        catch (Exception ex) { throw new Exception(ex.Message); }
    }

    private static List<Empleado> CrearEmpleados(DataTable tabla)
    {
        try
        {
            List<Empleado> empleados = new List<Empleado>();
            foreach (DataRow fila in tabla.Rows)
            {
                Empleado empleado = new Empleado(Convert.ToInt32(fila["ID"]), Convert.ToString(fila["Nombre"]) ?? string.Empty, Convert.ToString(fila["Email"]) ?? string.Empty, Convert.ToBoolean(fila["Activo"]), Convert.ToDecimal(fila["CostoHora"]), Convert.ToDecimal(fila["HorasDisponiblesSemanales"]), Convert.ToString(fila["Seniority"]) ?? string.Empty, Convert.ToString(fila["EstadoLaboral"]) ?? string.Empty, fila["FechaIngreso"] == DBNull.Value ? null : Convert.ToDateTime(fila["FechaIngreso"]), new List<Skill>());
                empleados.Add(empleado);
            }
            return empleados;
        }
        catch (Exception ex) { throw new Exception(ex.Message); }
    }

    private static List<RegistroHora> CrearRegistrosHora(DataTable tabla)
    {
        try
        {
            List<RegistroHora> registros = new List<RegistroHora>();
            foreach (DataRow fila in tabla.Rows)
            {
                RegistroHora registro = new RegistroHora(Convert.ToInt32(fila["ID"]), Convert.ToInt32(fila["IdTarea"]), Convert.ToInt32(fila["IdEmpleado"]), Convert.ToDateTime(fila["Fecha"]), Convert.ToDecimal(fila["CantidadHoras"]), Convert.ToString(fila["Descripcion"]), Convert.ToBoolean(fila["Activo"]));
                registros.Add(registro);
            }
            return registros;
        }
        catch (Exception ex) { throw new Exception(ex.Message); }
    }
}
