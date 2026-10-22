using System.Data;
using Microsoft.Data.SqlClient;
using TeamBalance.BE.Entidades;
using TeamBalance.DAL;

namespace TeamBalance.MPP;

public sealed class MPPIngresosPlataforma
{
    private readonly Conexion _conexion;

    public MPPIngresosPlataforma(Conexion conexion) => _conexion = conexion;

    public List<CobroPlataformaConfirmado> ConsultarCobrosConfirmados()
    {
        DataTable tabla = _conexion.Leer("dbo.usp_IngresosPlataforma_ConsultarCobrosConfirmados");
        return tabla.Rows.Cast<DataRow>().Select(fila => new CobroPlataformaConfirmado
        {
            IdAgencia = Convert.ToInt32(fila["IdAgencia"]),
            Agencia = Convert.ToString(fila["Agencia"]) ?? string.Empty,
            Importe = Convert.ToDecimal(fila["Importe"]),
            Moneda = Convert.ToString(fila["Moneda"]) ?? string.Empty,
            Proveedor = fila["Proveedor"] == DBNull.Value ? null : Convert.ToString(fila["Proveedor"]),
            FechaCobro = Convert.ToDateTime(fila["FechaCobro"])
        }).ToList();
    }
}
