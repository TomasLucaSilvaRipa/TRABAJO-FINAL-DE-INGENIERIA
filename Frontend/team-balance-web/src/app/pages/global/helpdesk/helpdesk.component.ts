import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { finalize } from 'rxjs';
import { AuthService } from '../../../services/auth.service';
import { BandejaSoporteItem, DetalleTicket, EstadoTicket, HelpdeskService, OrigenBandejaSoporte, TicketSoporte } from '../../../services/helpdesk.service';
import { LocalizationService } from '../../../services/localization.service';

@Component({ selector: 'app-helpdesk', imports: [CommonModule, ReactiveFormsModule], templateUrl: './helpdesk.component.html', changeDetection: ChangeDetectionStrategy.OnPush })
export class HelpdeskComponent {
  private readonly service = inject(HelpdeskService); private readonly fb = inject(FormBuilder);
  readonly auth = inject(AuthService); readonly localization = inject(LocalizationService);
  readonly tickets = signal<TicketSoporte[]>([]); readonly bandeja = signal<BandejaSoporteItem[]>([]); readonly detalle = signal<DetalleTicket | null>(null); readonly consultaPlanSeleccionada = signal<BandejaSoporteItem | null>(null);
  readonly cargando = signal(true); readonly error = signal(''); readonly mensaje = signal(''); readonly enviando = signal(false); readonly actualizandoEstado = signal(false);
  readonly crearForm = this.fb.nonNullable.group({ categoria: ['Funcionalidad', Validators.required], asunto: ['', [Validators.required, Validators.maxLength(200)]], descripcion: ['', [Validators.required, Validators.maxLength(4000)]] });
  readonly mensajeForm = this.fb.nonNullable.group({ mensaje: ['', [Validators.required, Validators.maxLength(4000)]] });
  readonly categorias = ['Acceso', 'Usuarios', 'Funcionalidad', 'Suscripción y pagos', 'Otro']; readonly estados: EstadoTicket[] = ['Pendiente', 'En revisión', 'Respondida', 'Resuelta'];
  constructor() { this.recargar(); }
  t(key: string): string { return this.localization.traducir(key); }
  etiquetaEstado(estado: EstadoTicket): string { return this.t(`helpdesk.status.${estado}`); }
  etiquetaCategoria(categoria: string): string { return this.t(`helpdesk.category.${categoria}`); }
  esConsultaPlan(item: BandejaSoporteItem): boolean { return item.origen === OrigenBandejaSoporte.ConsultaPlan; }
  etiquetaOrigen(item: BandejaSoporteItem): string { return this.t(this.esConsultaPlan(item) ? 'helpdesk.origin.plan' : 'helpdesk.origin.ticket'); }
  estaSeleccionado(item: BandejaSoporteItem): boolean {
    if (this.esConsultaPlan(item)) {
      const consultaPlan = this.consultaPlanSeleccionada();
      return consultaPlan?.origen === item.origen && consultaPlan.id === item.id;
    }
    return this.detalle()?.consulta.id === item.id;
  }
  recargar(): void {
    this.cargando.set(true); this.error.set('');
    if (this.auth.esSoporte()) {
      this.service.bandeja().pipe(finalize(() => this.cargando.set(false))).subscribe({ next: items => this.bandeja.set(items), error: e => this.error.set(e?.error ?? this.t('helpdesk.loadError')) });
      return;
    }
    this.service.misConsultas().pipe(finalize(() => this.cargando.set(false))).subscribe({ next: items => this.tickets.set(items), error: e => this.error.set(e?.error ?? this.t('helpdesk.loadError')) });
  }
  crear(): void {
    this.crearForm.markAllAsTouched(); if (this.crearForm.invalid) return; const v = this.crearForm.getRawValue(); this.enviando.set(true);
    this.service.crear(v.categoria, v.asunto, v.descripcion).pipe(finalize(() => this.enviando.set(false))).subscribe({ next: t => { this.mensaje.set(this.t('helpdesk.created')); this.crearForm.reset({ categoria: 'Funcionalidad', asunto: '', descripcion: '' }); this.tickets.update(x => [t, ...x]); this.abrir(t.id); }, error: e => this.error.set(e?.error ?? this.t('helpdesk.createError')) });
  }
  abrir(id: number): void { this.consultaPlanSeleccionada.set(null); this.detalle.set(null); this.service.detalle(id).subscribe({ next: d => this.detalle.set(d), error: e => this.error.set(e?.error ?? this.t('helpdesk.detailError')) }); }
  abrirElemento(item: BandejaSoporteItem): void { if (this.esConsultaPlan(item)) { this.detalle.set(null); this.consultaPlanSeleccionada.set(item); return; } this.abrir(item.id); }
  enviar(): void {
    const d = this.detalle(); const consultaPlan = this.consultaPlanSeleccionada(); this.mensajeForm.markAllAsTouched(); if ((!d && !consultaPlan) || this.mensajeForm.invalid) return; const mensaje = this.mensajeForm.controls.mensaje.value; this.enviando.set(true);
    if (consultaPlan) {
      this.service.responderConsultaPlan(consultaPlan.id, mensaje).pipe(finalize(() => this.enviando.set(false))).subscribe({ next: resultado => { this.consultaPlanSeleccionada.set(resultado); this.mensajeForm.reset({ mensaje: '' }); this.mensaje.set(this.t('helpdesk.planAnswered')); this.recargar(); }, error: e => this.error.set(e?.error ?? this.t('helpdesk.sendError')) });
      return;
    }
    if (!d) return;
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
