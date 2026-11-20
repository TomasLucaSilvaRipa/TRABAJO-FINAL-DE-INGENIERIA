using System.Data;
using System.Text.Json;
using Microsoft.Data.SqlClient;
using TeamBalance.BE.Entidades;

namespace TeamBalance.MPP;

public sealed partial class MPPSuscripcion
{
    public OperacionSuscripcionPendiente CrearDistribucion(Usuario usuario, CambioPlanSuscripcionRequest solicitud, List<PagoSuscripcion> pagos)
    {
        DataTable tabla = _conexion.Leer("dbo.usp_Suscripcion_CrearDistribucion", new List<SqlParameter>
        {
            new("@IdAgencia", usuario.IdAgencia), new("@IdSuscripcion", solicitud.IdSuscripcion),
            new("@IdPlanNuevo", solicitud.IdPlanComercial), new("@ReferenciaInterna", solicitud.ReferenciaInterna),
            new("@Credito", solicitud.CreditoAplicado), new("@Pagos", JsonSerializer.Serialize(pagos)),
        });
        return LeerDistribucion(tabla);
    }

    public OperacionSuscripcionPendiente? ConsultarDistribucion(Usuario usuario, string referencia)
    {
        DataTable tabla = _conexion.Leer("dbo.usp_Suscripcion_ConsultarDistribucion", new List<SqlParameter>
        {
            new("@IdAgencia", usuario.IdAgencia), new("@ReferenciaInterna", referencia),
        });
        return tabla.Rows.Count == 0 ? null : LeerDistribucion(tabla);
    }

    public OperacionSuscripcionPendiente ConfirmarParte(Usuario usuario, string referencia, string referenciaPago, string referenciaProveedor, string estado)
    {
        return LeerDistribucion(_conexion.Leer("dbo.usp_Suscripcion_ConfirmarParte", new List<SqlParameter>
        {
            new("@IdAgencia", usuario.IdAgencia), new("@ReferenciaInterna", referencia),
            new("@ReferenciaParte", referenciaPago), new("@ReferenciaProveedor", referenciaProveedor), new("@EstadoProveedor", estado),
        }));
    }

    public void GuardarCheckout(Usuario usuario, PagoSuscripcion pago, string url)
    {
        _conexion.Escribir("dbo.usp_Suscripcion_GuardarCheckout", new List<SqlParameter>
        {
            new("@IdAgencia", usuario.IdAgencia), new("@ReferenciaParte", pago.ReferenciaInterna),
            new("@Intento", pago.Intento), new("@UrlPago", url),
        });
    }

    public void DescartarSinCheckout(Usuario usuario, string referencia)
    {
        _conexion.Escribir("dbo.usp_Suscripcion_DescartarSinCheckout", new List<SqlParameter> { new("@IdAgencia", usuario.IdAgencia), new("@ReferenciaInterna", referencia) });
    }

    private static OperacionSuscripcionPendiente LeerDistribucion(DataTable tabla)
    {
        if (tabla.Rows.Count != 1) throw new KeyNotFoundException("No encontramos la actualización de esta agencia.");
        DataRow fila = tabla.Rows[0];
        return new OperacionSuscripcionPendiente
        {
            IdOperacion = Convert.ToInt32(fila["IdOperacion"]), IdSuscripcion = Convert.ToInt32(fila["IdSuscripcion"]),
            IdPlanNuevo = Convert.ToInt32(fila["IdPlanNuevo"]), IdAgencia = Convert.ToInt32(fila["IdAgencia"]),
            ReferenciaInterna = Convert.ToString(fila["ReferenciaInterna"])!, Importe = Convert.ToDecimal(fila["Importe"]),
            Moneda = Convert.ToString(fila["Moneda"])!, Estado = Convert.ToString(fila["Estado"])!,
            CreditoAplicado = Convert.ToDecimal(fila["CreditoAplicado"]),
            AplicadaAhora = Convert.ToBoolean(fila["AplicadaAhora"]),
            Pagos = JsonSerializer.Deserialize<List<PagoSuscripcion>>(Convert.ToString(fila["Pagos"])!) ?? new(),
        };
    }
}
