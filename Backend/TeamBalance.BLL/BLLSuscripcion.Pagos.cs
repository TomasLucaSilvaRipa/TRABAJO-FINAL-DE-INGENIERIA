using TeamBalance.BE.Entidades;
using TeamBalance.Services;

namespace TeamBalance.BLL;

public sealed partial class BLLSuscripcion
{
    public CotizacionPagoSuscripcion ConsultarCotizacionPago(Usuario usuario, int idPlan)
    {
        ValidarDueño(usuario);
        PlanComercial plan = _planMPP.ConsultarPlan(new PlanComercial(idPlan));
        if (!plan.Activo) throw new ArgumentException("La modalidad no está disponible.");
        CobroMercadoPago cobro = _mercadoPagoService.ObtenerCobroPlan(plan);
        return new(plan.PrecioVigente, plan.Moneda, cobro.Importe, cobro.Moneda);
    }

    private async Task<InicioActualizacionSuscripcion> SolicitarDistribucion(Usuario usuario, CambioPlanSuscripcionRequest solicitud)
    {
        ValidarDueño(usuario);
        Suscripcion actual = ConsultarSuscripcion(usuario);
        PlanComercial plan = _planMPP.ConsultarPlan(new PlanComercial(solicitud.IdPlanComercial));
        if (!plan.Activo || plan.PrecioVigente <= 0 || plan.FechaVigenciaHasta < DateTime.Now)
            throw new ArgumentException("Seleccioná una modalidad vigente.");
        if (actual.Activo && actual.IdPlanComercial == plan.ID)
            throw new ArgumentException("La modalidad seleccionada ya es la contratada.");

        decimal credito = 0;
        if (solicitud.UsarSaldoAFavor)
        {
            decimal disponible = _cuentaCorrienteMPP.Consultar(usuario)
                .Where(d => d.Tipo == "NotaCredito" && d.Estado == "Disponible" && string.Equals(d.Moneda, plan.Moneda, StringComparison.OrdinalIgnoreCase))
                .Sum(d => d.SaldoPendiente);
            credito = solicitud.ImporteCredito ?? Math.Min(disponible, plan.PrecioVigente);
            if (credito <= 0 || credito > disponible || credito > plan.PrecioVigente || decimal.Round(credito, 2) != credito)
                throw new ArgumentException("El importe de la nota de crédito debe estar dentro del saldo disponible.");
        }

        List<DistribucionPagoSuscripcion> partes = solicitud.Pagos ?? new();
        if (solicitud.Pagos is null && credito < plan.PrecioVigente)
            partes.Add(new() { Proveedor = solicitud.Proveedor, Importe = plan.PrecioVigente - credito });
        List<PagoSuscripcion> pagos = PrepararDistribucion(partes, plan, credito, _mercadoPagoService.ObtenerCobroPlan(plan));
        solicitud.IdSuscripcion = actual.ID;
        solicitud.ReferenciaInterna = $"sus-{Guid.NewGuid():N}";
        solicitud.CreditoAplicado = credito;
        for (int i = 0; i < pagos.Count; i++) pagos[i].ReferenciaInterna = $"{solicitud.ReferenciaInterna}-p{i + 1}";
        OperacionSuscripcionPendiente operacion = _suscripcionMPP.CrearDistribucion(usuario, solicitud, pagos);
        ResultadoGestionSuscripcion resultado;
        try { resultado = await PrepararSiguientePago(usuario, operacion); }
        catch
        {
            try { _suscripcionMPP.DescartarSinCheckout(usuario, operacion.ReferenciaInterna); }
            catch (Exception ex) { Console.Error.WriteLine($"[Suscripciones] No se pudo liberar una solicitud sin checkout: {ex.Message}"); }
            throw;
        }
        RegistrarBitacora(usuario, actual, "SolicitarActualizacionSuscripcion", $"Se solicitó {plan.Nombre} con {pagos.Count} pago(s) externo(s) y {credito:0.00} de crédito interno.");
        return new InicioActualizacionSuscripcion(resultado.UrlPagoSiguiente ?? string.Empty, operacion.ReferenciaInterna, operacion.Importe, operacion.Moneda, DateTime.Now);
    }

    public static List<PagoSuscripcion> PrepararDistribucion(List<DistribucionPagoSuscripcion> partes, PlanComercial plan, decimal credito, CobroMercadoPago cobro)
    {
        if (plan.PrecioVigente <= 0 || credito < 0 || credito > plan.PrecioVigente || partes.Any(p => p.Importe <= 0 || decimal.Round(p.Importe, 2) != p.Importe))
            throw new ArgumentException("Los importes deben ser positivos y tener como máximo dos decimales.");
        if (partes.Sum(p => p.Importe) + credito != plan.PrecioVigente)
            throw new ArgumentException("La suma de los pagos y la nota de crédito debe coincidir con el total del plan.");
        decimal acumuladoMercadoPago = 0;
        decimal cobradoMercadoPago = 0;
        List<PagoSuscripcion> pagos = new();
        foreach (DistribucionPagoSuscripcion parte in partes)
        {
            if (parte.Proveedor != "MercadoPago" && parte.Proveedor != "Stripe") throw new ArgumentException("Seleccioná un proveedor válido para cada pago.");
            if (parte.MedioPago is not ("" or "credit_card" or "debit_card" or "account_money")) throw new ArgumentException("Seleccioná un medio de pago válido.");
            if (parte.Proveedor == "Stripe" && (parte.MedioPago == "account_money" || !plan.Moneda.Equals("USD", StringComparison.OrdinalIgnoreCase)))
                throw new ArgumentException("Stripe admite tarjetas y pagos en USD.");
            decimal importe = parte.Importe;
            string moneda = plan.Moneda;
            if (parte.Proveedor == "MercadoPago")
            {
                acumuladoMercadoPago += parte.Importe;
                decimal nuevoTotal = decimal.Round(cobro.Importe * acumuladoMercadoPago / plan.PrecioVigente, 2, MidpointRounding.AwayFromZero);
                importe = nuevoTotal - cobradoMercadoPago;
                cobradoMercadoPago = nuevoTotal;
                moneda = cobro.Moneda;
            }
            if (importe <= 0) throw new ArgumentException("Una de las partes es demasiado pequeña para cobrarla. Aumentá su importe.");
            pagos.Add(new() { Proveedor = parte.Proveedor, MedioPago = parte.MedioPago, ImportePlan = parte.Importe, Importe = importe, Moneda = moneda });
        }
        return pagos;
    }

    public async Task<ResultadoGestionSuscripcion> ContinuarActualizacion(Usuario usuario, string referencia)
    {
        ValidarDueño(usuario);
        OperacionSuscripcionPendiente operacion = _suscripcionMPP.ConsultarDistribucion(usuario, referencia)
            ?? throw new KeyNotFoundException("No encontramos una actualización de esta agencia para continuar.");
        return await PrepararSiguientePago(usuario, operacion, reanudando: true);
    }

    private async Task<ResultadoGestionSuscripcion> VerificarParte(Usuario usuario, OperacionSuscripcionPendiente operacion, VerificarPagoSuscripcionRequest solicitud)
    {
        PagoSuscripcion parte = operacion.Pagos.SingleOrDefault(p => p.ReferenciaInterna == solicitud.ReferenciaOperacion)
            ?? throw new ArgumentException("La referencia recibida no identifica una parte del pago.");
        if (parte.Estado != "Aprobada")
        {
            string id, estado, referencia, moneda;
            decimal importe;
            if (parte.Proveedor == "Stripe")
            {
                (id, estado, referencia, importe, moneda, _) = await _stripeService.ConsultarPago(solicitud.PaymentId);
            }
            else
            {
                MercadoPagoPayment pago = await _mercadoPagoService.ConsultarPago(solicitud.PaymentId);
                (id, estado, referencia, importe, moneda) = (pago.Id, pago.Status, pago.ExternalReference, pago.TransactionAmount, pago.CurrencyId);
            }
            if (referencia != parte.ReferenciaInterna || importe != parte.Importe || !string.Equals(moneda, parte.Moneda, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("El pago informado por el proveedor no coincide con la referencia, el importe o la moneda de esta parte.");
            operacion = _suscripcionMPP.ConfirmarParte(usuario, operacion.ReferenciaInterna, parte.ReferenciaInterna, id, estado);
            if (operacion.AplicadaAhora) await NotificarActualizacionSuscripcion(usuario, _planMPP.ConsultarPlan(operacion.PlanNuevo));
            if (!estado.Equals("approved", StringComparison.OrdinalIgnoreCase))
                return ResultadoDistribucion(operacion, "Esta parte todavía no está aprobada. Podés consultar su estado o continuar desde la suscripción.");
        }
        return await PrepararSiguientePago(usuario, operacion);
    }

    private async Task<ResultadoGestionSuscripcion> PrepararSiguientePago(Usuario usuario, OperacionSuscripcionPendiente operacion, bool reanudando = false)
    {
        if (operacion.Estado == "Aprobada") return ResultadoFinal(usuario, operacion);
        PagoSuscripcion? siguiente = operacion.Pagos.FirstOrDefault(p => p.Estado != "Aprobada");
        if (siguiente is null)
        {
            operacion = _suscripcionMPP.ConfirmarParte(usuario, operacion.ReferenciaInterna, string.Empty, string.Empty, "approved");
            if (operacion.AplicadaAhora) await NotificarActualizacionSuscripcion(usuario, _planMPP.ConsultarPlan(operacion.PlanNuevo));
            return ResultadoFinal(usuario, operacion);
        }
        if (reanudando && siguiente.Proveedor == "MercadoPago")
        {
            // Si se cerró el checkout sin volver a TeamBalance, la aprobación aún puede estar en Mercado Pago.
            long? pago = await _mercadoPagoService.BuscarPagoActivo(siguiente);
            if (pago.HasValue)
                return await VerificarParte(usuario, operacion, new() { ReferenciaOperacion = siguiente.ReferenciaInterna, PaymentId = pago.Value.ToString() });
        }
        if (siguiente.Estado == "Pendiente" && !string.IsNullOrWhiteSpace(siguiente.ReferenciaProveedor))
            return await VerificarParte(usuario, operacion, new() { ReferenciaOperacion = siguiente.ReferenciaInterna, PaymentId = siguiente.ReferenciaProveedor });
        if (string.IsNullOrWhiteSpace(siguiente.UrlPago))
        {
            PlanComercial plan = _planMPP.ConsultarPlan(operacion.PlanNuevo);
            string url;
            if (siguiente.Proveedor == "Stripe") url = await _stripeService.CrearPago(siguiente, plan);
            else
            {
                var preferencia = new MercadoPagoPreferenceRequest($"TeamBalance · {plan.Nombre} · pago {operacion.Pagos.IndexOf(siguiente) + 1}/{operacion.Pagos.Count}", siguiente.Importe, siguiente.Moneda, siguiente.ReferenciaInterna, plan.Nombre, true)
                { MedioPago = reanudando ? string.Empty : siguiente.MedioPago, ClaveIdempotencia = $"{siguiente.ReferenciaInterna}-{siguiente.Intento}" };
                url = await _mercadoPagoService.CrearPago(preferencia);
            }
            _suscripcionMPP.GuardarCheckout(usuario, siguiente, url);
            operacion = _suscripcionMPP.ConsultarDistribucion(usuario, operacion.ReferenciaInterna)!;
            siguiente = operacion.Pagos.FirstOrDefault(p => p.Estado != "Aprobada");
        }
        if (reanudando && siguiente is { Proveedor: "MercadoPago", MedioPago.Length: > 0, UrlPago.Length: > 0 })
            await _mercadoPagoService.HabilitarOtrosMedios(siguiente.UrlPago);

        ResultadoGestionSuscripcion resultado = ResultadoDistribucion(operacion, "Continuamos con la siguiente parte del pago.");
        resultado.UrlPagoSiguiente = siguiente?.UrlPago;
        return resultado;
    }

    private ResultadoGestionSuscripcion ResultadoFinal(Usuario usuario, OperacionSuscripcionPendiente operacion)
    {
        Suscripcion suscripcion = ConsultarSuscripcion(usuario);
        ResultadoGestionSuscripcion resultado = ResultadoDistribucion(operacion, "Suscripción actualizada correctamente.");
        resultado.Suscripcion = suscripcion;
        return resultado;
    }

    private static ResultadoGestionSuscripcion ResultadoDistribucion(OperacionSuscripcionPendiente operacion, string mensaje)
    {
        return new ResultadoGestionSuscripcion(mensaje, operacion.Estado)
        {
            ReferenciaOperacion = operacion.ReferenciaInterna,
            PagoConfirmado = operacion.Estado == "Aprobada",
            PagosCompletados = operacion.Pagos.Count(p => p.Estado == "Aprobada"),
            PagosTotales = operacion.Pagos.Count,
        };
    }
}
