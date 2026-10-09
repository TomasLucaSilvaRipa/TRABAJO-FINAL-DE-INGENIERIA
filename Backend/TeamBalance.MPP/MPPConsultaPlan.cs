using System.Data;
using Microsoft.Data.SqlClient;
using TeamBalance.BE.Entidades;
using TeamBalance.DAL;

namespace TeamBalance.MPP;

public class MPPConsultaPlan
{
    private readonly Conexion _conexion;
    public MPPConsultaPlan(Conexion conexion)
    {
        _conexion = conexion;
    }

    public List<ConsultaPlan> Consultar(PlanComercial planComercial)
    {
        try
        {
            List<SqlParameter> parametros = new List<SqlParameter>() { new SqlParameter("@IdPlanComercial", planComercial.ID) };
            DataTable tabla = _conexion.Leer("dbo.usp_ConsultaPlan_ConsultarPublicas", parametros);
            List<ConsultaPlan> consultas = new List<ConsultaPlan>();
            foreach (DataRow fila in tabla.Rows)
            {
                ConsultaPlan consulta = CrearConsulta(fila);
                consultas.Add(consulta);
            }
            return consultas;
        }
        catch (KeyNotFoundException ex) { throw new KeyNotFoundException(ex.Message); }
        catch (Exception ex) { throw new Exception(ex.Message); }
    }

    public ConsultaPlan Registrar(ConsultaPlan consulta)
    {
        try
        {
            List<SqlParameter> parametros = new List<SqlParameter>() { new SqlParameter("@IdPlanComercial", consulta.IdPlanComercial), new SqlParameter("@Nombre", consulta.Nombre), new SqlParameter("@Email", consulta.Email), new SqlParameter("@Consulta", consulta.Consulta) };
            DataTable tabla = _conexion.Leer("dbo.usp_ConsultaPlan_Registrar", parametros);
            if (tabla.Rows.Count != 1) { throw new InvalidOperationException("No fue posible registrar la consulta."); }
            ConsultaPlan resultado = CrearConsulta(tabla.Rows[0]);
            return resultado;
        }
        catch (Exception ex) { throw new Exception(ex.Message); }
    }

    public List<ConsultaPlan> ConsultarBandeja(ConsultaPlan filtro)
    {
        try
        {
            List<SqlParameter> parametros = new List<SqlParameter>() { new SqlParameter("@Estado", string.IsNullOrWhiteSpace(filtro.Estado) ? DBNull.Value : filtro.Estado) };
            DataTable tabla = _conexion.Leer("dbo.usp_ConsultaPlan_ConsultarBandeja", parametros);
            List<ConsultaPlan> consultas = new List<ConsultaPlan>();
            foreach (DataRow fila in tabla.Rows)
            {
                ConsultaPlan consulta = CrearConsulta(fila);
                consultas.Add(consulta);
            }
            return consultas;
        }
        catch (Exception ex) { throw new Exception(ex.Message); }
    }

    public ConsultaPlan Responder(ConsultaPlan consulta, Usuario usuario)
    {
        try
        {
            List<SqlParameter> parametros = new List<SqlParameter>() { new SqlParameter("@IdConsultaPlan", consulta.ID), new SqlParameter("@IdUsuarioSoporte", usuario.ID), new SqlParameter("@Respuesta", consulta.Respuesta) };
            DataTable tabla = _conexion.Leer("dbo.usp_ConsultaPlan_Responder", parametros);
            if (tabla.Rows.Count != 1) { throw new KeyNotFoundException("La consulta pública indicada no existe."); }
            ConsultaPlan resultado = CrearConsulta(tabla.Rows[0]);
            return resultado;
        }
        catch (KeyNotFoundException ex) { throw new KeyNotFoundException(ex.Message); }
        catch (Exception ex) { throw new Exception(ex.Message); }
    }

    private static ConsultaPlan CrearConsulta(DataRow fila)
    {
        try
        {
            string estado = fila.Table.Columns.Contains("Estado") ? Convert.ToString(fila["Estado"]) ?? "Pendiente" : "Pendiente";
            string? respuesta = fila.Table.Columns.Contains("Respuesta") && fila["Respuesta"] != DBNull.Value ? Convert.ToString(fila["Respuesta"]) : null;
            DateTime? fechaRespuesta = fila.Table.Columns.Contains("FechaRespuesta") && fila["FechaRespuesta"] != DBNull.Value ? Convert.ToDateTime(fila["FechaRespuesta"]) : null;
            int? idUsuarioSoporte = fila.Table.Columns.Contains("IdUsuarioSoporte") && fila["IdUsuarioSoporte"] != DBNull.Value ? Convert.ToInt32(fila["IdUsuarioSoporte"]) : null;
            string? nombrePlan = fila.Table.Columns.Contains("NombrePlan") && fila["NombrePlan"] != DBNull.Value ? Convert.ToString(fila["NombrePlan"]) : null;
            string? nombreRespondedor = fila.Table.Columns.Contains("NombreRespondedor") && fila["NombreRespondedor"] != DBNull.Value ? Convert.ToString(fila["NombreRespondedor"]) : null;
            ConsultaPlan consulta = new ConsultaPlan(Convert.ToInt32(fila["ID"]), Convert.ToInt32(fila["IdPlanComercial"]), Convert.ToString(fila["Nombre"]) ?? string.Empty, Convert.ToString(fila["Email"]) ?? string.Empty, Convert.ToString(fila["Consulta"]) ?? string.Empty, Convert.ToDateTime(fila["FechaAlta"]), Convert.ToBoolean(fila["Activo"]), estado, respuesta, fechaRespuesta, idUsuarioSoporte, nombrePlan, nombreRespondedor);
            return consulta;
        }
        catch (Exception ex) { throw new Exception(ex.Message); }
    }
}
