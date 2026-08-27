using System;
using TeamBalance.BE.Entidades;
using TeamBalance.MPP;
using TeamBalance.Services;

namespace TeamBalance.BLL
{
    public class ContratacionBLL
    {
        private readonly MercadoPagoService _mercadoPagoService;
        private readonly MPPContratacion _contratacionMPP;

        public ContratacionBLL(MercadoPagoService mercadoPagoService, MPPContratacion contratacionMPP)
        {
            _mercadoPagoService = mercadoPagoService;
            _contratacionMPP = contratacionMPP;
        }

        public async Task<ContratacionInicioResponse> Contratar(ContratacionRequest request)
        {
            ValidarSolicitud(request);

            if (!string.Equals(request.ProveedorPagoSeleccionado, "MercadoPago", StringComparison.Ordinal))
            {
                throw new InvalidOperationException("Por el momento, la contratación está disponible únicamente con Mercado Pago.");
            }

            var referenciaContratacion = Guid.NewGuid().ToString("N");
            var referenciaOperacion = Guid.NewGuid().ToString("N");

            ContratacionPendiente contratacion = _contratacionMPP.CrearPendiente(
                request,
                referenciaContratacion,
                referenciaOperacion);

            string urlPago = await _mercadoPagoService.CrearPago(contratacion);

            return new ContratacionInicioResponse
            {
                UrlPago = urlPago,
                Referencia = contratacion.ReferenciaContratacion,
            };
        }

        public EstadoContratacionResponse ConsultarEstado(string referenciaContratacion)
        {
            EstadoContratacionPersistido contratacion = _contratacionMPP.ConsultarEstado(referenciaContratacion);
            return CrearRespuestaEstado(contratacion);
        }

        public async Task<EstadoContratacionResponse> VerificarPagoMercadoPago(
            string referenciaContratacion,
            string paymentId)
        {
            if (string.IsNullOrWhiteSpace(paymentId))
            {
                throw new ArgumentException("Mercado Pago no informó el identificador del pago.", nameof(paymentId));
            }

            EstadoContratacionPersistido contratacion = _contratacionMPP.ConsultarEstado(referenciaContratacion);

            if (string.Equals(contratacion.EstadoContratacion, "Aprobada", StringComparison.OrdinalIgnoreCase))
            {
                return CrearRespuestaEstado(contratacion);
            }

            MercadoPagoPayment payment = await _mercadoPagoService.ConsultarPago(paymentId);

            if (!string.Equals(payment.ExternalReference, referenciaContratacion, StringComparison.Ordinal))
            {
                throw new InvalidOperationException("El pago no corresponde a la contratación indicada.");
            }

            if (payment.TransactionAmount != contratacion.Importe ||
                !string.Equals(payment.CurrencyId, contratacion.Moneda, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("El importe o la moneda informados por Mercado Pago no coinciden con la contratación.");
            }

            EstadoContratacionPersistido resultado = _contratacionMPP.ActualizarResultadoPago(
                referenciaContratacion,
                payment.Id,
                payment.Status,
                $"Mercado Pago informó el estado '{payment.Status}'.");

            return CrearRespuestaEstado(resultado);
        }

        private static EstadoContratacionResponse CrearRespuestaEstado(EstadoContratacionPersistido contratacion)
        {
            return new EstadoContratacionResponse
            {
                Referencia = contratacion.ReferenciaContratacion,
                Estado = contratacion.EstadoContratacion,
                PuedeRegistrar = string.Equals(contratacion.EstadoContratacion, "Aprobada", StringComparison.OrdinalIgnoreCase),
            };
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

            if (request.Periodicidad is not ("Mensual" or "Anual"))
            {
                throw new ArgumentException("Seleccioná una periodicidad mensual o anual.");
            }
        }
    }
}
