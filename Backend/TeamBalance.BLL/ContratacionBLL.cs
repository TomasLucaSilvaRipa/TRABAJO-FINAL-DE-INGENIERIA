using System;
using TeamBalance.BE.Entidades;
using TeamBalance.MPP;
using TeamBalance.Services;

namespace TeamBalance.BLL
{
    public class ContratacionBLL
    {
        private readonly MercadoPagoService _mercadoPagoService;
        private readonly StripeService _stripeService;
        private readonly MPPContratacion _contratacionMPP;
        private readonly MPPPlanComercial _planMPP;
        private readonly BLLBitacora _bitacoraBLL;
        private readonly EmailService _emailService;

        public ContratacionBLL(MercadoPagoService mercadoPagoService, StripeService stripeService, MPPContratacion contratacionMPP, MPPPlanComercial planMPP, BLLBitacora bitacoraBLL, EmailService emailService)
        {
            _mercadoPagoService = mercadoPagoService;
            _stripeService = stripeService;
            _contratacionMPP = contratacionMPP;
            _planMPP = planMPP;
            _bitacoraBLL = bitacoraBLL;
            _emailService = emailService;
        }

        public async Task<ContratacionInicioResponse> Contratar(ContratacionRequest request)
        {
            ValidarSolicitud(request);

            if (!string.Equals(request.ProveedorPagoSeleccionado, "MercadoPago", StringComparison.Ordinal) && !string.Equals(request.ProveedorPagoSeleccionado, "Stripe", StringComparison.Ordinal))
            {
                throw new InvalidOperationException("Seleccioná Mercado Pago o Stripe como proveedor de pago.");
            }

            string referenciaContratacion = Guid.NewGuid().ToString("N");
            string referenciaOperacion = Guid.NewGuid().ToString("N");

            ContratacionPendiente contratacion = _contratacionMPP.CrearPendiente(
                request,
                referenciaContratacion,
                referenciaOperacion);

            Bitacora bitacora = new Bitacora(0, null, null, "ContratacionServicio", contratacion.IdContratacion, "IniciarContratacion", "Se inició una contratación pendiente de pago.", "Pendiente", "Informacion", "Contratacion", DateTime.Now, null);
            _bitacoraBLL.Add(bitacora);

            PlanComercial plan = _planMPP.ConsultarPlan(new PlanComercial(contratacion.IdPlanComercial));
            string urlPago = string.Equals(request.ProveedorPagoSeleccionado, "Stripe", StringComparison.Ordinal)
                ? await _stripeService.CrearPago(contratacion, plan)
                : await _mercadoPagoService.CrearPago(new MercadoPagoPreferenceRequest("Suscripción TeamBalance", contratacion.Importe, contratacion.Moneda, contratacion.ReferenciaContratacion, plan.Nombre));

            ContratacionInicioResponse respuesta = new ContratacionInicioResponse(urlPago, contratacion.ReferenciaContratacion);
            return respuesta;
        }

        public EstadoContratacionResponse ConsultarEstado(ContratacionServicio contratacionServicio)
        {
            EstadoContratacionPersistido contratacion = _contratacionMPP.ConsultarEstado(contratacionServicio);
            return CrearRespuestaEstado(contratacion);
        }

        public ContratacionServicio ConsultarContratacionParaRegistro(ContratacionServicio contratacionServicio)
        {
            return _contratacionMPP.ConsultarContratacionParaRegistro(contratacionServicio);
        }

        public async Task<EstadoContratacionResponse> VerificarPagoMercadoPago(ContratacionServicio contratacionServicio, string paymentId)
        {
            if (string.IsNullOrWhiteSpace(paymentId))
            {
                throw new ArgumentException("Mercado Pago no informó el identificador del pago.", nameof(paymentId));
            }

            EstadoContratacionPersistido contratacion = _contratacionMPP.ConsultarEstado(contratacionServicio);

            if (string.Equals(contratacion.EstadoContratacion, "Aprobada", StringComparison.OrdinalIgnoreCase))
            {
                return CrearRespuestaEstado(contratacion);
            }

            string referenciaProveedor;
            string estadoProveedor;
            string mensajeProveedor;

            if (string.Equals(contratacion.ProveedorPagoSeleccionado, "Stripe", StringComparison.Ordinal))
            {
                (string id, string estado, string referencia, decimal importe, string moneda, string referenciaRenovacion) = await _stripeService.ConsultarPago(paymentId);
                if (!string.Equals(referencia, contratacionServicio.ReferenciaContratacion, StringComparison.Ordinal)) throw new InvalidOperationException("La sesión de Stripe no corresponde a la contratación indicada.");
                if (importe != contratacion.Importe || !string.Equals(moneda, contratacion.Moneda, StringComparison.OrdinalIgnoreCase)) throw new InvalidOperationException("El importe o la moneda informados por Stripe no coinciden con la contratación.");
                referenciaProveedor = id;
                estadoProveedor = estado;
                mensajeProveedor = $"Stripe informó el estado '{estado}'.";
            }
            else
            {
                MercadoPagoPayment payment = await _mercadoPagoService.ConsultarPago(paymentId);
                if (!string.Equals(payment.ExternalReference, contratacionServicio.ReferenciaContratacion, StringComparison.Ordinal)) throw new InvalidOperationException("El pago no corresponde a la contratación indicada.");
                if (payment.TransactionAmount != contratacion.Importe || !string.Equals(payment.CurrencyId, contratacion.Moneda, StringComparison.OrdinalIgnoreCase)) throw new InvalidOperationException("El importe o la moneda informados por Mercado Pago no coinciden con la contratación.");
                referenciaProveedor = payment.Id;
                estadoProveedor = payment.Status;
                mensajeProveedor = $"Mercado Pago informó el estado '{payment.Status}'.";
            }

            EstadoContratacionPersistido resultado = _contratacionMPP.ActualizarResultadoPago(contratacionServicio, referenciaProveedor, estadoProveedor, mensajeProveedor);

            if (string.Equals(resultado.EstadoContratacion, "Aprobada", StringComparison.OrdinalIgnoreCase))
            {
                ContratacionServicio contratacionParaRegistro = _contratacionMPP.ConsultarContratacionParaRegistro(contratacionServicio);

                await _emailService.EnviarCorreoContinuarRegistro(contratacionParaRegistro);
            }

            return CrearRespuestaEstado(resultado);
        }

        private static EstadoContratacionResponse CrearRespuestaEstado(EstadoContratacionPersistido contratacion)
        {
            bool puedeRegistrar = string.Equals(contratacion.EstadoContratacion, "Aprobada", StringComparison.OrdinalIgnoreCase) && !contratacion.IdAgencia.HasValue && !contratacion.IdUsuario.HasValue;
            EstadoContratacionResponse respuesta = new EstadoContratacionResponse(contratacion.ReferenciaContratacion, contratacion.EstadoContratacion, puedeRegistrar);
            return respuesta;
        }

        private static void ValidarSolicitud(ContratacionRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.NombreComercialAgencia) ||
                string.IsNullOrWhiteSpace(request.CUIT) ||
                string.IsNullOrWhiteSpace(request.NombreResponsable) ||
                string.IsNullOrWhiteSpace(request.ApellidoResponsable) ||
                string.IsNullOrWhiteSpace(request.EmailLaboralResponsable))
            {
                throw new ArgumentException("Completá los datos obligatorios de la contratación.");
            }

            if (request.IdPlanComercial <= 0){ throw new ArgumentException("Seleccioná un plan comercial válido."); }
        }
    }
}
