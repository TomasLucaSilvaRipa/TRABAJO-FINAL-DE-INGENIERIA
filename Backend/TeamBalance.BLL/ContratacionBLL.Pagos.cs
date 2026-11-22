using TeamBalance.BE.Entidades;
using TeamBalance.MPP;
using TeamBalance.Services;

namespace TeamBalance.BLL;

public partial class ContratacionBLL
{
    public CotizacionPagoSuscripcion CotizarPagoCombinado(PlanComercial planSolicitado)
    {
        PlanComercial plan = _planMPP.ConsultarPlan(planSolicitado);
        if (!plan.Activo) throw new KeyNotFoundException("El plan comercial no está disponible.");
        CobroMercadoPago cobro = _mercadoPagoService.ObtenerCobroPlan(plan);
        return new(plan.PrecioVigente, plan.Moneda, cobro.Importe, cobro.Moneda);
    }

    private ContratacionInicioResponse ContratarConPagos(ContratacionRequest solicitud)
    {
        List<DistribucionPagoSuscripcion> partes = solicitud.Pagos!;
        if (partes.Count < 2 || partes.Any(parte => parte is null))
            throw new ArgumentException("Seleccioná por lo menos dos pagos para combinar.");

        PlanComercial plan = _planMPP.ConsultarPlan(solicitud.PlanComercial);
        if (!plan.Activo) throw new KeyNotFoundException("El plan comercial no está disponible.");
        if (partes.Sum(parte => parte.Importe) != plan.PrecioVigente)
            throw new ArgumentException("Los pagos deben sumar exactamente el precio del plan.");

        List<PagoSuscripcion> pagos = BLLSuscripcion.PrepararDistribucion(partes, plan, 0, _mercadoPagoService.ObtenerCobroPlan(plan));
        string referencia = $"alta-{Guid.NewGuid():N}";
        for (int indice = 0; indice < pagos.Count; indice++) pagos[indice].ReferenciaInterna = $"{referencia}-p{indice + 1}";

        solicitud.ProveedorPagoSeleccionado = "Combinado";
        ContratacionPendiente contratacion = _contratacionMPP.CrearPendiente(solicitud, referencia, pagos[0].ReferenciaInterna, pagos);
        _bitacoraBLL.Add(new Bitacora(0, null, null, "ContratacionServicio", contratacion.Contratacion.ID, "IniciarContratacion", "Se inició una contratación con pago combinado.", "Pendiente", "Informacion", "Contratacion", DateTime.Now, null));
        return new($"/pago/resultado?external_reference={referencia}&iniciar=1", referencia);
    }

    public async Task<EstadoContratacionResponse> ContinuarPagoCombinado(ContratacionServicio contratacion)
    {
        var datos = _contratacionMPP.ConsultarPagosCombinados(contratacion);
        PagoSuscripcion? parte = datos.Pagos.FirstOrDefault(pago => pago.Estado != "approved");
        if (parte is null) return CrearEstadoPagoCombinado(contratacion);

        if (!string.IsNullOrEmpty(parte.UrlPago))
        {
            if (parte.Proveedor == "Stripe")
            {
                return await ConfirmarParteContratacion(contratacion, parte, parte.ReferenciaProveedor, permitirRetomarCheckout: true);
            }

            long? pago = await _mercadoPagoService.BuscarPagoActivo(parte);
            if (pago.HasValue) return await ConfirmarParteContratacion(contratacion, parte, pago.Value.ToString());
        }

        return await AbrirSiguienteCheckout(contratacion);
    }

    private async Task<EstadoContratacionResponse> VerificarPagoCombinado(string referenciaParte, string paymentId)
    {
        int separador = referenciaParte.LastIndexOf("-p", StringComparison.Ordinal);
        if (separador < 0) throw new ArgumentException("La referencia no identifica una parte del pago.");
        ContratacionServicio contratacion = new(referenciaParte[..separador]);
        var datos = _contratacionMPP.ConsultarPagosCombinados(contratacion);
        PagoSuscripcion parte = datos.Pagos.SingleOrDefault(pago => pago.ReferenciaInterna == referenciaParte)
            ?? throw new KeyNotFoundException("No encontramos esa parte del pago.");
        if (parte.Estado == "approved") return await ContinuarPagoCombinado(contratacion);
        if (!ReferenceEquals(parte, datos.Pagos.First(pago => pago.Estado != "approved")))
            throw new InvalidOperationException("Primero completá las partes anteriores.");
        return await ConfirmarParteContratacion(contratacion, parte, paymentId);
    }

    private async Task<EstadoContratacionResponse> ConfirmarParteContratacion(
        ContratacionServicio contratacion, PagoSuscripcion parte, string paymentId, bool permitirRetomarCheckout = false)
    {
        string id, estado, referencia, moneda;
        decimal importe;
        if (parte.Proveedor == "Stripe")
        {
            if (!string.Equals(paymentId, parte.ReferenciaProveedor, StringComparison.Ordinal))
                throw new InvalidOperationException("La sesión de Stripe no coincide con esta parte.");
            (id, estado, referencia, importe, moneda, _) = await _stripeService.ConsultarPago(paymentId);
        }
        else
        {
            MercadoPagoPayment pago = await _mercadoPagoService.ConsultarPago(paymentId);
            (id, estado, referencia, importe, moneda) = (pago.Id, pago.Status, pago.ExternalReference, pago.TransactionAmount, pago.CurrencyId);
        }

        if (referencia != parte.ReferenciaInterna || importe != parte.Importe || !string.Equals(moneda, parte.Moneda, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("El proveedor informó una referencia, importe o moneda que no coincide con esta parte.");

        if (estado == "pending" && permitirRetomarCheckout)
        {
            EstadoContratacionResponse pendiente = CrearEstadoPagoCombinado(contratacion);
            if (pendiente.Estado == "Pendiente") pendiente.UrlPagoSiguiente = parte.UrlPago;
            return pendiente;
        }

        bool aprobadaAhora = _contratacionMPP.GuardarResultadoParte(contratacion, parte, id, estado);
        if (aprobadaAhora)
        {
            ContratacionServicio paraRegistro = _contratacionMPP.ConsultarContratacionParaRegistro(contratacion);
            _bitacoraBLL.Add(new Bitacora(0, null, null, "ContratacionServicio", paraRegistro.ID, "VerificarPagoContratacion",
                "Se aprobaron todas las partes del pago de la contratación.", "Exitoso", "Informacion", "Contratacion", DateTime.Now, null));
            await _emailService.EnviarCorreoContinuarRegistro(paraRegistro);
        }
        if (estado == "approved" || (parte.Proveedor == "Stripe" && estado == "cancelled"))
            return await AbrirSiguienteCheckout(contratacion);
        return CrearEstadoPagoCombinado(contratacion);
    }

    private async Task<EstadoContratacionResponse> AbrirSiguienteCheckout(ContratacionServicio contratacion)
    {
        var datos = _contratacionMPP.ConsultarPagosCombinados(contratacion);
        EstadoContratacionResponse resultado = CrearEstadoPagoCombinado(contratacion, datos.Pagos);
        PagoSuscripcion? parte = datos.Pagos.FirstOrDefault(pago => pago.Estado != "approved");
        if (parte is null || resultado.Estado == "Aprobada") return resultado;

        if (string.IsNullOrEmpty(parte.UrlPago))
        {
            PlanComercial plan = _planMPP.ConsultarPlan(new PlanComercial(datos.IdPlan));
            string url;
            string sesion = string.Empty;
            if (parte.Proveedor == "Stripe")
            {
                (url, sesion) = await _stripeService.CrearPagoContratacion(parte, plan);
            }
            else
            {
                url = await _mercadoPagoService.CrearPago(new MercadoPagoPreferenceRequest(
                    $"TeamBalance · {plan.Nombre} · pago {resultado.PagosCompletados + 1}/{resultado.PagosTotales}", parte.Importe, parte.Moneda, parte.ReferenciaInterna, plan.Nombre, true)
                {
                    MedioPago = parte.MedioPago,
                    ClaveIdempotencia = $"{parte.ReferenciaInterna}-{parte.Intento}"
                });
            }
            _contratacionMPP.GuardarCheckoutParte(contratacion, parte, url, sesion);
            datos = _contratacionMPP.ConsultarPagosCombinados(contratacion);
            resultado = CrearEstadoPagoCombinado(contratacion, datos.Pagos);
            parte = datos.Pagos.FirstOrDefault(pago => pago.Estado != "approved");
        }

        if (resultado.Estado == "Pendiente") resultado.UrlPagoSiguiente = parte?.UrlPago;
        return resultado;
    }

    private EstadoContratacionResponse CrearEstadoPagoCombinado(ContratacionServicio contratacion, List<PagoSuscripcion>? pagos = null)
    {
        pagos ??= _contratacionMPP.ConsultarPagosCombinados(contratacion).Pagos;
        EstadoContratacionResponse estado = CrearRespuestaEstado(_contratacionMPP.ConsultarEstado(contratacion));
        estado.PagosCompletados = pagos.Count(pago => pago.Estado == "approved");
        estado.PagosTotales = pagos.Count;
        return estado;
    }
}
