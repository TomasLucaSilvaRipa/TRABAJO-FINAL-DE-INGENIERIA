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
            new SqlParameter("@ResultadoCodigo", bitacora.ResultadoEnum.HasValue ? (object)(int)bitacora.ResultadoEnum.Value : DBNull.Value),
            new SqlParameter("@CriticidadCodigo", (int)bitacora.CriticidadEnum),
            new SqlParameter("@ModuloCodigo", bitacora.ModuloEnum.HasValue ? (object)(int)bitacora.ModuloEnum.Value : DBNull.Value),
            new SqlParameter("@FechaHora", bitacora.FechaHora),
            new SqlParameter("@DireccionIP", (object?)bitacora.DireccionIP ?? DBNull.Value),
        };

        return _conexion.Escribir("dbo.usp_Bitacora_Registrar", parametros);
    }

    public List<Bitacora> LeerBitacora(FiltroBitacora filtro)
    {
        List<SqlParameter> parametros = new List<SqlParameter>()
        {
            new SqlParameter("@IdAgencia", (object?)filtro.IdAgencia ?? DBNull.Value),
        };

        DataTable tabla = _conexion.Leer("dbo.usp_Bitacora_Consultar", parametros);

        return CrearLista(tabla);
    }

    public List<Bitacora> Filtrar(FiltroBitacora filtro)
    {
        List<SqlParameter> parametros = new List<SqlParameter>()
        {
            new SqlParameter("@IdAgencia", (object?)filtro.IdAgencia ?? DBNull.Value),
            new SqlParameter("@Desde", (object?)filtro.Desde ?? DBNull.Value),
            new SqlParameter("@Hasta", (object?)filtro.Hasta ?? DBNull.Value),
            new SqlParameter("@IdUsuario", (object?)filtro.IdUsuario ?? DBNull.Value),
            new SqlParameter("@Entidad", (object?)filtro.Entidad ?? DBNull.Value),
            new SqlParameter("@Accion", (object?)filtro.Accion ?? DBNull.Value),
            new SqlParameter("@ResultadoCodigo", filtro.Resultado.HasValue ? (object)(int)filtro.Resultado.Value : DBNull.Value),
            new SqlParameter("@CriticidadCodigo", filtro.Criticidad.HasValue ? (object)(int)filtro.Criticidad.Value : DBNull.Value),
            new SqlParameter("@ModuloCodigo", filtro.Modulo.HasValue ? (object)(int)filtro.Modulo.Value : DBNull.Value),
        };

        DataTable tabla = _conexion.Leer("dbo.usp_Bitacora_Consultar", parametros);

        return CrearLista(tabla);
    }

    private static List<Bitacora> CrearLista(DataTable tabla)
    {
        List<Bitacora> lista = new List<Bitacora>();

        foreach (DataRow fila in tabla.Rows)
        {
            Bitacora bitacora = new Bitacora(Convert.ToInt32(fila["ID"]), fila["IdUsuario"] == DBNull.Value ? null : Convert.ToInt32(fila["IdUsuario"]), fila["IdAgencia"] == DBNull.Value ? null : Convert.ToInt32(fila["IdAgencia"]), Convert.ToString(fila["Entidad"]), fila["IdEntidad"] == DBNull.Value ? null : Convert.ToInt32(fila["IdEntidad"]), Convert.ToString(fila["Accion"]) ?? string.Empty, Convert.ToString(fila["Mensaje"]) ?? string.Empty, null, null, Convert.ToString(fila["Modulo"]), Convert.ToDateTime(fila["FechaHora"]), Convert.ToString(fila["DireccionIP"]));
            bitacora.ResultadoEnum = fila["ResultadoCodigo"] == DBNull.Value ? null : (ResultadoBitacora)Convert.ToInt32(fila["ResultadoCodigo"]);
            bitacora.CriticidadEnum = fila["CriticidadCodigo"] == DBNull.Value ? CriticidadBitacora.Informacion : (CriticidadBitacora)Convert.ToInt32(fila["CriticidadCodigo"]);
            if (fila.Table.Columns.Contains("ModuloCodigo") && fila["ModuloCodigo"] != DBNull.Value) { bitacora.ModuloEnum = (ModuloBitacora)Convert.ToInt32(fila["ModuloCodigo"]); }

            lista.Add(bitacora);
        }

        return lista;
    }
}
