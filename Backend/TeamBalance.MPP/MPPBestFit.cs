using System.Data;
using Microsoft.Data.SqlClient;
using TeamBalance.BE.Entidades;
using TeamBalance.DAL;

namespace TeamBalance.MPP;

public class MPPBestFit
{
    private readonly Conexion _conexion;
    public MPPBestFit(Conexion conexion) { _conexion = conexion; }

    public List<RecomendacionBestFit> Sugerir(RecomendacionBestFit recomendacion, Usuario usuario)
    {
        try
        {
            List<SqlParameter> parametros = new List<SqlParameter>() { new SqlParameter("@IdTarea", recomendacion.IdTarea), new SqlParameter("@IdUsuario", usuario.ID) };
            DataTable tabla = _conexion.Leer("dbo.usp_BestFit_Sugerir", parametros);
            List<RecomendacionBestFit> recomendaciones = new List<RecomendacionBestFit>();
            foreach (DataRow fila in tabla.Rows)
            {
                RecomendacionBestFit resultado = new RecomendacionBestFit(Convert.ToInt32(fila["ID"]), Convert.ToInt32(fila["IdTarea"]), Convert.ToInt32(fila["IdEmpleadoSugerido"]), Convert.ToInt32(fila["IdUsuarioSolicitante"]), Convert.ToDecimal(fila["PuntajeCompatibilidad"]), Convert.ToDateTime(fila["FechaGeneracion"]), Convert.ToBoolean(fila["SeleccionadaPorPM"]), Convert.ToString(fila["Estado"]), Convert.ToBoolean(fila["Activo"]), Convert.ToString(fila["NombreEmpleado"]) ?? string.Empty, Convert.ToString(fila["SeniorityEmpleado"]) ?? string.Empty, Convert.ToDecimal(fila["HorasDisponibles"]));
                recomendaciones.Add(resultado);
            }
            return recomendaciones;
        }
        catch (Exception ex) { throw new Exception(ex.Message); }
    }

    public bool ConfirmarAsignacion(RecomendacionBestFit recomendacion, Usuario usuario)
    {
        try
        {
            List<SqlParameter> parametros = new List<SqlParameter>() { new SqlParameter("@ID", recomendacion.ID), new SqlParameter("@IdUsuario", usuario.ID) };
            bool resultado = _conexion.Escribir("dbo.usp_BestFit_ConfirmarAsignacion", parametros);
            return resultado;
        }
        catch (Exception ex) { throw new Exception(ex.Message); }
    }
}
