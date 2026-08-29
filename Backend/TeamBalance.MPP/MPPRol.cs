using System.Data;
using Microsoft.Data.SqlClient;
using TeamBalance.BE.Entidades;
using TeamBalance.DAL;

namespace TeamBalance.MPP;

public class MPPRol
{
    private readonly Conexion _conexion;

    public MPPRol(Conexion conexion)
    {
        _conexion = conexion;
    }

    public Rol ConsultarRolPorNombre(string nombre)
    {
        List<SqlParameter> parametros = new List<SqlParameter>()
        {
            new("@Nombre", nombre),
        };

        DataTable resultado = _conexion.Leer("dbo.usp_Rol_ConsultarPorNombre", parametros);

        if (resultado.Rows.Count >= 1)
        {
            DataRow fila = resultado.Rows[0];

            return new Rol()
            {
                ID = Convert.ToInt32(fila["ID"]),
                Nombre = Convert.ToString(fila["Nombre"]) ?? string.Empty,
                Descripcion = Convert.ToString(fila["Descripcion"]),
                EsRolBase = Convert.ToBoolean(fila["EsRolBase"]),
                Activo = Convert.ToBoolean(fila["Activo"]),
                FechaBaja = fila["FechaBaja"] == DBNull.Value ? null : Convert.ToDateTime(fila["FechaBaja"]),
            };
        }
        else{ throw new KeyNotFoundException("No existe un rol activo con el nombre indicado."); }
    }
}
