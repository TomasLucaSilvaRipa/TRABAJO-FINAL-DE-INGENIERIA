import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import {
  ContratacionService,
  EstadoContratacionResponse,
} from '../../../services/contratacion.service';
import { ResultadoGestionSuscripcion, SuscripcionService } from '../../../services/suscripcion.service';
import { AuthService } from '../../../services/auth.service';
import { LocalizationService } from '../../../services/localization.service';

@Component({
  selector: 'app-payment-result',
  imports: [RouterLink],
  templateUrl: './payment-result.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class PaymentResultComponent {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly contratacionService = inject(ContratacionService);
  private readonly suscripcionService = inject(SuscripcionService);
  private readonly authService = inject(AuthService);
  readonly localization = inject(LocalizationService);

  readonly loading = signal(true);
  readonly errorMessage = signal('');
  readonly resultado = signal<EstadoContratacionResponse | null>(null);
  readonly resultadoSuscripcion = signal<ResultadoGestionSuscripcion | null>(null);
  readonly esGestionSuscripcion = signal(false);
  readonly referenciaContinuar = signal('');
  readonly puedeRevisarRenovacion = signal(false);
  readonly esRenovacion = signal(false);
  readonly errorRenovacion = signal('');
  readonly referenciaAlta = signal('');
  readonly errorAlta = signal(false);

  constructor() {
    this.route.queryParamMap.subscribe((params) => {
      const leerParametro = (nombre: string): string => {
        const valor = params.get(nombre)?.trim();
        return valor && valor !== 'null' && valor !== 'undefined' ? valor : '';
      };
      const referencia = leerParametro('external_reference');
      const referenciaLegadaMp = /^ren-\d+-[a-f\d]{32}$/i.test(referencia) ? referencia : '';
      const referenciaMp = leerParametro('referencia_renovacion') || referenciaLegadaMp;
      const preapprovalId = leerParametro('preapproval_id')
        || (referenciaMp ? sessionStorage.getItem(`renovacion-mercadopago:${referenciaMp}`) ?? '' : '');
      const sessionId = leerParametro('session_id');
      // Mercado Pago devuelve payment_id o collection_id; Stripe devuelve
      // session_id. El backend identifica el proveedor de la operación.
      const paymentId = leerParametro('payment_id') || leerParametro('collection_id') || sessionId;

      if (/^alta-[a-f0-9]{32}(?:-p[1-9]\d*)?$/.test(referencia)) {
        this.referenciaAlta.set(referencia.replace(/-p\d+$/, ''));
        if (paymentId && referencia !== this.referenciaAlta()) {
          this.contratacionService.verificarPago(referencia, paymentId).subscribe({
            next: resultado => this.recibirResultadoAlta(resultado),
            error: () => this.falloPagoAlta(),
          });
        } else if (params.get('iniciar') === '1') {
          this.continuarPagoAlta();
        } else {
          this.contratacionService.consultarEstado(this.referenciaAlta()).subscribe({
            next: resultado => this.recibirResultadoAlta(resultado),
            error: () => this.falloPagoAlta(),
          });
        }
        return;
      }

      if (params.get('renovacion') === 'mercadopago' || referenciaMp || preapprovalId || referencia.startsWith('ren-')) {
        this.esGestionSuscripcion.set(true);
        this.esRenovacion.set(true);
        const esMercadoPago = Boolean(referenciaMp || preapprovalId || params.get('renovacion') === 'mercadopago');
        const puedeVerificar = esMercadoPago ? Boolean(referenciaMp || preapprovalId) : Boolean(sessionId);
        this.puedeRevisarRenovacion.set(puedeVerificar);
        if (!puedeVerificar) {
          this.loading.set(false);
          this.errorRenovacion.set('renewalMissing');
          return;
        }

        const verificacion = esMercadoPago
          ? this.suscripcionService.verificarRenovacionMercadoPago(referenciaMp, preapprovalId)
          : this.suscripcionService.verificarRenovacionStripe(sessionId);
        verificacion.subscribe({
          next: resultado => {
            if (referenciaMp) sessionStorage.removeItem(`renovacion-mercadopago:${referenciaMp}`);
            this.resultadoSuscripcion.set(resultado);
            this.loading.set(false);
          },
          error: ({ status, error }: { status: number; error: unknown }) => {
            this.loading.set(false);
            const pendiente = error === 'Stripe todavía no confirmó la autorización de la renovación automática.'
              ? 'renewalPendingStripe'
              : error === 'Mercado Pago todavía no confirmó la autorización de la renovación automática.' ? 'renewalPendingMp' : '';
            this.errorRenovacion.set(pendiente || (status === 502 && !esMercadoPago ? 'renewalUnavailable' : 'renewalCheckUnavailable'));
          },
        });
        return;
      }

      if (!referencia) {
        this.loading.set(false);
        this.errorMessage.set('missingReference');
        return;
      }

      if (referencia.startsWith('sus-') || referencia.startsWith('cred-')) {
        this.esGestionSuscripcion.set(true);
        if (/^sus-[a-f0-9]+-p\d+$/.test(referencia)) this.referenciaContinuar.set(referencia);
        if (!paymentId) {
          this.loading.set(false);
          this.errorMessage.set('incomplete');
          return;
        }
        this.suscripcionService.verificarActualizacion(referencia, paymentId).subscribe({
          next: resultado => this.recibirResultadoSuscripcion(resultado),
          error: () => {
            this.loading.set(false);
            this.errorMessage.set('subscriptionCheckFailed');
          },
        });
        return;
      }

      const solicitud = paymentId ? this.contratacionService.verificarPago(referencia, paymentId) : this.contratacionService.consultarEstado(referencia);
      solicitud.subscribe({
        next: (resultado) => {
          this.resultado.set(resultado);
          this.loading.set(false);

          if (resultado.puedeRegistrar) {
            void this.router.navigate(['/registrar-agencia'], {
              queryParams: { referencia: resultado.referencia },
              replaceUrl: true,
            });
          }
        },
        error: () => {
          this.loading.set(false);
          this.errorMessage.set('contractCheckFailed');
        },
      });
    });
  }

  continuarPago(): void {
    this.loading.set(true);
    this.errorMessage.set('');
    this.suscripcionService.continuarActualizacion(this.referenciaContinuar()).subscribe({
      next: resultado => this.recibirResultadoSuscripcion(resultado),
      error: () => { this.loading.set(false); this.errorMessage.set('resumeFailed'); },
    });
  }

  verificarDeNuevo(): void { window.location.reload(); }

  continuarPagoAlta(): void {
    this.loading.set(true);
    this.errorAlta.set(false);
    this.contratacionService.continuarPago(this.referenciaAlta()).subscribe({
      next: resultado => this.recibirResultadoAlta(resultado),
      error: () => this.falloPagoAlta(),
    });
  }

  private falloPagoAlta(): void {
    this.errorAlta.set(true);
    this.contratacionService.consultarEstado(this.referenciaAlta()).subscribe({
      next: resultado => this.recibirResultadoAlta(resultado),
      error: () => this.loading.set(false),
    });
  }

  private recibirResultadoAlta(resultado: EstadoContratacionResponse): void {
    this.resultado.set(resultado);
    this.loading.set(false);
    if (resultado.urlPagoSiguiente) {
      window.location.assign(resultado.urlPagoSiguiente);
      return;
    }
    if (resultado.puedeRegistrar) {
      void this.router.navigate(['/registrar-agencia'], {
        queryParams: { referencia: resultado.referencia },
        replaceUrl: true,
      });
    }
  }

  t(key: string): string {
    return this.localization.traducir(`private.subscription.${key}`);
  }

  paymentText(key: string): string {
    return this.localization.traducir(`paymentResult.${key}`);
  }

  subscriptionPaymentText(resultado: ResultadoGestionSuscripcion): string {
    if (resultado.urlPagoSiguiente) return this.paymentText('nextPayment');
    const estado = resultado.estado.toLowerCase();
    if (resultado.pagoConfirmado || estado === 'approved' || estado === 'aprobada') return this.paymentText('subscriptionConfirmed');
    if (['rejected', 'cancelled', 'rechazada'].includes(estado)) return this.paymentText('subscriptionRejected');
    return this.t('pendingParts');
  }

  private recibirResultadoSuscripcion(resultado: ResultadoGestionSuscripcion): void {
    if (resultado.pagoConfirmado) this.authService.establecerSuscripcionRestringida(false);
    if (resultado.referenciaOperacion && !resultado.pagoConfirmado) this.referenciaContinuar.set(resultado.referenciaOperacion);
    if (resultado.pagoConfirmado) this.referenciaContinuar.set('');
    this.resultadoSuscripcion.set(resultado);
    this.loading.set(false);
    if (resultado.urlPagoSiguiente) window.location.assign(resultado.urlPagoSiguiente);
  }
}
