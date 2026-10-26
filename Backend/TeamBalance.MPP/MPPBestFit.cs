using System.Data;
using Microsoft.Data.SqlClient;
using TeamBalance.BE.Entidades;
using TeamBalance.DAL;

namespace TeamBalance.MPP;

public class MPPBestFit
{
    private readonly Conexion _conexion;
    public MPPBestFit(Conexion conexion) { _conexion = conexion; }

    public List<Empleado> ConsultarCandidatosParaBestFit(Tarea tarea, Usuario usuario)
    {
        try
        {
            List<SqlParameter> parametros = new List<SqlParameter>() { new SqlParameter("@IdTarea", tarea.ID), new SqlParameter("@IdAgencia", usuario.IdAgencia) };
            DataTable tabla = _conexion.Leer("dbo.usp_BestFit_ConsultarCandidatos", parametros);
            List<Empleado> empleados = new List<Empleado>();
            foreach (DataRow fila in tabla.Rows)
            {
                Empleado empleado = new Empleado();
                empleado.ID = Convert.ToInt32(fila["ID"]);
                empleado.Nombre = Convert.ToString(fila["Nombre"]) ?? string.Empty;
                empleado.Apellido = Convert.ToString(fila["Apellido"]) ?? string.Empty;
                empleado.Seniority = Convert.ToString(fila["Seniority"]) ?? string.Empty;
                empleado.HorasDisponiblesSemanales = Convert.ToDecimal(fila["HorasDisponiblesSemanales"]);
                empleado.HorasComprometidasSemanales = Convert.ToDecimal(fila["HorasComprometidasSemanales"]);
                empleado.CantidadTareasHistoricasComparables = Convert.ToInt32(fila["CantidadTareasHistoricasComparables"]);
                empleado.RatioHistoricoConsumo = Convert.ToDecimal(fila["RatioHistoricoConsumo"]);
                empleado.EmpleadoSkills.Add(new EmpleadoSkill(0, empleado.ID, tarea.IdSkillRequerido!.Value, Convert.ToString(fila["NivelSkill"]), true));
                empleados.Add(empleado);
            }
            return empleados;
        }
        catch (Exception ex) { throw new Exception(ex.Message); }
    }

    public bool GuardarRecomendacion(RecomendacionBestFit recomendacion, Usuario usuario)
    {
        try
        {
            List<SqlParameter> parametros = new List<SqlParameter>() { new SqlParameter("@IdTarea", recomendacion.IdTarea), new SqlParameter("@IdEmpleado", recomendacion.IdEmpleadoSugerido), new SqlParameter("@IdUsuario", usuario.ID), new SqlParameter("@PuntajeCompatibilidad", recomendacion.PuntajeCompatibilidad) };
            bool resultado = _conexion.Escribir("dbo.usp_BestFit_GuardarRecomendacion", parametros);
            return resultado;
        }
        catch (Exception ex) { throw new Exception(ex.Message); }
    }

    public List<RecomendacionBestFit> ConsultarRecomendaciones(Tarea tarea, Usuario usuario)
    {
        try
        {
            List<SqlParameter> parametros = new List<SqlParameter>() { new SqlParameter("@IdTarea", tarea.ID), new SqlParameter("@IdUsuario", usuario.ID) };
            DataTable tabla = _conexion.Leer("dbo.usp_BestFit_ConsultarRecomendaciones", parametros);
            List<RecomendacionBestFit> recomendaciones = new List<RecomendacionBestFit>();
            foreach (DataRow fila in tabla.Rows)
            {
                RecomendacionBestFit recomendacion = new RecomendacionBestFit();
                recomendacion.ID = Convert.ToInt32(fila["ID"]);
                recomendacion.IdTarea = Convert.ToInt32(fila["IdTarea"]);
                recomendacion.IdEmpleadoSugerido = Convert.ToInt32(fila["IdEmpleadoSugerido"]);
                recomendacion.IdUsuarioSolicitante = Convert.ToInt32(fila["IdUsuarioSolicitante"]);
                recomendacion.PuntajeCompatibilidad = Convert.ToDecimal(fila["PuntajeCompatibilidad"]);
                recomendacion.FechaGeneracion = Convert.ToDateTime(fila["FechaGeneracion"]);
                recomendacion.SeleccionadaPorPM = Convert.ToBoolean(fila["SeleccionadaPorPM"]);
                recomendacion.Estado = Convert.ToString(fila["Estado"]);
                recomendacion.Activo = Convert.ToBoolean(fila["Activo"]);
                recomendacion.NombreEmpleado = Convert.ToString(fila["NombreEmpleado"]) ?? string.Empty;
                recomendacion.SeniorityEmpleado = Convert.ToString(fila["SeniorityEmpleado"]) ?? string.Empty;
                recomendacion.HorasDisponibles = Convert.ToDecimal(fila["HorasDisponibles"]);
                recomendaciones.Add(recomendacion);
            }
            return recomendaciones;
        }
        catch (Exception ex) { throw new Exception(ex.Message); }
    }
}
