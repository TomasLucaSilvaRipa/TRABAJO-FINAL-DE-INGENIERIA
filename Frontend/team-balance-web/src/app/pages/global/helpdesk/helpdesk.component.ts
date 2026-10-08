import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { finalize } from 'rxjs';
import { AuthService } from '../../../services/auth.service';
import { DetalleTicket, EstadoTicket, HelpdeskService, TicketSoporte } from '../../../services/helpdesk.service';
import { LocalizationService } from '../../../services/localization.service';

@Component({ selector: 'app-helpdesk', imports: [CommonModule, ReactiveFormsModule], templateUrl: './helpdesk.component.html', changeDetection: ChangeDetectionStrategy.OnPush })
export class HelpdeskComponent {
  private readonly service = inject(HelpdeskService); private readonly fb = inject(FormBuilder);
  readonly auth = inject(AuthService); readonly localization = inject(LocalizationService);
  readonly tickets = signal<TicketSoporte[]>([]); readonly bandeja = signal<TicketSoporte[]>([]); readonly detalle = signal<DetalleTicket | null>(null);
  readonly cargando = signal(true); readonly error = signal(''); readonly mensaje = signal(''); readonly enviando = signal(false); readonly actualizandoEstado = signal(false);
  readonly crearForm = this.fb.nonNullable.group({ categoria: ['Funcionalidad', Validators.required], asunto: ['', [Validators.required, Validators.maxLength(200)]], descripcion: ['', [Validators.required, Validators.maxLength(4000)]] });
  readonly mensajeForm = this.fb.nonNullable.group({ mensaje: ['', [Validators.required, Validators.maxLength(4000)]] });
  readonly categorias = ['Acceso', 'Usuarios', 'Funcionalidad', 'Suscripción y pagos', 'Otro']; readonly estados: EstadoTicket[] = ['Pendiente', 'En revisión', 'Respondida', 'Resuelta'];
  constructor() { this.recargar(); }
  t(key: string): string { return this.localization.traducir(key); }
  etiquetaEstado(estado: EstadoTicket): string { return this.t(`helpdesk.status.${estado}`); }
  etiquetaCategoria(categoria: string): string { return this.t(`helpdesk.category.${categoria}`); }
  recargar(): void {
    this.cargando.set(true); this.error.set('');
    const consulta = this.auth.esSoporte() ? this.service.bandeja() : this.service.misConsultas();
    consulta.pipe(finalize(() => this.cargando.set(false))).subscribe({ next: items => { this.tickets.set(this.auth.esSoporte() ? [] : items); this.bandeja.set(this.auth.esSoporte() ? items : []); }, error: e => this.error.set(e?.error ?? this.t('helpdesk.loadError')) });
  }
  crear(): void {
    this.crearForm.markAllAsTouched(); if (this.crearForm.invalid) return; const v = this.crearForm.getRawValue(); this.enviando.set(true);
    this.service.crear(v.categoria, v.asunto, v.descripcion).pipe(finalize(() => this.enviando.set(false))).subscribe({ next: t => { this.mensaje.set(this.t('helpdesk.created')); this.crearForm.reset({ categoria: 'Funcionalidad', asunto: '', descripcion: '' }); this.tickets.update(x => [t, ...x]); this.abrir(t.id); }, error: e => this.error.set(e?.error ?? this.t('helpdesk.createError')) });
  }
  abrir(id: number): void { this.detalle.set(null); this.service.detalle(id).subscribe({ next: d => this.detalle.set(d), error: e => this.error.set(e?.error ?? this.t('helpdesk.detailError')) }); }
  enviar(): void {
    const d = this.detalle(); this.mensajeForm.markAllAsTouched(); if (!d || this.mensajeForm.invalid) return; const mensaje = this.mensajeForm.controls.mensaje.value; this.enviando.set(true);
    this.service.enviarMensaje(d.consulta.id, mensaje).pipe(finalize(() => this.enviando.set(false))).subscribe({ next: () => { this.mensajeForm.reset({ mensaje: '' }); this.abrir(d.consulta.id); this.recargar(); }, error: e => this.error.set(e?.error ?? this.t('helpdesk.sendError')) });
  }
  cambiarEstado(estado: string): void {
    const d = this.detalle(); if (!d) return; this.actualizandoEstado.set(true);
    this.service.cambiarEstado(d.consulta.id, estado as EstadoTicket).pipe(finalize(() => this.actualizandoEstado.set(false))).subscribe({ next: () => { this.mensaje.set(this.t('helpdesk.statusSaved')); this.abrir(d.consulta.id); this.recargar(); }, error: e => this.error.set(e?.error ?? this.t('helpdesk.statusError')) });
  }
  resolver(): void {
    const d = this.detalle(); if (!d) return; this.actualizandoEstado.set(true);
    this.service.resolver(d.consulta.id).pipe(finalize(() => this.actualizandoEstado.set(false))).subscribe({ next: () => { this.mensaje.set(this.t('helpdesk.resolved')); this.abrir(d.consulta.id); this.recargar(); }, error: e => this.error.set(e?.error ?? this.t('helpdesk.statusError')) });
  }
}
