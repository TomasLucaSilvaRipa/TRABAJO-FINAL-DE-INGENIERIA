import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { RouterLink } from '@angular/router';
import { AuthService } from '../../../services/auth.service';

@Component({
  selector: 'app-subscription-expired',
  imports: [RouterLink],
  templateUrl: './subscription-expired.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class SubscriptionExpiredComponent {
  readonly esDueno = inject(AuthService).tienePermiso('GestionarSuscripcion');
}
