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
            new("@IdUsuario", (object?)bitacora.IdUsuario ?? DBNull.Value),
            new("@IdAgencia", (object?)bitacora.IdAgencia ?? DBNull.Value),
            new("@Entidad", (object?)bitacora.Entidad ?? DBNull.Value),
            new("@IdEntidad", (object?)bitacora.IdEntidad ?? DBNull.Value),
            new("@Accion", bitacora.Accion),
            new("@Mensaje", bitacora.Mensaje),
            new("@Resultado", (object?)bitacora.Resultado ?? DBNull.Value),
            new("@Criticidad", (object?)bitacora.Criticidad ?? DBNull.Value),
            new("@Modulo", (object?)bitacora.Modulo ?? DBNull.Value),
            new("@FechaHora", bitacora.FechaHora),
            new("@DireccionIP", (object?)bitacora.DireccionIP ?? DBNull.Value),
        };

        return _conexion.Escribir("dbo.usp_Bitacora_Registrar", parametros);
    }

    public List<Bitacora> LeerBitacora(int? idAgencia = null)
    {
        List<SqlParameter> parametros = new List<SqlParameter>()
        {
            new("@IdAgencia", (object?)idAgencia ?? DBNull.Value),
        };

        DataTable tabla = _conexion.Leer("dbo.usp_Bitacora_Consultar", parametros);

        return CrearLista(tabla);
    }

    public List<Bitacora> Filtrar(
        int? idAgencia,
        DateTime? desde = null,
        DateTime? hasta = null,
        int? idUsuario = null,
        string? entidad = null,
        string? accion = null,
        string? resultado = null,
        string? criticidad = null,
        string? modulo = null)
    {
        List<SqlParameter> parametros = new List<SqlParameter>()
        {
            new("@IdAgencia", (object?)idAgencia ?? DBNull.Value),
            new("@Desde", (object?)desde ?? DBNull.Value),
            new("@Hasta", (object?)hasta ?? DBNull.Value),
            new("@IdUsuario", (object?)idUsuario ?? DBNull.Value),
            new("@Entidad", (object?)entidad ?? DBNull.Value),
            new("@Accion", (object?)accion ?? DBNull.Value),
            new("@Resultado", (object?)resultado ?? DBNull.Value),
            new("@Criticidad", (object?)criticidad ?? DBNull.Value),
            new("@Modulo", (object?)modulo ?? DBNull.Value),
        };

        DataTable tabla = _conexion.Leer("dbo.usp_Bitacora_Consultar", parametros);

        return CrearLista(tabla);
    }

    private static List<Bitacora> CrearLista(DataTable tabla)
    {
        List<Bitacora> lista = new List<Bitacora>();

        foreach (DataRow fila in tabla.Rows)
        {
            Bitacora bitacora = new Bitacora()
            {
                ID = Convert.ToInt32(fila["ID"]),
                IdUsuario = fila["IdUsuario"] == DBNull.Value ? null : Convert.ToInt32(fila["IdUsuario"]),
                IdAgencia = fila["IdAgencia"] == DBNull.Value ? null : Convert.ToInt32(fila["IdAgencia"]),
                Entidad = Convert.ToString(fila["Entidad"]),
                IdEntidad = fila["IdEntidad"] == DBNull.Value ? null : Convert.ToInt32(fila["IdEntidad"]),
                Accion = Convert.ToString(fila["Accion"]) ?? string.Empty,
                Mensaje = Convert.ToString(fila["Mensaje"]) ?? string.Empty,
                Resultado = Convert.ToString(fila["Resultado"]),
                Criticidad = Convert.ToString(fila["Criticidad"]),
                Modulo = Convert.ToString(fila["Modulo"]),
                FechaHora = Convert.ToDateTime(fila["FechaHora"]),
                DireccionIP = Convert.ToString(fila["DireccionIP"]),
            };

            lista.Add(bitacora);
        }

        return lista;
    }
}
