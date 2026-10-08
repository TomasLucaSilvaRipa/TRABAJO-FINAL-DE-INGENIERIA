namespace TeamBalance.BE.Entidades;

// DTOs del CU05-003. No contienen ni reciben datos sensibles de pago.
public sealed class CambioPlanSuscripcionRequest
{
    public int IdPlanComercial { get; set; }
}

public sealed class CancelacionRenovacionRequest
{
    public string? Motivo { get; set; }
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
    public int IdOperacion { get; set; }
    public int IdSuscripcion { get; set; }
    public int IdPlanNuevo { get; set; }
    public string ReferenciaInterna { get; set; } = string.Empty;
    public decimal Importe { get; set; }
    public string Moneda { get; set; } = string.Empty;
    public string Proveedor { get; set; } = string.Empty;
}

public sealed class SincronizacionVencimientosResultado
{
    public int IdSuscripcion { get; set; }
    public int IdAgencia { get; set; }
    public string Estado { get; set; } = string.Empty;
}

public sealed class ConfiguracionRenovacionAutomatica
{
    public string PublicKey { get; set; } = string.Empty;
    public decimal ImportePrueba { get; set; }
    public string MonedaPrueba { get; set; } = "ARS";
    public string NombrePlan { get; set; } = string.Empty;
}

public sealed class AutorizarRenovacionAutomaticaRequest
{
    public string CardToken { get; set; } = string.Empty;
    public string PayerEmail { get; set; } = string.Empty;
}

public sealed class RenovacionProveedorPendiente
{
    public int IdSuscripcion { get; set; }
    public int IdAgencia { get; set; }
    public int IdPlanComercial { get; set; }
    public string ReferenciaRenovacionProveedor { get; set; } = string.Empty;
}
