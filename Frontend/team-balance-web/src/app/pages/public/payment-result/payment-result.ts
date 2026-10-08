import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import {
  ContratacionService,
  EstadoContratacionResponse,
} from '../../../services/contratacion.service';
import { ResultadoGestionSuscripcion, SuscripcionService } from '../../../services/suscripcion.service';

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

  readonly loading = signal(true);
  readonly errorMessage = signal('');
  readonly resultado = signal<EstadoContratacionResponse | null>(null);
  readonly resultadoSuscripcion = signal<ResultadoGestionSuscripcion | null>(null);

  constructor() {
    this.route.queryParamMap.subscribe((params) => {
      const referencia = params.get('external_reference');
      // Mercado Pago puede devolver `payment_id` o `collection_id` según la
      // modalidad/version del Checkout Pro. Ambos identifican el mismo pago
      // para la verificación del lado del servidor.
      const paymentId = params.get('payment_id') ?? params.get('collection_id');

      if (!referencia) {
        this.loading.set(false);
        this.errorMessage.set('No recibimos la referencia de tu contratación. Volvé a iniciar el proceso desde los planes.');
        return;
      }

      if (referencia.startsWith('sus-')) {
        if (!paymentId) {
          this.loading.set(false);
          this.errorMessage.set('La actualización quedó pendiente de confirmación de Mercado Pago. Volvé a la suscripción para consultar el estado.');
          return;
        }
        this.suscripcionService.verificarActualizacion(referencia, paymentId).subscribe({
          next: resultado => { this.resultadoSuscripcion.set(resultado); this.loading.set(false); },
          error: error => {
            this.loading.set(false);
            this.errorMessage.set(error?.error?.message ?? error?.error ?? 'No pudimos verificar la actualización de suscripción en este momento.');
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
          this.errorMessage.set('No pudimos verificar el estado del pago en este momento. Volvé a intentarlo en unos minutos.');
        },
      });
    });
  }
}
