using System.Data;
using System.Text.Json;
using Microsoft.Data.SqlClient;
using TeamBalance.BE.Entidades;
using TeamBalance.DAL;

namespace TeamBalance.MPP;

/// <summary>
/// Acceso a la relación ya existente entre una tarea y sus empleados asignados.
/// No crea un tipo nuevo de persona: todas las filas refieren a Empleado.
/// </summary>
public class MPPAsignacionTarea
{
    private readonly Conexion _conexion;

    public MPPAsignacionTarea(Conexion conexion) => _conexion = conexion;

    public List<AsignacionTarea> Consultar(Tarea tarea, Usuario usuario)
    {
        DataTable tabla = _conexion.Leer("dbo.usp_AsignacionTarea_Consultar", new List<SqlParameter>
        {
            new("@IdTarea", tarea.ID), new("@IdAgencia", usuario.IdAgencia)
        });

        return tabla.Rows.Cast<DataRow>().Select(fila => new AsignacionTarea(
            Convert.ToInt32(fila["ID"]),
            Convert.ToInt32(fila["IdTarea"]),
            Convert.ToInt32(fila["IdEmpleado"]),
            Convert.ToInt32(fila["IdUsuarioAsignador"]),
            Convert.ToDateTime(fila["FechaDesde"]),
            fila["FechaHasta"] == DBNull.Value ? null : Convert.ToDateTime(fila["FechaHasta"]),
            Convert.ToString(fila["Motivo"]),
            Convert.ToDecimal(fila["HorasAsignadas"]),
            Convert.ToBoolean(fila["Activa"]))).ToList();
    }

    public List<AsignacionTarea> ConsultarActivas(IEnumerable<int> idsTarea, Usuario usuario)
    {
        List<int> ids = idsTarea.Distinct().Where(id => id > 0).ToList();
        if (ids.Count == 0) return new List<AsignacionTarea>();

        DataTable tabla = _conexion.Leer("dbo.usp_AsignacionTarea_ConsultarActivas", new List<SqlParameter>
        {
            new("@IdsTareaJson", JsonSerializer.Serialize(ids)), new("@IdAgencia", usuario.IdAgencia)
        });

        return tabla.Rows.Cast<DataRow>().Select(fila => new AsignacionTarea(
            Convert.ToInt32(fila["ID"]), Convert.ToInt32(fila["IdTarea"]), Convert.ToInt32(fila["IdEmpleado"]),
            Convert.ToInt32(fila["IdUsuarioAsignador"]), Convert.ToDateTime(fila["FechaDesde"]),
            fila["FechaHasta"] == DBNull.Value ? null : Convert.ToDateTime(fila["FechaHasta"]), Convert.ToString(fila["Motivo"]),
            Convert.ToDecimal(fila["HorasAsignadas"]), Convert.ToBoolean(fila["Activa"]))).ToList();
    }

    /// <summary>
    /// Obtiene únicamente la capacidad semanal configurada de los empleados
    /// seleccionados. La asignación de horas se resuelve en la BLL; este método
    /// sólo recupera el dato persistido necesario para calcularla.
    /// </summary>
    public List<Empleado> ConsultarCapacidades(IEnumerable<int> idsEmpleado, Usuario usuario)
    {
        List<int> ids = idsEmpleado.Distinct().Where(id => id > 0).ToList();
        if (ids.Count == 0) return new List<Empleado>();

        DataTable tabla = _conexion.Leer("dbo.usp_AsignacionTarea_ConsultarCapacidades", new List<SqlParameter>
        {
            new("@IdsEmpleadoJson", JsonSerializer.Serialize(ids)), new("@IdAgencia", usuario.IdAgencia)
        });

        return tabla.Rows.Cast<DataRow>().Select(fila => new Empleado(Convert.ToInt32(fila["ID"]), Convert.ToDecimal(fila["HorasDisponiblesSemanales"]))).ToList();
    }

    public List<AsignacionTarea> Sincronizar(Tarea tarea, IEnumerable<AsignacionTarea> asignaciones, Usuario usuario)
    {
        List<AsignacionTareaSincronizacion> datosAsignaciones = asignaciones.Select(item => new AsignacionTareaSincronizacion(item)).ToList();
        string json = JsonSerializer.Serialize(datosAsignaciones);

        DataTable tabla = _conexion.Leer("dbo.usp_AsignacionTarea_Sincronizar", new List<SqlParameter>
        {
            new("@IdTarea", tarea.ID), new("@IdAgencia", usuario.IdAgencia), new("@IdUsuarioAsignador", usuario.ID), new("@AsignacionesJson", json)
        });

        return tabla.Rows.Cast<DataRow>().Select(fila => new AsignacionTarea(
            Convert.ToInt32(fila["ID"]), Convert.ToInt32(fila["IdTarea"]), Convert.ToInt32(fila["IdEmpleado"]),
            Convert.ToInt32(fila["IdUsuarioAsignador"]), Convert.ToDateTime(fila["FechaDesde"]),
            fila["FechaHasta"] == DBNull.Value ? null : Convert.ToDateTime(fila["FechaHasta"]), Convert.ToString(fila["Motivo"]),
            Convert.ToDecimal(fila["HorasAsignadas"]), Convert.ToBoolean(fila["Activa"]))).ToList();
    }
}
