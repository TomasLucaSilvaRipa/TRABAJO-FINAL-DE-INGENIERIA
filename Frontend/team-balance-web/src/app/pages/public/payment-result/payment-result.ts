import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import {
  ContratacionService,
  EstadoContratacionResponse,
} from '../../../services/contratacion.service';

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

  readonly loading = signal(true);
  readonly errorMessage = signal('');
  readonly resultado = signal<EstadoContratacionResponse | null>(null);

  constructor() {
    this.route.queryParamMap.subscribe((params) => {
      const referencia = params.get('external_reference');
      const paymentId = params.get('payment_id');

      if (!referencia) {
        this.loading.set(false);
        this.errorMessage.set('No recibimos la referencia de tu contratación. Volvé a iniciar el proceso desde los planes.');
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
