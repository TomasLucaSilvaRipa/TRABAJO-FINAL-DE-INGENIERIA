using System.Data;
using Microsoft.Data.SqlClient;
using TeamBalance.BE.Entidades;
using TeamBalance.DAL;

namespace TeamBalance.MPP;

public class MPPOpinionServicio
{
    private readonly Conexion _conexion;
    public MPPOpinionServicio(Conexion conexion) { _conexion = conexion; }

    public OpinionServicio Guardar(OpinionServicio opinion)
    {
        List<SqlParameter> parametros = new List<SqlParameter>() { new SqlParameter("@IdUsuario", opinion.IdUsuario), new SqlParameter("@IdAgencia", (object?)opinion.IdAgencia ?? DBNull.Value), new SqlParameter("@Calificacion", opinion.Calificacion), new SqlParameter("@Titulo", opinion.Titulo), new SqlParameter("@Comentario", opinion.Comentario) };
        DataTable tabla = _conexion.Leer("dbo.usp_OpinionServicio_Guardar", parametros);
        if (tabla.Rows.Count != 1) { throw new InvalidOperationException("No fue posible guardar la opinión."); }
        OpinionServicio resultado = CrearOpinion(tabla.Rows[0]);
        return resultado;
    }

    public List<OpinionServicio> ConsultarPublicas()
    {
        DataTable tabla = _conexion.Leer("dbo.usp_OpinionServicio_ConsultarPublicas");
        List<OpinionServicio> opiniones = new List<OpinionServicio>();
        foreach (DataRow fila in tabla.Rows) { 
            OpinionServicio opinion = CrearOpinion(fila); 
            opiniones.Add(opinion); 
        }
        return opiniones;
    }

    private static OpinionServicio CrearOpinion(DataRow fila)
    {
        OpinionServicio opinion = new OpinionServicio(Convert.ToInt32(fila["ID"]), Convert.ToInt32(fila["IdUsuario"]), fila["IdAgencia"] == DBNull.Value ? null : Convert.ToInt32(fila["IdAgencia"]), Convert.ToInt32(fila["Calificacion"]), Convert.ToString(fila["Titulo"]) ?? string.Empty, Convert.ToString(fila["Comentario"]) ?? string.Empty, Convert.ToDateTime(fila["FechaAlta"]), Convert.ToString(fila["NombreUsuario"]) ?? string.Empty, Convert.ToString(fila["NombreRol"]) ?? string.Empty);
        return opinion;
    }
}
