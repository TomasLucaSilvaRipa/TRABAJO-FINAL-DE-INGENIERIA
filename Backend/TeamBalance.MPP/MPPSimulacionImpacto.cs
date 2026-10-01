using System.Data;
using Microsoft.Data.SqlClient;
using TeamBalance.BE.Entidades;
using TeamBalance.DAL;

namespace TeamBalance.MPP;

public class MPPSimulacionImpacto
{
    private readonly Conexion _conexion;
    public MPPSimulacionImpacto(Conexion conexion) { _conexion = conexion; }
    public SimulacionImpacto Simular(SimulacionImpacto simulacion, Usuario usuario)
    {
        try
        {
            List<SqlParameter> parametros = new List<SqlParameter>() { new SqlParameter("@IdTarea", simulacion.IdTarea), new SqlParameter("@IdEmpleadoCandidato", simulacion.IdEmpleadoCandidato), new SqlParameter("@IdUsuario", usuario.ID) };
            DataTable tabla = _conexion.Leer("dbo.usp_SimulacionImpacto_Crear", parametros);
            DataRow fila = tabla.Rows[0];
            SimulacionImpacto resultado = new SimulacionImpacto(Convert.ToInt32(fila["ID"]), Convert.ToInt32(fila["IdTarea"]), Convert.ToInt32(fila["IdEmpleadoCandidato"]), Convert.ToInt32(fila["IdUsuarioCreador"]), Convert.ToDecimal(fila["CargaActual"]), Convert.ToDecimal(fila["CargaProyectada"]), Convert.ToDecimal(fila["DisponibilidadRestante"]), Convert.ToDecimal(fila["PorcentajeOcupacionActual"]), Convert.ToDecimal(fila["PorcentajeOcupacionProyectado"]), Convert.ToBoolean(fila["GeneraSobrecarga"]), Convert.ToString(fila["AdvertenciasJson"]), Convert.ToString(fila["ImpactoOperativo"]), Convert.ToDateTime(fila["FechaCreacion"]), null, Convert.ToDateTime(fila["FechaExpiracion"]), Convert.ToBoolean(fila["Activo"]));
            return resultado;
        }
        catch (Exception ex) { throw new Exception(ex.Message); }
    }
}
