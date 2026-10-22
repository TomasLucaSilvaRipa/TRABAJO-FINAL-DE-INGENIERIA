import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { LocalizationService } from '../../../services/localization.service';
import { Notificacion, NotificacionesService } from '../../../services/notificaciones.service';

@Component({ selector: 'app-notifications', imports: [], templateUrl: './notifications.component.html', changeDetection: ChangeDetectionStrategy.OnPush })
export class NotificationsComponent {
  private readonly service = inject(NotificacionesService);
  readonly localization = inject(LocalizationService);
  readonly notificaciones = signal<Notificacion[]>([]);
  constructor() { this.actualizar(); }
  actualizar(): void { this.service.consultar().subscribe({ next: items => this.notificaciones.set(items) }); }
  marcarLeida(item: Notificacion): void { if (!item.leida) this.service.marcarLeida(item.id).subscribe({ next: () => this.notificaciones.update(items => items.map(actual => actual.id === item.id ? { ...actual, leida: true } : actual)) }); }
  marcarTodas(): void { this.service.marcarTodasLeidas().subscribe({ next: () => this.notificaciones.update(items => items.map(item => ({ ...item, leida: true }))) }); }
  fecha(valor: string): string { return new Intl.DateTimeFormat(this.localization.language() === 'en' ? 'en-US' : 'es-AR', { dateStyle: 'medium', timeStyle: 'short' }).format(new Date(valor)); }
}
