using TeamBalance.BE.Entidades;
using TeamBalance.MPP;
using TeamBalance.Services;

namespace TeamBalance.BLL;

public sealed class BLLSuscripcion
{
    private readonly MPPSuscripcion _suscripcionMPP;
    private readonly MPPPlanComercial _planMPP;
    private readonly BLLRol _rolBLL;
    private readonly BLLBitacora _bitacoraBLL;
    private readonly MercadoPagoService _mercadoPagoService;

    public BLLSuscripcion(MPPSuscripcion suscripcionMPP, MPPPlanComercial planMPP, BLLRol rolBLL, BLLBitacora bitacoraBLL, MercadoPagoService mercadoPagoService)
    {
        _suscripcionMPP = suscripcionMPP;
        _planMPP = planMPP;
        _rolBLL = rolBLL;
        _bitacoraBLL = bitacoraBLL;
        _mercadoPagoService = mercadoPagoService;
    }

    public Suscripcion ConsultarSuscripcion(Usuario solicitante)
    {
        ValidarDueño(solicitante);
        return _suscripcionMPP.ConsultarActual(solicitante) ?? throw new KeyNotFoundException("La agencia no tiene una suscripción registrada.");
    }

    public List<PlanComercial> ConsultarPlanesDisponibles(Usuario solicitante)
    {
        ValidarDueño(solicitante);
        return _planMPP.ConsultarPlanes(true);
    }

    public List<OperacionSuscripcionHistorial> ConsultarHistorial(Usuario solicitante)
    {
        ValidarDueño(solicitante);
        return _suscripcionMPP.ConsultarHistorial(solicitante);
    }

    public async Task<InicioActualizacionSuscripcion> SolicitarActualizacion(Usuario solicitante, CambioPlanSuscripcionRequest solicitud)
    {
        ValidarDueño(solicitante);
        if (solicitud.IdPlanComercial <= 0) throw new ArgumentException("Seleccioná una modalidad válida.");

        Suscripcion suscripcion = _suscripcionMPP.ConsultarActual(solicitante) ?? throw new KeyNotFoundException("La agencia no tiene una suscripción registrada.");
        if (suscripcion.Activo && suscripcion.IdPlanComercial == solicitud.IdPlanComercial) throw new ArgumentException("La modalidad seleccionada ya es la contratada.");

        PlanComercial plan = _planMPP.ConsultarPlan(new PlanComercial(solicitud.IdPlanComercial));
        if (!plan.Activo || (plan.FechaVigenciaHasta.HasValue && plan.FechaVigenciaHasta.Value < DateTime.Now)) throw new ArgumentException("La modalidad seleccionada no está disponible.");

        solicitud.IdSuscripcion = suscripcion.ID;
        solicitud.ReferenciaInterna = $"sus-{Guid.NewGuid():N}";
        solicitud.Proveedor = "MercadoPago";
        OperacionSuscripcionPendiente operacion = _suscripcionMPP.CrearSolicitudCambioPlan(solicitud);

        string urlPago = await _mercadoPagoService.CrearPago(new MercadoPagoPreferenceRequest($"TeamBalance · cambio a {plan.Nombre}", operacion.Importe, operacion.Moneda, operacion.ReferenciaInterna, plan.Nombre));

        RegistrarBitacora(solicitante, suscripcion, "SolicitarActualizacionSuscripcion", $"Se solicitó la actualización de suscripción a la modalidad {plan.Nombre}.");
        return new InicioActualizacionSuscripcion(urlPago, solicitud.ReferenciaInterna, operacion.Importe, operacion.Moneda, DateTime.Now);
    }

    public async Task<ResultadoGestionSuscripcion> VerificarActualizacionPago(VerificarPagoSuscripcionRequest solicitud)
    {
        if (string.IsNullOrWhiteSpace(solicitud.ReferenciaOperacion) || string.IsNullOrWhiteSpace(solicitud.PaymentId)) throw new ArgumentException("No recibimos la referencia de la operación de actualización.");

        OperacionSuscripcionPendiente operacion = new OperacionSuscripcionPendiente();
        operacion.ReferenciaInterna = solicitud.ReferenciaOperacion;
        operacion = _suscripcionMPP.ConsultarOperacionPendiente(operacion);
        MercadoPagoPayment pago = await _mercadoPagoService.ConsultarPago(solicitud.PaymentId);
        if (!string.Equals(pago.ExternalReference, operacion.ReferenciaInterna, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("La referencia devuelta por Mercado Pago no corresponde a la actualización solicitada.");
        }
        bool importeConvertidoPorProveedor = pago.TransactionAmount != operacion.Importe ||
            !string.Equals(pago.CurrencyId, operacion.Moneda, StringComparison.OrdinalIgnoreCase);
        PlanComercial plan = _planMPP.ConsultarPlan(new PlanComercial(operacion.IdPlanNuevo));
        if (importeConvertidoPorProveedor && !_mercadoPagoService.EsCobroPruebaEsperado(pago, plan.Nombre))
        {
            throw new InvalidOperationException($"El pago recibido fue {pago.TransactionAmount:0.##} {pago.CurrencyId}; no coincide con la cotización comercial ni con el cobro de prueba configurado.");
        }
        string detalleProveedor = importeConvertidoPorProveedor
            ? $"Mercado Pago informó el estado '{pago.Status}'. Cobro acreditado: {pago.TransactionAmount:0.##} {pago.CurrencyId}; cotización del plan: {operacion.Importe:0.##} {operacion.Moneda}."
            : $"Mercado Pago informó el estado '{pago.Status}'.";

        // Fuera del modo de prueba el importe y la moneda deben coincidir con la cotización.
        // En desarrollo sólo se admite el importe ARS configurado para este plan.
        operacion.ReferenciaProveedor = pago.Id;
        operacion.EstadoProveedor = pago.Status;
        operacion.DetalleProveedor = detalleProveedor;
        Suscripcion suscripcion = _suscripcionMPP.AplicarResultadoCambioPlan(operacion);
        bool aprobada = string.Equals(pago.Status, "approved", StringComparison.OrdinalIgnoreCase);
        bool rechazada = string.Equals(pago.Status, "rejected", StringComparison.OrdinalIgnoreCase) || string.Equals(pago.Status, "cancelled", StringComparison.OrdinalIgnoreCase);
        string mensaje = aprobada
            ? "Suscripción actualizada correctamente."
            : rechazada
                ? "El proveedor rechazó la actualización de suscripción."
                : "La actualización quedó pendiente de confirmación del proveedor.";
        return new ResultadoGestionSuscripcion(mensaje, suscripcion.Estado, suscripcion);
    }

    public async Task<ResultadoGestionSuscripcion> CancelarRenovacion(Usuario solicitante, CancelacionRenovacionRequest solicitud)
    {
        ValidarDueño(solicitante);
        Suscripcion actual = _suscripcionMPP.ConsultarActual(solicitante) ?? throw new KeyNotFoundException("La agencia no tiene una suscripción registrada.");
        if (!actual.RenovacionAutomatica) throw new ArgumentException("La renovación automática ya se encuentra cancelada.");
        if (actual.FechaVencimiento <= DateTime.Now) throw new InvalidOperationException("El período vigente ya finalizó y no admite cancelar la renovación.");

        if (!string.IsNullOrWhiteSpace(actual.ReferenciaRenovacionProveedor))
        {
            MercadoPagoPreapproval proveedor = await _mercadoPagoService.ActualizarRenovacionAutomatica(actual.ReferenciaRenovacionProveedor, "paused");
            actual.EstadoRenovacionProveedor = proveedor.Status;
            _suscripcionMPP.ActualizarEstadoRenovacionProveedor(actual);
        }
        actual.RenovacionAutomatica = false;
        actual.MotivoRenovacion = solicitud.Motivo;
        Suscripcion resultado = _suscripcionMPP.ActualizarRenovacion(actual);
        RegistrarBitacora(solicitante, resultado, "CancelarRenovacionAutomatica", "Se canceló la renovación automática de la suscripción.");
        return new ResultadoGestionSuscripcion("Renovación automática cancelada correctamente.", resultado.Estado, resultado);
    }

    public async Task<ResultadoGestionSuscripcion> ReactivarRenovacion(Usuario solicitante)
    {
        ValidarDueño(solicitante);
        Suscripcion actual = _suscripcionMPP.ConsultarActual(solicitante) ?? throw new KeyNotFoundException("La agencia no tiene una suscripción registrada.");
        if (actual.RenovacionAutomatica) throw new ArgumentException("La renovación automática ya se encuentra activa.");
        if (!actual.Activo || actual.FechaVencimiento <= DateTime.Now) throw new InvalidOperationException("El período vigente ya finalizó y no admite reactivar la renovación.");

        if (string.IsNullOrWhiteSpace(actual.ReferenciaRenovacionProveedor))
            throw new InvalidOperationException("Para reactivar esta suscripción primero autorizá el medio de pago en Mercado Pago.");

        MercadoPagoPreapproval proveedor = await _mercadoPagoService.ActualizarRenovacionAutomatica(actual.ReferenciaRenovacionProveedor, "authorized");
        actual.RenovacionAutomatica = true;
        actual.MotivoRenovacion = null;
        Suscripcion resultado = _suscripcionMPP.ActualizarRenovacion(actual);
        resultado.EstadoRenovacionProveedor = proveedor.Status;
        _suscripcionMPP.ActualizarEstadoRenovacionProveedor(resultado);
        RegistrarBitacora(solicitante, resultado, "ReactivarRenovacionAutomatica", "Se reactivó la renovación automática de la suscripción.");
        return new ResultadoGestionSuscripcion("Renovación automática reactivada correctamente.", resultado.Estado, resultado);
    }

    public bool PuedeUsarAgencia(Usuario usuario) => _suscripcionMPP.PuedeUsarAgencia(usuario);

    public int SincronizarVencimientos()
    {
        List<SincronizacionVencimientosResultado> vencidas = _suscripcionMPP.SincronizarVencimientos();
        foreach (SincronizacionVencimientosResultado vencida in vencidas)
        {
            _bitacoraBLL.Add(new Bitacora(null, vencida.IdAgencia, "Suscripcion", vencida.IdSuscripcion, "VencerSuscripcion", "La suscripción alcanzó su fecha de vencimiento y el acceso de la agencia fue suspendido.", "Exitoso", "Advertencia", "Suscripción"));
        }
        return vencidas.Count;
    }

    public ConfiguracionRenovacionAutomatica ObtenerConfiguracionRenovacionAutomatica(Usuario solicitante)
    {
        ValidarDueño(solicitante);
        Suscripcion actual = _suscripcionMPP.ConsultarActual(solicitante) ?? throw new KeyNotFoundException("La agencia no tiene una suscripción registrada.");
        PlanComercial plan = _planMPP.ConsultarPlan(new PlanComercial(actual.IdPlanComercial));
        (string publicKey, decimal importe, string moneda) = _mercadoPagoService.ObtenerCobroRecurrente(plan.Nombre);
        return new ConfiguracionRenovacionAutomatica { PublicKey = publicKey, ImportePrueba = importe, MonedaPrueba = moneda, NombrePlan = plan.Nombre };
    }

    public async Task<ResultadoGestionSuscripcion> AutorizarRenovacionAutomatica(Usuario solicitante, AutorizarRenovacionAutomaticaRequest solicitud)
    {
        ValidarDueño(solicitante);
        Suscripcion actual = _suscripcionMPP.ConsultarActual(solicitante) ?? throw new KeyNotFoundException("La agencia no tiene una suscripción registrada.");
        if (!actual.Activo || !actual.Estado.Equals("Activa", StringComparison.OrdinalIgnoreCase) || actual.FechaVencimiento <= DateTime.Now)
            throw new InvalidOperationException("La suscripción debe estar activa para autorizar la renovación automática.");
        if (string.IsNullOrWhiteSpace(solicitud.PayerEmail) || !solicitud.PayerEmail.Contains('@', StringComparison.Ordinal) || string.IsNullOrWhiteSpace(solicitud.CardToken))
            throw new ArgumentException("Completá los datos de la cuenta de prueba en Mercado Pago.");

        PlanComercial plan = _planMPP.ConsultarPlan(new PlanComercial(actual.IdPlanComercial));
        MercadoPagoPreapproval preapproval = await _mercadoPagoService.CrearRenovacionAutomatica(
            plan.Nombre, $"ren-{actual.ID}-{Guid.NewGuid():N}", solicitud.PayerEmail.Trim(), solicitud.CardToken);
        if (!preapproval.Status.Equals("authorized", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Mercado Pago no dejó autorizada la renovación automática.");

        actual.ReferenciaRenovacionProveedor = preapproval.Id;
        actual.EstadoRenovacionProveedor = preapproval.Status;
        _suscripcionMPP.RegistrarRenovacionProveedor(actual);
        Suscripcion resultado = _suscripcionMPP.ConsultarActual(solicitante) ?? throw new InvalidOperationException("No fue posible actualizar la suscripción.");
        RegistrarBitacora(solicitante, resultado, "AutorizarRenovacionAutomatica", "Se autorizó una renovación automática mediante Mercado Pago.");
        return new ResultadoGestionSuscripcion("Renovación automática autorizada correctamente. Mercado Pago realizará los cobros recurrentes según su calendario.", resultado.Estado, resultado);
    }

    public async Task<int> SincronizarCobrosRecurrentes()
    {
        int aplicados = 0;
        foreach (RenovacionProveedorPendiente renovacion in _suscripcionMPP.ConsultarRenovacionesProveedor())
        {
            List<MercadoPagoAuthorizedPayment> pagos = await _mercadoPagoService.ConsultarCobrosRecurrentes(renovacion.ReferenciaRenovacionProveedor);
            foreach (MercadoPagoAuthorizedPayment pago in pagos)
            {
                renovacion.ReferenciaPago = pago.PaymentId;
                renovacion.Importe = pago.TransactionAmount;
                renovacion.Moneda = pago.CurrencyId;
                renovacion.Detalle = $"Cobro recurrente aprobado por Mercado Pago (autorización {renovacion.ReferenciaRenovacionProveedor}).";
                _suscripcionMPP.AplicarCobroRecurrente(renovacion);
                aplicados++;
            }
        }
        return aplicados;
    }

    private void ValidarDueño(Usuario solicitante)
    {
        if (!solicitante.IdAgencia.HasValue) throw new UnauthorizedAccessException("Tu usuario no pertenece a una agencia.");
        if (!_rolBLL.TienePermiso(solicitante, "GestionarSuscripcion")) throw new UnauthorizedAccessException("No tenés permiso para gestionar la suscripción.");
    }

    private void RegistrarBitacora(Usuario usuario, Suscripcion suscripcion, string accion, string mensaje)
    {
        _bitacoraBLL.Add(new Bitacora(usuario.ID, suscripcion.IdAgencia, "Suscripcion", suscripcion.ID, accion, mensaje, "Exitoso", "Informacion", "Suscripción"));
    }
}
