using System.Data;
using Microsoft.Data.SqlClient;
using TeamBalance.BE.Entidades;

namespace TeamBalance.MPP;

public sealed partial class MPPContratacion
{
    public (int IdPlan, List<PagoSuscripcion> Pagos) ConsultarPagosCombinados(ContratacionServicio contratacion)
    {
        DataTable tabla = _conexion.Leer("dbo.usp_Contratacion_ConsultarPartes",
            new() { new("@ReferenciaContratacion", contratacion.ReferenciaContratacion) });
        if (tabla.Rows.Count == 0) throw new KeyNotFoundException("No encontramos esa contratación con pago combinado.");

        List<PagoSuscripcion> pagos = new();
        foreach (DataRow fila in tabla.Rows)
        {
            pagos.Add(new PagoSuscripcion
            {
                ReferenciaInterna = (string)fila["ReferenciaInterna"],
                Proveedor = (string)fila["Proveedor"],
                MedioPago = (string)fila["MedioPago"],
                Importe = (decimal)fila["Importe"],
                Moneda = (string)fila["Moneda"],
                Estado = (string)fila["Estado"],
                UrlPago = Convert.ToString(fila["UrlPago"]) ?? string.Empty,
                ReferenciaProveedor = Convert.ToString(fila["ReferenciaProveedor"]) ?? string.Empty,
                Intento = (int)fila["Intento"]
            });
        }
        return ((int)tabla.Rows[0]["IdPlanComercial"], pagos);
    }

    public void GuardarCheckoutParte(ContratacionServicio contratacion, PagoSuscripcion pago, string url, string sesion)
    {
        List<SqlParameter> parametros = ParametrosParte(contratacion, pago);
        parametros.Add(new("@UrlPago", url));
        parametros.Add(new("@ReferenciaProveedor", sesion));
        _conexion.Escribir("dbo.usp_Contratacion_GuardarCheckoutParte", parametros);
    }

    public bool GuardarResultadoParte(ContratacionServicio contratacion, PagoSuscripcion pago, string referenciaProveedor, string estado)
    {
        List<SqlParameter> parametros = ParametrosParte(contratacion, pago);
        parametros.Add(new("@ReferenciaProveedor", referenciaProveedor));
        parametros.Add(new("@EstadoProveedor", estado));
        DataTable resultado = _conexion.Leer("dbo.usp_Contratacion_ConfirmarParte", parametros);
        return resultado.Rows.Count == 1 && (bool)resultado.Rows[0]["AprobadaAhora"];
    }

    private static List<SqlParameter> ParametrosParte(ContratacionServicio contratacion, PagoSuscripcion pago) =>
        new()
        {
            new("@ReferenciaContratacion", contratacion.ReferenciaContratacion),
            new("@ReferenciaParte", pago.ReferenciaInterna),
            new("@Intento", pago.Intento)
        };
}
