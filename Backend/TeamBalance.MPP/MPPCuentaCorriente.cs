using System.Data;
using Microsoft.Data.SqlClient;
using TeamBalance.BE.Entidades;
using TeamBalance.DAL;

namespace TeamBalance.MPP;

public sealed class MPPCuentaCorriente
{
    private readonly Conexion _conexion;

    public MPPCuentaCorriente(Conexion conexion)
    {
        _conexion = conexion;
    }

    public List<DocumentoComercial> Consultar(Usuario usuario)
    {
        List<SqlParameter> parametros = new List<SqlParameter>() { new SqlParameter("@IdAgencia", usuario.IdAgencia ?? throw new UnauthorizedAccessException("El usuario no pertenece a una agencia.")) };
        DataTable tabla = _conexion.Leer("dbo.usp_CuentaCorriente_Consultar", parametros);
        return CrearDocumentos(tabla);
    }

    public DocumentoComercial CancelarSuscripcion(DocumentoComercial documento, Usuario usuario)
    {
        List<SqlParameter> parametros = new List<SqlParameter>() { new SqlParameter("@IdAgencia", usuario.IdAgencia ?? throw new UnauthorizedAccessException("El usuario no pertenece a una agencia.")), new SqlParameter("@IdSuscripcion", documento.IdSuscripcion ?? throw new ArgumentException("Indicá la suscripción a cancelar.")), new SqlParameter("@Motivo", documento.Motivo ?? string.Empty) };
        DataTable tabla = _conexion.Leer("dbo.usp_CuentaCorriente_CancelarSuscripcion", parametros);
        if (tabla.Rows.Count != 1) { throw new InvalidOperationException("No fue posible emitir la nota de crédito de la cancelación."); }
        return CrearDocumento(tabla.Rows[0]);
    }

    public DocumentoComercial ReactivarConNotaCredito(DocumentoComercial documento, Usuario usuario)
    {
        List<SqlParameter> parametros = new List<SqlParameter>() { new SqlParameter("@IdAgencia", usuario.IdAgencia ?? throw new UnauthorizedAccessException("El usuario no pertenece a una agencia.")), new SqlParameter("@IdSuscripcion", documento.IdSuscripcion ?? throw new ArgumentException("Indicá la suscripción a regularizar.")), new SqlParameter("@IdPlanComercial", documento.IdPlanComercial ?? throw new ArgumentException("Seleccioná una modalidad para reactivar.")) };
        DataTable tabla = _conexion.Leer("dbo.usp_CuentaCorriente_ReactivarConNotaCredito", parametros);
        if (tabla.Rows.Count != 1) { throw new InvalidOperationException("No fue posible aplicar la nota de crédito."); }
        return CrearDocumento(tabla.Rows[0]);
    }

    private static List<DocumentoComercial> CrearDocumentos(DataTable tabla)
    {
        List<DocumentoComercial> documentos = new List<DocumentoComercial>();
        foreach (DataRow fila in tabla.Rows)
        {
            documentos.Add(CrearDocumento(fila));
        }
        return documentos;
    }

    private static DocumentoComercial CrearDocumento(DataRow fila)
    {
        return new DocumentoComercial(Convert.ToInt32(fila["ID"]), Convert.ToInt32(fila["IdAgencia"]), fila["IdSuscripcion"] == DBNull.Value ? null : Convert.ToInt32(fila["IdSuscripcion"]), fila["IdDocumentoOrigen"] == DBNull.Value ? null : Convert.ToInt32(fila["IdDocumentoOrigen"]), Convert.ToString(fila["Tipo"]) ?? string.Empty, Convert.ToString(fila["Numero"]) ?? string.Empty, Convert.ToString(fila["Concepto"]) ?? string.Empty, Convert.ToDecimal(fila["Importe"]), Convert.ToDecimal(fila["SaldoPendiente"]), Convert.ToString(fila["Moneda"]) ?? "ARS", Convert.ToString(fila["Estado"]) ?? string.Empty, Convert.ToDateTime(fila["FechaEmision"]), fila["FechaVencimiento"] == DBNull.Value ? null : Convert.ToDateTime(fila["FechaVencimiento"]), fila["Motivo"] == DBNull.Value ? null : Convert.ToString(fila["Motivo"]));
    }
}
