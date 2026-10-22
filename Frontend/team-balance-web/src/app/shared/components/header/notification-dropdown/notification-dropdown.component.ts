import { CommonModule } from '@angular/common';
import { Component, HostListener, inject, OnInit, signal } from '@angular/core';
import { RouterModule } from '@angular/router';
import { LocalizationService } from '../../../../services/localization.service';
import { Notificacion, NotificacionesService } from '../../../../services/notificaciones.service';

@Component({ selector: 'app-notification-dropdown', templateUrl: './notification-dropdown.component.html', imports: [CommonModule, RouterModule] })
export class NotificationDropdownComponent implements OnInit {
  private readonly notificacionesService = inject(NotificacionesService);
  readonly localization = inject(LocalizationService);
  readonly notificaciones = signal<Notificacion[]>([]);
  readonly abierto = signal(false);
  readonly cerrando = signal(false);

  ngOnInit(): void { this.actualizar(); }
  get sinLeer(): number { return this.notificaciones().filter(item => !item.leida).length; }
  get recientes(): Notificacion[] { return this.notificaciones().slice(0, 5); }
  alternar(): void { this.abierto() ? this.cerrar() : (this.abierto.set(true), this.cerrando.set(false), this.actualizar()); }
  cerrar(): void { if (!this.abierto() || this.cerrando()) return; this.cerrando.set(true); window.setTimeout(() => { this.abierto.set(false); this.cerrando.set(false); }, 180); }
  actualizar(): void { this.notificacionesService.consultar().subscribe({ next: items => this.notificaciones.set(items), error: () => this.notificaciones.set([]) }); }
  marcarLeida(item: Notificacion): void { if (item.leida) return; this.notificacionesService.marcarLeida(item.id).subscribe({ next: () => this.notificaciones.update(items => items.map(actual => actual.id === item.id ? { ...actual, leida: true } : actual)) }); }
  marcarTodas(): void { this.notificacionesService.marcarTodasLeidas().subscribe({ next: () => this.notificaciones.update(items => items.map(item => ({ ...item, leida: true }))) }); }
  fecha(fecha: string): string { return new Intl.DateTimeFormat(this.localization.language() === 'en' ? 'en-US' : 'es-AR', { dateStyle: 'short', timeStyle: 'short' }).format(new Date(fecha)); }
  @HostListener('document:mousedown', ['$event']) alClickAfuera(evento: MouseEvent): void { const objetivo = evento.target as HTMLElement; if (this.abierto() && !objetivo.closest('.tb-notifications')) this.cerrar(); }
}
