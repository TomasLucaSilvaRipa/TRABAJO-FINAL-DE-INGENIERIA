import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { RouterModule } from '@angular/router';
import { AgencyService, Suscripcion } from '../../../../services/agency.service';
import { LocalizationService } from '../../../../services/localization.service';

@Component({
  selector: 'app-subscription',
  imports: [CommonModule, RouterModule],
  templateUrl: './subscription-manager.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class Subscription {
  private readonly agencyService = inject(AgencyService);
  readonly localization = inject(LocalizationService);
  readonly suscripcion = signal<Suscripcion | null>(null);
  readonly cargando = signal(true);
  readonly error = signal('');

  constructor() { this.agencyService.consultarSuscripcion().subscribe({ next: (suscripcion: Suscripcion | null) => { this.suscripcion.set(suscripcion); this.cargando.set(false); }, error: () => { this.error.set('No se pudo consultar la suscripción de la agencia.'); this.cargando.set(false); } }); }
}
