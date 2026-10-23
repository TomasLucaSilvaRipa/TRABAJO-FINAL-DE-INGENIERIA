namespace TeamBalance.BE.Entidades;

// DTOs del CU05-003. No contienen ni reciben datos sensibles de pago.
public sealed class CambioPlanSuscripcionRequest
{
    public int IdSuscripcion { get; set; }
    public int IdPlanComercial { get; set; }
    public string ReferenciaInterna { get; set; } = string.Empty;
    public string Proveedor { get; set; } = string.Empty;
    public decimal ImportePago { get; set; }
    public string MonedaPago { get; set; } = string.Empty;
    public decimal CreditoAplicado { get; set; }
}

public sealed class CancelacionRenovacionRequest
{
    public string? Motivo { get; set; }
}

public sealed class VerificarPagoSuscripcionRequest
{
    public string ReferenciaOperacion { get; set; } = string.Empty;
    public string PaymentId { get; set; } = string.Empty;
}

public sealed class InicioActualizacionSuscripcion
{
    public InicioActualizacionSuscripcion(string urlPago, string referenciaOperacion, decimal importe, string moneda, DateTime fechaAplicacion)
    {
        UrlPago = urlPago;
        ReferenciaOperacion = referenciaOperacion;
        Importe = importe;
        Moneda = moneda;
        FechaAplicacion = fechaAplicacion;
    }

    public string UrlPago { get; set; }
    public string ReferenciaOperacion { get; set; }
    public decimal Importe { get; set; }
    public string Moneda { get; set; }
    public DateTime FechaAplicacion { get; set; }
}

public sealed class ResultadoGestionSuscripcion
{
    public ResultadoGestionSuscripcion(string mensaje, string estado, Suscripcion? suscripcion = null)
    {
        Mensaje = mensaje;
        Estado = estado;
        Suscripcion = suscripcion;
    }

    public string Mensaje { get; set; }
    public string Estado { get; set; }
    public Suscripcion? Suscripcion { get; set; }
}

public sealed class OperacionSuscripcionHistorial
{
    public OperacionSuscripcionHistorial(int id, string tipoOperacion, string? modalidad, decimal? importe, string? moneda, string? proveedor, string? referencia, string estado, string? detalle, DateTime fecha)
    {
        ID = id;
        TipoOperacion = tipoOperacion;
        Modalidad = modalidad;
        Importe = importe;
        Moneda = moneda;
        Proveedor = proveedor;
        Referencia = referencia;
        Estado = estado;
        Detalle = detalle;
        Fecha = fecha;
    }

    public int ID { get; set; }
    public string TipoOperacion { get; set; } = string.Empty;
    public string? Modalidad { get; set; }
    public decimal? Importe { get; set; }
    public string? Moneda { get; set; }
    public string? Proveedor { get; set; }
    public string? Referencia { get; set; }
    public string Estado { get; set; } = string.Empty;
    public string? Detalle { get; set; }
    public DateTime Fecha { get; set; }
}

public sealed class OperacionSuscripcionPendiente
{
    public OperacionSuscripcionPendiente()
    {
    }

    public OperacionSuscripcionPendiente(string referenciaInterna)
    {
        ReferenciaInterna = referenciaInterna;
    }

    public OperacionSuscripcionPendiente(int idOperacion, int idSuscripcion, int idPlanNuevo, string referenciaInterna, decimal importe, string moneda, string proveedor, string tipoOperacion = "")
    {
        IdOperacion = idOperacion;
        IdSuscripcion = idSuscripcion;
        IdPlanNuevo = idPlanNuevo;
        ReferenciaInterna = referenciaInterna;
        Importe = importe;
        Moneda = moneda;
        Proveedor = proveedor;
        TipoOperacion = tipoOperacion;
    }

    public int IdOperacion { get; set; }
    public int IdSuscripcion { get; set; }
    public int IdPlanNuevo { get; set; }
    public string ReferenciaInterna { get; set; } = string.Empty;
    public decimal Importe { get; set; }
    public string Moneda { get; set; } = string.Empty;
    public string Proveedor { get; set; } = string.Empty;
    public string TipoOperacion { get; set; } = string.Empty;
    public string ReferenciaProveedor { get; set; } = string.Empty;
    public string EstadoProveedor { get; set; } = string.Empty;
    public string DetalleProveedor { get; set; } = string.Empty;
}

public sealed class SincronizacionVencimientosResultado
{
    public SincronizacionVencimientosResultado(int idSuscripcion, int idAgencia, string estado)
    {
        IdSuscripcion = idSuscripcion;
        IdAgencia = idAgencia;
        Estado = estado;
    }

    public int IdSuscripcion { get; set; }
    public int IdAgencia { get; set; }
    public string Estado { get; set; } = string.Empty;
}

public sealed class ConfiguracionRenovacionAutomatica
{
    public ConfiguracionRenovacionAutomatica()
    {
    }

    public ConfiguracionRenovacionAutomatica(string publicKey, decimal importePrueba, string monedaPrueba, string nombrePlan)
    {
        PublicKey = publicKey;
        ImportePrueba = importePrueba;
        MonedaPrueba = monedaPrueba;
        NombrePlan = nombrePlan;
    }

    public string PublicKey { get; set; } = string.Empty;
    public decimal ImportePrueba { get; set; }
    public string MonedaPrueba { get; set; } = "ARS";
    public string NombrePlan { get; set; } = string.Empty;
}

public sealed class AutorizarRenovacionAutomaticaRequest
{
    public string PayerEmail { get; set; } = string.Empty;
}

public sealed class SolicitudRenovacionMercadoPago
{
    public SolicitudRenovacionMercadoPago(string nombrePlan, string referenciaExterna, string payerEmail)
    {
        NombrePlan = nombrePlan;
        ReferenciaExterna = referenciaExterna;
        PayerEmail = payerEmail;
    }

    public string NombrePlan { get; set; }
    public string ReferenciaExterna { get; set; }
    public string PayerEmail { get; set; }
}

public sealed class RenovacionProveedorPendiente
{
    public RenovacionProveedorPendiente(int idSuscripcion, int idAgencia, int idPlanComercial, string referenciaRenovacionProveedor, string proveedor)
    {
        IdSuscripcion = idSuscripcion;
        IdAgencia = idAgencia;
        IdPlanComercial = idPlanComercial;
        ReferenciaRenovacionProveedor = referenciaRenovacionProveedor;
        Proveedor = proveedor;
    }

    public int IdSuscripcion { get; set; }
    public int IdAgencia { get; set; }
    public int IdPlanComercial { get; set; }
    public string ReferenciaRenovacionProveedor { get; set; } = string.Empty;
    public string Proveedor { get; set; } = string.Empty;
    public string ReferenciaPago { get; set; } = string.Empty;
    public decimal Importe { get; set; }
    public string Moneda { get; set; } = string.Empty;
    public string Detalle { get; set; } = string.Empty;
    public string EstadoProveedor { get; set; } = string.Empty;
}
