using System.Data;
using Microsoft.Data.SqlClient;
using TeamBalance.BE.Entidades;
using TeamBalance.DAL;

namespace TeamBalance.MPP;

public class MPPBitacora
{
    private readonly Conexion _conexion;

    public MPPBitacora(Conexion conexion)
    {
        _conexion = conexion;
    }

    public bool Add(Bitacora bitacora)
    {
        List<SqlParameter> parametros = new List<SqlParameter>()
        {
            new SqlParameter("@IdUsuario", (object?)bitacora.IdUsuario ?? DBNull.Value),
            new SqlParameter("@IdAgencia", (object?)bitacora.IdAgencia ?? DBNull.Value),
            new SqlParameter("@Entidad", (object?)bitacora.Entidad ?? DBNull.Value),
            new SqlParameter("@IdEntidad", (object?)bitacora.IdEntidad ?? DBNull.Value),
            new SqlParameter("@Accion", bitacora.Accion),
            new SqlParameter("@Mensaje", bitacora.Mensaje),
            new SqlParameter("@Resultado", (object?)bitacora.Resultado ?? DBNull.Value),
            new SqlParameter("@Criticidad", (object?)bitacora.Criticidad ?? DBNull.Value),
            new SqlParameter("@Modulo", (object?)bitacora.Modulo ?? DBNull.Value),
            new SqlParameter("@FechaHora", bitacora.FechaHora),
            new SqlParameter("@DireccionIP", (object?)bitacora.DireccionIP ?? DBNull.Value),
        };

        return _conexion.Escribir("dbo.usp_Bitacora_Registrar", parametros);
    }

    public List<Bitacora> LeerBitacora(int? idAgencia = null)
    {
        List<SqlParameter> parametros = new List<SqlParameter>()
        {
            new SqlParameter("@IdAgencia", (object?)idAgencia ?? DBNull.Value),
        };

        DataTable tabla = _conexion.Leer("dbo.usp_Bitacora_Consultar", parametros);

        return CrearLista(tabla);
    }

    public List<Bitacora> Filtrar(int? idAgencia, DateTime? desde = null, DateTime? hasta = null, int? idUsuario = null, string? entidad = null, string? accion = null, string? resultado = null, string? criticidad = null, string? modulo = null)
    {
        List<SqlParameter> parametros = new List<SqlParameter>()
        {
            new SqlParameter("@IdAgencia", (object?)idAgencia ?? DBNull.Value),
            new SqlParameter("@Desde", (object?)desde ?? DBNull.Value),
            new SqlParameter("@Hasta", (object?)hasta ?? DBNull.Value),
            new SqlParameter("@IdUsuario", (object?)idUsuario ?? DBNull.Value),
            new SqlParameter("@Entidad", (object?)entidad ?? DBNull.Value),
            new SqlParameter("@Accion", (object?)accion ?? DBNull.Value),
            new SqlParameter("@Resultado", (object?)resultado ?? DBNull.Value),
            new SqlParameter("@Criticidad", (object?)criticidad ?? DBNull.Value),
            new SqlParameter("@Modulo", (object?)modulo ?? DBNull.Value),
        };

        DataTable tabla = _conexion.Leer("dbo.usp_Bitacora_Consultar", parametros);

        return CrearLista(tabla);
    }

    private static List<Bitacora> CrearLista(DataTable tabla)
    {
        List<Bitacora> lista = new List<Bitacora>();

        foreach (DataRow fila in tabla.Rows)
        {
            Bitacora bitacora = new Bitacora(Convert.ToInt32(fila["ID"]), fila["IdUsuario"] == DBNull.Value ? null : Convert.ToInt32(fila["IdUsuario"]), fila["IdAgencia"] == DBNull.Value ? null : Convert.ToInt32(fila["IdAgencia"]), Convert.ToString(fila["Entidad"]), fila["IdEntidad"] == DBNull.Value ? null : Convert.ToInt32(fila["IdEntidad"]), Convert.ToString(fila["Accion"]) ?? string.Empty, Convert.ToString(fila["Mensaje"]) ?? string.Empty, Convert.ToString(fila["Resultado"]), Convert.ToString(fila["Criticidad"]), Convert.ToString(fila["Modulo"]), Convert.ToDateTime(fila["FechaHora"]), Convert.ToString(fila["DireccionIP"]));

            lista.Add(bitacora);
        }

        return lista;
    }
}
