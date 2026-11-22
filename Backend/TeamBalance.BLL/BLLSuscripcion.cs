using Microsoft.Extensions.Logging;
using TeamBalance.BE.Entidades;
using TeamBalance.MPP;
using TeamBalance.Services;

namespace TeamBalance.BLL;

public sealed partial class BLLSuscripcion
{
    private readonly MPPSuscripcion _suscripcionMPP;
    private readonly MPPPlanComercial _planMPP;
    private readonly BLLRol _rolBLL;
    private readonly BLLBitacora _bitacoraBLL;
    private readonly MercadoPagoService _mercadoPagoService;
    private readonly StripeService _stripeService;
    private readonly MPPCuentaCorriente _cuentaCorrienteMPP;
    private readonly MPPUsuario _usuarioMPP;
    private readonly EmailService _emailService;

    private readonly ILogger<BLLSuscripcion> _logger;

    public BLLSuscripcion(MPPSuscripcion suscripcionMPP, MPPPlanComercial planMPP, BLLRol rolBLL, BLLBitacora bitacoraBLL, MercadoPagoService mercadoPagoService, StripeService stripeService, MPPCuentaCorriente cuentaCorrienteMPP, MPPUsuario usuarioMPP, EmailService emailService, ILogger<BLLSuscripcion> logger)
    {
        _suscripcionMPP = suscripcionMPP;
        _planMPP = planMPP;
        _rolBLL = rolBLL;
        _bitacoraBLL = bitacoraBLL;
        _mercadoPagoService = mercadoPagoService;
        _stripeService = stripeService;
        _cuentaCorrienteMPP = cuentaCorrienteMPP;
        _usuarioMPP = usuarioMPP;
        _emailService = emailService;
        _logger = logger;
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
        return await SolicitarDistribucion(solicitante, solicitud);
    }

    public async Task<InicioActualizacionSuscripcion> SolicitarReactivacionConCreditoParcial(Usuario solicitante, CambioPlanSuscripcionRequest solicitud)
    {
        ValidarDueño(solicitante);
        if (solicitud.IdPlanComercial <= 0) throw new ArgumentException("Seleccioná una modalidad válida.");
        Suscripcion suscripcion = _suscripcionMPP.ConsultarActual(solicitante) ?? throw new KeyNotFoundException("La agencia no tiene una suscripción registrada.");
        if (suscripcion.Activo) throw new InvalidOperationException("La suscripción ya está activa.");
        PlanComercial plan = _planMPP.ConsultarPlan(new PlanComercial(solicitud.IdPlanComercial));
        if (!plan.Activo || (plan.FechaVigenciaHasta.HasValue && plan.FechaVigenciaHasta.Value < DateTime.Now)) throw new ArgumentException("La modalidad seleccionada no está disponible.");
        if (!string.Equals(solicitud.Proveedor, "MercadoPago", StringComparison.Ordinal) && !string.Equals(solicitud.Proveedor, "Stripe", StringComparison.Ordinal)) throw new ArgumentException("Seleccioná Mercado Pago o Stripe para pagar la diferencia.");

        decimal creditoDisponible = _cuentaCorrienteMPP.Consultar(solicitante)
            .Where(documento => string.Equals(documento.Tipo, "NotaCredito", StringComparison.OrdinalIgnoreCase) && string.Equals(documento.Estado, "Disponible", StringComparison.OrdinalIgnoreCase) && string.Equals(documento.Moneda, plan.Moneda, StringComparison.OrdinalIgnoreCase))
            .Sum(documento => documento.SaldoPendiente);
        if (creditoDisponible <= 0 || creditoDisponible >= plan.PrecioVigente) throw new InvalidOperationException("El saldo disponible no requiere un pago de diferencia para esta modalidad.");

        if (string.Equals(solicitud.Proveedor, "Stripe", StringComparison.Ordinal))
        {
            solicitud.CreditoAplicado = creditoDisponible;
            solicitud.ImportePago = plan.PrecioVigente - creditoDisponible;
            solicitud.MonedaPago = plan.Moneda;
        }
        else
        {
            PlanComercial planAnterior = _planMPP.ConsultarPlan(new PlanComercial(suscripcion.IdPlanComercial));
            CobroMercadoPago cobroAnterior = _mercadoPagoService.ObtenerCobroPlan(planAnterior);
            CobroMercadoPago cobroNuevo = _mercadoPagoService.ObtenerCobroPlan(plan);
            if (!string.Equals(cobroAnterior.Moneda, cobroNuevo.Moneda, StringComparison.OrdinalIgnoreCase)) throw new InvalidOperationException("No se pudo establecer la equivalencia del saldo para Mercado Pago.");
            solicitud.CreditoAplicado = Math.Min(creditoDisponible, planAnterior.PrecioVigente);
            decimal proporcionCredito = solicitud.CreditoAplicado / planAnterior.PrecioVigente;
            solicitud.ImportePago = cobroNuevo.Importe - (cobroAnterior.Importe * proporcionCredito);
            solicitud.MonedaPago = cobroNuevo.Moneda;
            if (solicitud.ImportePago <= 0) throw new InvalidOperationException("El saldo disponible cubre esta modalidad; no hace falta generar un pago adicional.");
        }

        solicitud.IdSuscripcion = suscripcion.ID;
        solicitud.ReferenciaInterna = $"cred-{Guid.NewGuid():N}";
        OperacionSuscripcionPendiente operacion = _suscripcionMPP.CrearSolicitudReactivacionConCreditoParcial(solicitud);
        string urlPago = string.Equals(solicitud.Proveedor, "Stripe", StringComparison.Ordinal)
            ? await _stripeService.CrearPago(operacion, plan)
            : await _mercadoPagoService.CrearPago(new MercadoPagoPreferenceRequest($"TeamBalance · diferencia para {plan.Nombre}", operacion.Importe, operacion.Moneda, operacion.ReferenciaInterna, plan.Nombre, true));
        RegistrarBitacora(solicitante, suscripcion, "SolicitarReactivacionConCreditoParcial", $"Se solicitó el pago de la diferencia para reactivar la modalidad {plan.Nombre} aplicando saldo a favor.");
        return new InicioActualizacionSuscripcion(urlPago, operacion.ReferenciaInterna, operacion.Importe, operacion.Moneda, DateTime.Now);
    }

    public async Task<ResultadoGestionSuscripcion> VerificarActualizacionPago(Usuario solicitante, VerificarPagoSuscripcionRequest solicitud)
    {
        ValidarDueño(solicitante);
        if (string.IsNullOrWhiteSpace(solicitud.ReferenciaOperacion) || string.IsNullOrWhiteSpace(solicitud.PaymentId)) throw new ArgumentException("No recibimos la referencia de la operación de actualización.");

        OperacionSuscripcionPendiente? distribuida = _suscripcionMPP.ConsultarDistribucion(solicitante, solicitud.ReferenciaOperacion);
        if (distribuida is not null) return await VerificarParte(solicitante, distribuida, solicitud);

        OperacionSuscripcionPendiente operacion = new OperacionSuscripcionPendiente(solicitud.ReferenciaOperacion);
        operacion = _suscripcionMPP.ConsultarOperacionPendiente(operacion);
        if (operacion.IdAgencia != solicitante.IdAgencia) throw new UnauthorizedAccessException("La operación no pertenece a tu agencia.");
        if (operacion.Estado == "Aprobada")
            return new ResultadoGestionSuscripcion("Suscripción actualizada correctamente.", "Aprobada", ConsultarSuscripcion(solicitante)) { PagoConfirmado = true };

        if (string.Equals(operacion.Proveedor, "Stripe", StringComparison.Ordinal))
        {
            (string id, string estado, string referencia, decimal importe, string moneda, string referenciaRenovacion) = await _stripeService.ConsultarPago(solicitud.PaymentId);
            if (!string.Equals(referencia, operacion.ReferenciaInterna, StringComparison.Ordinal)) throw new InvalidOperationException("La sesión devuelta por Stripe no corresponde a la actualización solicitada.");
            if (importe != operacion.Importe || !string.Equals(moneda, operacion.Moneda, StringComparison.OrdinalIgnoreCase)) throw new InvalidOperationException("El importe o la moneda informados por Stripe no coinciden con la actualización solicitada.");
            operacion.ReferenciaProveedor = id;
            operacion.EstadoProveedor = estado;
            operacion.DetalleProveedor = $"Stripe informó el estado '{estado}'.";
        }
        else
        {
            MercadoPagoPayment pago = await _mercadoPagoService.ConsultarPago(solicitud.PaymentId);
            if (!string.Equals(pago.ExternalReference, operacion.ReferenciaInterna, StringComparison.Ordinal)) throw new InvalidOperationException("La referencia devuelta por Mercado Pago no corresponde a la actualización solicitada.");
            bool requiereImporteExacto = string.Equals(operacion.TipoOperacion, "Regularización con crédito parcial", StringComparison.OrdinalIgnoreCase) || string.Equals(operacion.TipoOperacion, "Actualización con crédito parcial", StringComparison.OrdinalIgnoreCase);
            bool importeConvertidoPorProveedor = pago.TransactionAmount != operacion.Importe || !string.Equals(pago.CurrencyId, operacion.Moneda, StringComparison.OrdinalIgnoreCase);
            PlanComercial plan = _planMPP.ConsultarPlan(new PlanComercial(operacion.IdPlanNuevo));
            if (importeConvertidoPorProveedor && (requiereImporteExacto || !_mercadoPagoService.EsCobroPruebaEsperado(pago, plan.Nombre))) throw new InvalidOperationException($"El pago recibido fue {pago.TransactionAmount:0.##} {pago.CurrencyId}; no coincide con el importe a regularizar.");
            operacion.ReferenciaProveedor = pago.Id;
            operacion.EstadoProveedor = pago.Status;
            operacion.DetalleProveedor = importeConvertidoPorProveedor
                ? $"Mercado Pago informó el estado '{pago.Status}'. Cobro acreditado: {pago.TransactionAmount:0.##} {pago.CurrencyId}; cotización del plan: {operacion.Importe:0.##} {operacion.Moneda}."
                : $"Mercado Pago informó el estado '{pago.Status}'.";
        }
        Suscripcion suscripcion = _suscripcionMPP.AplicarResultadoCambioPlan(operacion);
        bool aprobada = string.Equals(operacion.EstadoProveedor, "approved", StringComparison.OrdinalIgnoreCase);
        bool rechazada = string.Equals(operacion.EstadoProveedor, "rejected", StringComparison.OrdinalIgnoreCase) || string.Equals(operacion.EstadoProveedor, "cancelled", StringComparison.OrdinalIgnoreCase);
        string mensaje = aprobada
            ? "Suscripción actualizada correctamente."
            : rechazada
                ? "El proveedor rechazó la actualización de suscripción."
                : "La actualización quedó pendiente de confirmación del proveedor.";
        if (aprobada)
        {
            PlanComercial planActualizado = _planMPP.ConsultarPlan(new PlanComercial(suscripcion.IdPlanComercial));
            await NotificarActualizacionSuscripcion(solicitante, planActualizado);
        }
        RegistrarBitacora(solicitante, suscripcion, "VerificarActualizacionSuscripcion", mensaje);
        return new ResultadoGestionSuscripcion(mensaje, operacion.EstadoProveedor, suscripcion) { PagoConfirmado = aprobada };
    }

    private async Task NotificarActualizacionSuscripcion(Usuario solicitante, PlanComercial plan)
    {
        try
        {
            Usuario destinatario = _usuarioMPP.ConsultarUsuarioPorId(new Usuario { ID = solicitante.ID }) ?? solicitante;
            bool enviado = await _emailService.EnviarCorreoActualizacionSuscripcion(destinatario, plan);
            if (!enviado)
            {
                Console.Error.WriteLine($"No se pudo enviar el correo de actualización de suscripción al usuario {solicitante.ID}.");
            }
        }
        catch (Exception ex)
        {
            // El proveedor ya confirmó el pago y el cambio de plan ya fue aplicado.
            // Un fallo de correo no debe transformar una operación aprobada en un error 502.
            Console.Error.WriteLine($"No se pudo enviar el correo de actualización de suscripción al usuario {solicitante.ID}: {ex.Message}");
        }
    }

    public async Task<ResultadoGestionSuscripcion> CancelarRenovacion(Usuario solicitante, CancelacionRenovacionRequest solicitud)
    {
        ValidarDueño(solicitante);
        Suscripcion actual = _suscripcionMPP.ConsultarActual(solicitante) ?? throw new KeyNotFoundException("La agencia no tiene una suscripción registrada.");
        if (!actual.RenovacionAutomatica) throw new ArgumentException("La renovación automática ya se encuentra cancelada.");
        if (actual.FechaVencimiento <= DateTime.Now) throw new InvalidOperationException("El período vigente ya finalizó y no admite cancelar la renovación.");

        if (!string.IsNullOrWhiteSpace(actual.ReferenciaRenovacionProveedor))
        {
            if (string.Equals(actual.ProveedorRenovacion, "Stripe", StringComparison.Ordinal)) actual.EstadoRenovacionProveedor = await _stripeService.ActualizarRenovacionAutomatica(actual.ReferenciaRenovacionProveedor, false);
            else
            {
                MercadoPagoPreapproval proveedor = await _mercadoPagoService.ActualizarRenovacionAutomatica(actual.ReferenciaRenovacionProveedor, "paused");
                actual.EstadoRenovacionProveedor = proveedor.Status;
            }
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

        if (string.IsNullOrWhiteSpace(actual.ReferenciaRenovacionProveedor)) throw new InvalidOperationException("Para reactivar esta suscripción primero autorizá un medio de pago.");

        if (string.Equals(actual.ProveedorRenovacion, "Stripe", StringComparison.Ordinal)) actual.EstadoRenovacionProveedor = await _stripeService.ActualizarRenovacionAutomatica(actual.ReferenciaRenovacionProveedor, true);
        else
        {
            MercadoPagoPreapproval proveedor = await _mercadoPagoService.ActualizarRenovacionAutomatica(actual.ReferenciaRenovacionProveedor, "authorized");
            actual.EstadoRenovacionProveedor = proveedor.Status;
        }
        actual.RenovacionAutomatica = true;
        actual.MotivoRenovacion = null;
        Suscripcion resultado = _suscripcionMPP.ActualizarRenovacion(actual);
        resultado.EstadoRenovacionProveedor = actual.EstadoRenovacionProveedor;
        resultado.ProveedorRenovacion = actual.ProveedorRenovacion;
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
        return new ConfiguracionRenovacionAutomatica(publicKey, importe, moneda, plan);
    }

    public async Task<InicioActualizacionSuscripcion> AutorizarRenovacionAutomatica(Usuario solicitante, AutorizarRenovacionAutomaticaRequest solicitud)
    {
        ValidarDueño(solicitante);
        Suscripcion actual = _suscripcionMPP.ConsultarActual(solicitante) ?? throw new KeyNotFoundException("La agencia no tiene una suscripción registrada.");
        if (!actual.Activo || !actual.Estado.Equals("Activa", StringComparison.OrdinalIgnoreCase) || actual.FechaVencimiento <= DateTime.Now)
            throw new InvalidOperationException("La suscripción debe estar activa para autorizar la renovación automática.");
        if (string.IsNullOrWhiteSpace(solicitud.PayerEmail) || !solicitud.PayerEmail.Contains('@', StringComparison.Ordinal))
            throw new ArgumentException("Ingresá un email válido para continuar en Mercado Pago.");
        if (!string.IsNullOrWhiteSpace(actual.ReferenciaRenovacionProveedor))
            throw new InvalidOperationException("La suscripción ya tiene una autorización recurrente configurada.");

        PlanComercial plan = _planMPP.ConsultarPlan(new PlanComercial(actual.IdPlanComercial));
        string referencia = $"ren-{actual.ID}-{Guid.NewGuid():N}";
        SolicitudRenovacionMercadoPago renovacion = new SolicitudRenovacionMercadoPago(plan.Nombre, referencia, solicitud.PayerEmail.Trim());
        MercadoPagoPreapproval preapproval = await _mercadoPagoService.CrearRenovacionAutomatica(renovacion);
        if (!preapproval.Status.Equals("pending", StringComparison.OrdinalIgnoreCase) || string.IsNullOrWhiteSpace(preapproval.InitPoint))
            throw new InvalidOperationException("Mercado Pago no devolvió el checkout para autorizar la renovación automática.");

        CobroMercadoPago cobro = _mercadoPagoService.ObtenerCobroPlan(plan);
        return new InicioActualizacionSuscripcion(preapproval.InitPoint, referencia, cobro.Importe, cobro.Moneda, actual.FechaVencimiento)
        {
            PreapprovalId = preapproval.Id,
        };
    }

    public async Task<ResultadoGestionSuscripcion> VerificarRenovacionAutomaticaMercadoPago(Usuario solicitante, VerificarPagoSuscripcionRequest solicitud)
    {
        ValidarDueño(solicitante);
        MercadoPagoPreapproval preapproval = string.IsNullOrWhiteSpace(solicitud.PaymentId)
            ? await _mercadoPagoService.ConsultarRenovacionAutomatica(solicitud.ReferenciaOperacion)
            : await _mercadoPagoService.ConsultarRenovacionPorId(solicitud.PaymentId);

        string[] segmentos = preapproval.ExternalReference.Split('-');
        if (segmentos.Length != 3 || segmentos[0] != "ren" || !int.TryParse(segmentos[1], out int idSuscripcion) ||
            !Guid.TryParseExact(segmentos[2], "N", out _) ||
            (!string.IsNullOrWhiteSpace(solicitud.ReferenciaOperacion) && solicitud.ReferenciaOperacion != preapproval.ExternalReference))
            throw new ArgumentException("La referencia de renovación automática no es válida.");

        Suscripcion actual = ConsultarSuscripcion(solicitante);
        if (actual.ID != idSuscripcion) throw new UnauthorizedAccessException("La renovación no pertenece a la suscripción de tu agencia.");
        if (!preapproval.Status.Equals("authorized", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Mercado Pago todavía no confirmó la autorización de la renovación automática.");

        return RegistrarRenovacionConfirmada(solicitante, actual, "MercadoPago", preapproval.Id, preapproval.Status);
    }

    public async Task<int> SincronizarCobrosRecurrentes()
    {
        int aplicados = 0;
        foreach (RenovacionProveedorPendiente renovacion in _suscripcionMPP.ConsultarRenovacionesProveedor())
        {
            if (string.Equals(renovacion.Proveedor, "Stripe", StringComparison.Ordinal))
            {
                List<(string ReferenciaPago, decimal Importe, string Moneda, string Estado)> pagosStripe = await _stripeService.ConsultarCobrosRecurrentes(renovacion.ReferenciaRenovacionProveedor);
                foreach ((string ReferenciaPago, decimal Importe, string Moneda, string Estado) pago in pagosStripe)
                {
                    renovacion.ReferenciaPago = pago.ReferenciaPago;
                    renovacion.Importe = pago.Importe;
                    renovacion.Moneda = pago.Moneda;
                    renovacion.Detalle = $"Cobro recurrente aprobado por Stripe (suscripción {renovacion.ReferenciaRenovacionProveedor}).";
                    _suscripcionMPP.AplicarCobroRecurrente(renovacion);
                    _bitacoraBLL.Add(new Bitacora(null, renovacion.IdAgencia, "Suscripcion", renovacion.IdSuscripcion, "AplicarCobroRecurrente", $"Se aplicó un cobro recurrente de {renovacion.Importe:0.##} {renovacion.Moneda} informado por Stripe.", "Exitoso", "Informacion", "Suscripción"));
                    aplicados++;
                }
            }
            else
            {
                List<MercadoPagoAuthorizedPayment> pagos = await _mercadoPagoService.ConsultarCobrosRecurrentes(renovacion.ReferenciaRenovacionProveedor);
                foreach (MercadoPagoAuthorizedPayment pago in pagos)
                {
                    renovacion.ReferenciaPago = pago.PaymentId;
                    renovacion.Importe = pago.TransactionAmount;
                    renovacion.Moneda = pago.CurrencyId;
                    renovacion.Detalle = $"Cobro recurrente aprobado por Mercado Pago (autorización {renovacion.ReferenciaRenovacionProveedor}).";
                    _suscripcionMPP.AplicarCobroRecurrente(renovacion);
                    _bitacoraBLL.Add(new Bitacora(null, renovacion.IdAgencia, "Suscripcion", renovacion.IdSuscripcion, "AplicarCobroRecurrente", $"Se aplicó un cobro recurrente de {renovacion.Importe:0.##} {renovacion.Moneda} informado por Mercado Pago.", "Exitoso", "Informacion", "Suscripción"));
                    aplicados++;
                }
            }
        }
        return aplicados;
    }

    public async Task<InicioActualizacionSuscripcion> SolicitarRenovacionAutomaticaStripe(Usuario solicitante)
    {
        ValidarDueño(solicitante);
        Suscripcion actual = _suscripcionMPP.ConsultarActual(solicitante) ?? throw new KeyNotFoundException("La agencia no tiene una suscripción registrada.");
        if (!actual.Activo || !actual.Estado.Equals("Activa", StringComparison.OrdinalIgnoreCase) || actual.FechaVencimiento <= DateTime.Now) throw new InvalidOperationException("La suscripción debe estar activa para autorizar la renovación automática.");
        if (!string.IsNullOrWhiteSpace(actual.ReferenciaRenovacionProveedor)) throw new InvalidOperationException("La suscripción ya tiene una autorización recurrente configurada.");
        PlanComercial plan = _planMPP.ConsultarPlan(new PlanComercial(actual.IdPlanComercial));
        string urlPago = await _stripeService.CrearRenovacionAutomatica(actual, plan);
        RegistrarBitacora(solicitante, actual, "SolicitarRenovacionAutomaticaStripe", "Se inició la autorización de renovación automática mediante Stripe.");
        return new InicioActualizacionSuscripcion(urlPago, $"ren-{actual.ID}", plan.PrecioVigente, plan.Moneda, actual.FechaVencimiento);
    }

    public async Task<ResultadoGestionSuscripcion> VerificarRenovacionAutomaticaStripe(Usuario solicitante, string sessionId)
    {
        ValidarDueño(solicitante);
        (string id, string estado, string referencia, decimal importe, string moneda, string referenciaRenovacion) = await _stripeService.ConsultarPago(sessionId);
        if (!referencia.StartsWith("ren-", StringComparison.Ordinal) || !int.TryParse(referencia.AsSpan(4), out int idSuscripcion)) throw new InvalidOperationException("La sesión de Stripe no corresponde a una renovación automática.");
        if (!string.Equals(estado, "approved", StringComparison.OrdinalIgnoreCase) || string.IsNullOrWhiteSpace(referenciaRenovacion)) throw new InvalidOperationException("Stripe todavía no confirmó la autorización de la renovación automática.");

        Suscripcion actual = ConsultarSuscripcion(solicitante);
        if (actual.ID != idSuscripcion) throw new UnauthorizedAccessException("La renovación no pertenece a la suscripción de tu agencia.");
        return RegistrarRenovacionConfirmada(solicitante, actual, "Stripe", referenciaRenovacion, "active");
    }

    private ResultadoGestionSuscripcion RegistrarRenovacionConfirmada(Usuario usuario, Suscripcion actual, string proveedor, string referencia, string estado)
    {
        if (!actual.Activo) throw new InvalidOperationException("La suscripción debe estar activa para confirmar la renovación.");
        if (!string.IsNullOrWhiteSpace(actual.ReferenciaRenovacionProveedor))
        {
            if (actual.ReferenciaRenovacionProveedor != referencia || actual.ProveedorRenovacion != proveedor)
                throw new InvalidOperationException("La suscripción ya tiene otra autorización recurrente configurada.");
            string mensaje = actual.RenovacionAutomatica ? "La renovación automática ya está autorizada." : "La autorización existe y la renovación está desactivada.";
            return new ResultadoGestionSuscripcion(mensaje, actual.Estado, actual);
        }

        actual.ReferenciaRenovacionProveedor = referencia;
        actual.EstadoRenovacionProveedor = estado;
        actual.ProveedorRenovacion = proveedor;
        _suscripcionMPP.RegistrarRenovacionProveedor(actual);
        actual.RenovacionAutomatica = true;
        try
        {
            RegistrarBitacora(usuario, actual, "AutorizarRenovacionAutomatica", $"Se autorizó una renovación automática mediante {proveedor}.");
        }
        catch (Exception ex)
        {
            // La autorización ya quedó registrada; repetir el checkout no arregla la bitácora.
            _logger.LogError(ex, "No se pudo registrar en bitácora la renovación confirmada de la suscripción {IdSuscripcion}.", actual.ID);
        }
        return new ResultadoGestionSuscripcion("Renovación automática autorizada correctamente.", actual.Estado, actual);
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
