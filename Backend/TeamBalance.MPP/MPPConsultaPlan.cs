using System.Data;
using Microsoft.Data.SqlClient;
using TeamBalance.BE.Entidades;
using TeamBalance.DAL;

namespace TeamBalance.MPP;

public class MPPConsultaPlan
{
    private readonly Conexion _conexion;
    public MPPConsultaPlan(Conexion conexion) { _conexion = conexion; }

    public List<ConsultaPlan> Consultar(PlanComercial planComercial)
    {
        List<SqlParameter> parametros = new List<SqlParameter>() { new SqlParameter("@IdPlanComercial", planComercial.ID) };
        DataTable tabla = _conexion.Leer("dbo.usp_ConsultaPlan_ConsultarPublicas", parametros);
        List<ConsultaPlan> consultas = new List<ConsultaPlan>();
        foreach (DataRow fila in tabla.Rows) { ConsultaPlan consulta = CrearConsulta(fila); consultas.Add(consulta); }
        return consultas;
    }

    public ConsultaPlan Registrar(ConsultaPlan consulta)
    {
        List<SqlParameter> parametros = new List<SqlParameter>() { new SqlParameter("@IdPlanComercial", consulta.IdPlanComercial), new SqlParameter("@Nombre", consulta.Nombre), new SqlParameter("@Email", consulta.Email), new SqlParameter("@Consulta", consulta.Consulta) };
        DataTable tabla = _conexion.Leer("dbo.usp_ConsultaPlan_Registrar", parametros);
        if (tabla.Rows.Count != 1) { throw new InvalidOperationException("No fue posible registrar la consulta."); }
        ConsultaPlan resultado = CrearConsulta(tabla.Rows[0]);
        return resultado;
    }

    private static ConsultaPlan CrearConsulta(DataRow fila) { 
        ConsultaPlan consulta = new ConsultaPlan(Convert.ToInt32(fila["ID"]), Convert.ToInt32(fila["IdPlanComercial"]), Convert.ToString(fila["Nombre"]) ?? string.Empty, Convert.ToString(fila["Email"]) ?? string.Empty, Convert.ToString(fila["Consulta"]) ?? string.Empty, Convert.ToDateTime(fila["FechaAlta"]), Convert.ToBoolean(fila["Activo"])); 
        return consulta; 
    }
}
