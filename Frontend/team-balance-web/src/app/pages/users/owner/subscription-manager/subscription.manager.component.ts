import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { RouterModule } from '@angular/router';
import { finalize, forkJoin } from 'rxjs';
import { PlanComercial } from '../../../../services/plan-comercial.service';
import { ConfiguracionRenovacionAutomatica, DocumentoComercial, OperacionSuscripcion, Suscripcion, SuscripcionService } from '../../../../services/suscripcion.service';
import { LocalizationService } from '../../../../services/localization.service';
import { AuthService } from '../../../../services/auth.service';

@Component({
  selector: 'app-subscription',
  imports: [CommonModule, RouterModule, ReactiveFormsModule],
  templateUrl: './subscription-manager.component.html',
  styleUrl: './subscription-manager.component.css',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class Subscription {
  private readonly suscripcionService = inject(SuscripcionService);
  private readonly formBuilder = inject(FormBuilder);
  private readonly authService = inject(AuthService);
  readonly localization = inject(LocalizationService);
  readonly suscripcion = signal<Suscripcion | null>(null);
  readonly cargando = signal(true);
  readonly error = signal('');
  readonly mensaje = signal('');
  readonly planes = signal<PlanComercial[]>([]);
  readonly historial = signal<OperacionSuscripcion[]>([]);
  readonly documentos = signal<DocumentoComercial[]>([]);
  readonly mostrandoHistorial = signal(false);
  readonly mostrandoActualizacion = signal(false);
  readonly procesando = signal(false);
  readonly cancelando = signal(false);
  readonly cancelandoServicio = signal(false);
  readonly regularizandoConCredito = signal(false);
  readonly mostrandoAutorizacion = signal(false);
  readonly cargandoFormularioMp = signal(false);
  readonly configuracionRenovacion = signal<ConfiguracionRenovacionAutomatica | null>(null);
  readonly cambioForm = this.formBuilder.nonNullable.group({ idPlanComercial: [0, [Validators.min(1)] ], proveedor: ['MercadoPago', [Validators.required]] });
  readonly renovacionMercadoPagoForm = this.formBuilder.nonNullable.group({ payerEmail: ['', [Validators.required, Validators.email]] });
  readonly bajaForm = this.formBuilder.nonNullable.group({ motivo: ['', [Validators.required, Validators.maxLength(1000)]] });
  readonly cancelacionServicioForm = this.formBuilder.nonNullable.group({ motivo: ['', [Validators.required, Validators.maxLength(1000)]] });
  constructor() { this.recargar(); }

  recargar(): void {
    this.cargando.set(true); this.error.set('');
    forkJoin({ suscripcion: this.suscripcionService.consultarActual(), planes: this.suscripcionService.consultarPlanes(), historial: this.suscripcionService.consultarHistorial(), documentos: this.suscripcionService.consultarCuentaCorriente() })
      .pipe(finalize(() => this.cargando.set(false)))
      .subscribe({ next: ({ suscripcion, planes, historial, documentos }) => { this.suscripcion.set(suscripcion); this.planes.set(planes); this.historial.set(historial); this.documentos.set(documentos); }, error: error => this.error.set(error?.error?.message ?? error?.error ?? 'No se pudo consultar la suscripción de la agencia.') });
  }

  abrirActualizacion(): void { this.mostrandoActualizacion.set(true); this.mensaje.set(''); }
  cerrarActualizacion(): void { this.mostrandoActualizacion.set(false); this.cambioForm.reset({ idPlanComercial: 0, proveedor: 'MercadoPago' }); }
  alternarHistorial(): void { this.mostrandoHistorial.update(valor => !valor); }

  actualizar(): void {
    this.cambioForm.markAllAsTouched(); if (this.cambioForm.invalid) return;
    this.procesando.set(true); this.error.set(''); this.mensaje.set('');
    const cambio = this.cambioForm.getRawValue();
    this.suscripcionService.solicitarActualizacion(cambio.idPlanComercial, cambio.proveedor).pipe(finalize(() => this.procesando.set(false))).subscribe({ next: respuesta => window.location.assign(respuesta.urlPago), error: error => this.error.set(error?.error?.message ?? error?.error ?? 'No se pudo solicitar la actualización al proveedor.') });
  }

  cancelarRenovacion(): void {
    this.bajaForm.markAllAsTouched(); if (this.bajaForm.invalid) return;
    this.procesando.set(true); this.error.set('');
    this.suscripcionService.cancelarRenovacion(this.bajaForm.getRawValue().motivo).pipe(finalize(() => this.procesando.set(false))).subscribe({ next: resultado => { this.suscripcion.set(resultado.suscripcion ?? null); this.mensaje.set(resultado.mensaje); this.cancelando.set(false); this.bajaForm.reset({ motivo: '' }); this.recargar(); }, error: error => this.error.set(error?.error?.message ?? error?.error ?? 'No se pudo cancelar la renovación automática.') });
  }

  cancelarServicio(): void {
    this.cancelacionServicioForm.markAllAsTouched();
    if (this.cancelacionServicioForm.invalid || !this.suscripcion()) return;
    this.procesando.set(true); this.error.set(''); this.mensaje.set('');
    const suscripcionActual = this.suscripcion()!;
    const documento: DocumentoComercial = { id: 0, idAgencia: suscripcionActual.idAgencia, idSuscripcion: suscripcionActual.id, tipo: '', numero: '', concepto: '', importe: 0, saldoPendiente: 0, moneda: suscripcionActual.moneda || 'ARS', estado: '', fechaEmision: new Date().toISOString(), motivo: this.cancelacionServicioForm.getRawValue().motivo };
    this.suscripcionService.cancelarServicio(documento).pipe(finalize(() => this.procesando.set(false))).subscribe({ next: notaCredito => { this.authService.establecerSuscripcionRestringida(true); this.mensaje.set(`Servicio cancelado. Se emitió ${notaCredito.numero} por ${this.localization.formatearMoneda(notaCredito.importe, notaCredito.moneda)} como crédito interno de la agencia.`); this.cancelandoServicio.set(false); this.cancelacionServicioForm.reset({ motivo: '' }); this.recargar(); }, error: error => this.error.set(this.obtenerMensajeError(error, 'No se pudo cancelar el servicio.')) });
  }

  abrirReactivacionConCredito(): void {
    this.cambioForm.reset({ idPlanComercial: 0, proveedor: 'MercadoPago' });
    this.regularizandoConCredito.set(true);
  }

  cancelarReactivacionConCredito(): void {
    this.cambioForm.reset({ idPlanComercial: 0, proveedor: 'MercadoPago' });
    this.regularizandoConCredito.set(false);
  }

  reactivarConNotaCredito(): void {
    this.cambioForm.markAllAsTouched();
    if (this.cambioForm.invalid || !this.suscripcion()) return;
    this.procesando.set(true); this.error.set(''); this.mensaje.set('');
    const suscripcionActual = this.suscripcion()!;
    const documento: DocumentoComercial = { id: 0, idAgencia: suscripcionActual.idAgencia, idSuscripcion: suscripcionActual.id, idPlanComercial: this.cambioForm.getRawValue().idPlanComercial, tipo: '', numero: '', concepto: '', importe: 0, saldoPendiente: 0, moneda: suscripcionActual.moneda || 'ARS', estado: '', fechaEmision: new Date().toISOString() };
    const plan = this.planes().find(item => item.id === documento.idPlanComercial);
    const creditoMismaMoneda = plan ? this.creditoDisponible(plan) : 0;
    if (plan && creditoMismaMoneda < plan.precioVigente) {
      const solicitud = { idPlanComercial: plan.id, proveedor: this.cambioForm.getRawValue().proveedor as 'MercadoPago' | 'Stripe' };
      this.suscripcionService.solicitarReactivacionConCreditoParcial(solicitud).pipe(finalize(() => this.procesando.set(false))).subscribe({ next: respuesta => window.location.assign(respuesta.urlPago), error: error => this.error.set(this.obtenerMensajeError(error, 'No se pudo preparar el pago de la diferencia.')) });
      return;
    }
    this.suscripcionService.reactivarConNotaCredito(documento).pipe(finalize(() => this.procesando.set(false))).subscribe({ next: factura => { this.authService.establecerSuscripcionRestringida(false); this.mensaje.set(`Servicio reactivado. La factura ${factura.numero} fue cubierta con el saldo a favor disponible.`); this.cancelarReactivacionConCredito(); this.recargar(); }, error: error => this.error.set(this.obtenerMensajeError(error, 'No se pudo aplicar el saldo a favor.')) });
  }

  planReactivacionSeleccionado(): PlanComercial | undefined { return this.planes().find(plan => plan.id === this.cambioForm.controls.idPlanComercial.value); }

  creditoDisponible(plan: PlanComercial): number {
    return this.documentos().filter(documento => documento.tipo === 'NotaCredito' && documento.estado === 'Disponible' && documento.moneda === plan.moneda).reduce((total, documento) => total + documento.saldoPendiente, 0);
  }

  requierePagoDiferencia(): boolean {
    const plan = this.planReactivacionSeleccionado();
    return !!plan && this.creditoDisponible(plan) < plan.precioVigente;
  }

  totalSaldoAFavor(): number { return this.documentos().filter((documento: DocumentoComercial) => documento.tipo === 'NotaCredito' && documento.estado === 'Disponible').reduce((total: number, documento: DocumentoComercial) => total + documento.saldoPendiente, 0); }

  reactivarRenovacion(): void {
    this.procesando.set(true); this.error.set('');
    this.suscripcionService.reactivarRenovacion().pipe(finalize(() => this.procesando.set(false))).subscribe({ next: resultado => { this.suscripcion.set(resultado.suscripcion ?? null); this.mensaje.set(resultado.mensaje); this.recargar(); }, error: error => this.error.set(error?.error?.message ?? error?.error ?? 'No se pudo reactivar la renovación automática.') });
  }

  abrirAutorizacionRenovacion(): void {
    this.error.set(''); this.mensaje.set(''); this.cargandoFormularioMp.set(true);
    this.suscripcionService.consultarConfiguracionRenovacion().pipe(finalize(() => this.cargandoFormularioMp.set(false))).subscribe({
      next: configuracion => {
        this.configuracionRenovacion.set(configuracion);
        this.mostrandoAutorizacion.set(true);
      },
      error: error => this.error.set(error?.error?.message ?? error?.error ?? 'No se pudo preparar el formulario seguro de Mercado Pago.')
    });
  }

  solicitarRenovacionStripe(): void {
    this.procesando.set(true); this.error.set(''); this.mensaje.set('');
    this.suscripcionService.solicitarRenovacionStripe().pipe(finalize(() => this.procesando.set(false))).subscribe({ next: respuesta => window.location.assign(respuesta.urlPago), error: error => this.error.set(error?.error?.message ?? error?.error ?? 'No se pudo preparar la renovación automática con Stripe.') });
  }

  cerrarAutorizacionRenovacion(): void {
    this.mostrandoAutorizacion.set(false); this.configuracionRenovacion.set(null);
    this.renovacionMercadoPagoForm.reset({ payerEmail: '' });
  }

  confirmarAutorizacionMercadoPago(): void {
    this.renovacionMercadoPagoForm.markAllAsTouched();
    if (this.renovacionMercadoPagoForm.invalid) return;
    this.procesando.set(true); this.error.set('');
    const payerEmail: string = this.renovacionMercadoPagoForm.getRawValue().payerEmail.trim();
    this.suscripcionService.autorizarRenovacion(payerEmail).pipe(finalize(() => this.procesando.set(false))).subscribe({
      next: resultado => window.location.assign(resultado.urlPago),
      error: error => this.error.set(error?.error?.message ?? error?.error ?? 'No se pudo autorizar la renovación automática.')
    });
  }

  private obtenerMensajeError(error: { error?: unknown }, mensajePredeterminado: string): string {
    if (typeof error.error === 'string' && error.error.trim()) { return error.error; }
    if (!error.error || typeof error.error !== 'object') { return mensajePredeterminado; }

    const respuesta = error.error as { message?: unknown; detail?: unknown; title?: unknown; errors?: Record<string, unknown> };
    if (typeof respuesta.message === 'string' && respuesta.message.trim()) { return respuesta.message; }
    if (typeof respuesta.detail === 'string' && respuesta.detail.trim()) { return respuesta.detail; }

    if (respuesta.errors)
    {
      const mensajeValidacion = Object.values(respuesta.errors).flat().find((mensaje: unknown) => typeof mensaje === 'string' && mensaje.trim());
      if (typeof mensajeValidacion === 'string') { return mensajeValidacion; }
    }

    if (typeof respuesta.title === 'string' && respuesta.title.trim()) { return respuesta.title; }
    return mensajePredeterminado;
  }
}
