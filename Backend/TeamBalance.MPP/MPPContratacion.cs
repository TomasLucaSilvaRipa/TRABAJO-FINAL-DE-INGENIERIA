using System.Data;
using Microsoft.Data.SqlClient;
using TeamBalance.BE.Entidades;
using TeamBalance.DAL;

namespace TeamBalance.MPP;

public sealed class MPPContratacion
{
    private readonly Conexion _conexion;

    public MPPContratacion(Conexion conexion)
    {
        _conexion = conexion;
    }

    public ContratacionPendiente CrearPendiente(
        ContratacionRequest request,
        string referenciaContratacion,
        string referenciaOperacion)
    {
        var parametros = new List<SqlParameter>
        {
            new("@ReferenciaContratacion", referenciaContratacion),
            new("@ReferenciaOperacion", referenciaOperacion),
            new("@NombreComercialAgencia", request.NombreComercialAgencia),
            new("@RazonSocial", request.RazonSocial),
            new("@CUIT", request.CUIT),
            new("@CondicionFiscal", request.CondicionFiscal),
            new("@EmailFacturacion", request.EmailFacturacion),
            new("@TelefonoContacto", request.TelefonoContacto),
            new("@NombreResponsable", request.NombreResponsable),
            new("@ApellidoResponsable", request.ApellidoResponsable),
            new("@EmailLaboralResponsable", request.EmailLaboralResponsable),
            new("@CargoResponsable", request.CargoResponsable),
            new("@ProveedorPagoSeleccionado", request.ProveedorPagoSeleccionado),
            new("@Periodicidad", request.Periodicidad),
        };

        DataTable resultado = _conexion.Leer("dbo.usp_Contratacion_CrearPendiente", parametros);

        if (resultado.Rows.Count != 1)
        {
            throw new InvalidOperationException("No fue posible iniciar la contratación.");
        }

        DataRow fila = resultado.Rows[0];

        return new ContratacionPendiente(
            Convert.ToInt32(fila["IdContratacion"]),
            Convert.ToInt32(fila["IdPlanComercial"]),
            Convert.ToString(fila["ReferenciaContratacion"]) ?? string.Empty,
            Convert.ToString(fila["ReferenciaOperacion"]) ?? string.Empty,
            Convert.ToDecimal(fila["Importe"]),
            Convert.ToString(fila["Moneda"]) ?? string.Empty);
    }

    public EstadoContratacionPersistido ConsultarEstado(string referenciaContratacion)
    {
        var parametros = new List<SqlParameter>
        {
            new("@ReferenciaContratacion", referenciaContratacion),
        };

        DataTable resultado = _conexion.Leer("dbo.usp_Contratacion_ConsultarEstado", parametros);

        if (resultado.Rows.Count != 1){ throw new KeyNotFoundException("No existe una contratación con la referencia indicada."); }

        DataRow fila = resultado.Rows[0];

        return new EstadoContratacionPersistido(
            Convert.ToString(fila["ReferenciaContratacion"]) ?? string.Empty,
            Convert.ToString(fila["EstadoContratacion"]) ?? string.Empty,
            Convert.ToDecimal(fila["Importe"]),
            Convert.ToString(fila["Moneda"]) ?? string.Empty);
    }

    public EstadoContratacionPersistido ActualizarResultadoPago(
        string referenciaContratacion,
        string referenciaProveedor,
        string estadoProveedor,
        string mensajeRespuesta)
    {
        var parametros = new List<SqlParameter>
        {
            new("@ReferenciaContratacion", referenciaContratacion),
            new("@ReferenciaProveedor", referenciaProveedor),
            new("@EstadoProveedor", estadoProveedor),
            new("@MensajeRespuesta", mensajeRespuesta),
        };

        DataTable resultado = _conexion.Leer("dbo.usp_Contratacion_ActualizarResultadoPago", parametros);

        if (resultado.Rows.Count != 1)
        {
            throw new InvalidOperationException("No fue posible actualizar el estado del pago.");
        }

        DataRow fila = resultado.Rows[0];

        return new EstadoContratacionPersistido(
            Convert.ToString(fila["ReferenciaContratacion"]) ?? string.Empty,
            Convert.ToString(fila["EstadoContratacion"]) ?? string.Empty,
            Convert.ToDecimal(fila["Importe"]),
            Convert.ToString(fila["Moneda"]) ?? string.Empty);
    }
}

public sealed record EstadoContratacionPersistido(
    string ReferenciaContratacion,
    string EstadoContratacion,
    decimal Importe,
    string Moneda);
