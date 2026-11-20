import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { RouterModule } from '@angular/router';
import { finalize, forkJoin } from 'rxjs';
import { PlanComercial } from '../../../../services/plan-comercial.service';
import { ConfiguracionRenovacionAutomatica, CotizacionPagoSuscripcion, DistribucionPagoSuscripcion, DocumentoComercial, OperacionSuscripcion, Suscripcion, SuscripcionService } from '../../../../services/suscripcion.service';
import { LocalizationService } from '../../../../services/localization.service';
import { AuthService } from '../../../../services/auth.service';

const subscriptionRecordKeys = new Map([
  ['activa', 'active'], ['activo', 'active'], ['active', 'active'],
  ['inactiva', 'inactive'], ['vencida', 'expired'], ['suspendida', 'suspended'],
  ['cancelada', 'cancelled'], ['cancelled', 'cancelled'], ['canceled', 'cancelled'],
  ['pendiente', 'pending'], ['pending', 'pending'], ['approved', 'approved'], ['aprobada', 'approved'],
  ['aplicada', 'applied'], ['aplicado', 'used'], ['disponible', 'available'], ['pagada', 'paid'],
  ['rechazada', 'rejected'], ['rejected', 'rejected'], ['trialing', 'trialing'],
  ['authorized', 'authorized'], ['paused', 'paused'], ['combinado', 'combined'],
  ['mensual', 'monthly'], ['trimestral', 'quarterly'], ['semestral', 'halfYearly'], ['anual', 'yearly'],
  ['mercadopago', 'mercadoPago'], ['factura', 'invoice'],
  ['notacredito', 'creditNote'], ['nota credito', 'creditNote'], ['nota de credito', 'creditNote'],
  ['notadebito', 'debitNote'], ['nota debito', 'debitNote'], ['nota de debito', 'debitNote'],
  ['actualizacion de suscripcion teambalance', 'documentUpdate'],
  ['reactivacion de suscripcion teambalance con nota de credito', 'documentCreditReactivation'],
  ['credito por cancelacion del servicio teambalance', 'documentCancellationCredit'],
  ['contratacion inicial de teambalance', 'documentInitial'],
  ['actualizacion distribuida', 'splitUpdate'], ['actualizacion de suscripcion', 'planUpdate'],
  ['cancelacion de renovacion automatica', 'cancelAuto'], ['reactivacion de renovacion automatica', 'reactivateAuto'],
  ['cancelacion de servicio', 'cancelOperation'], ['reactivacion con nota de credito', 'reactivateOperation'],
  ['cobro recurrente', 'recurringCharge'],
  ['renovacion automatica', 'renovacionAutomatica'], ['vencimiento', 'expiry'],
  ['actualizacion de modalidad', 'planUpdate'], ['regularizacion con tarjeta', 'regularizarConTarjeta'],
  ['regularizacion con credito parcial', 'creditPartialReactivation'],
  ['actualizacion con credito parcial', 'creditPartialUpdate'],
  ['actualizacion con credito completo', 'creditFullUpdate'],
  ['solicitud de actualizacion de modalidad', 'requestPlanChange'],
  ['renovacion automatica cancelada correctamente.', 'cancelRenewalDone'],
  ['renovacion automatica reactivada correctamente.', 'reactivateRenewalDone'],
  ['suscripcion actualizada correctamente.', 'updateDone'],
  ['todas las partes del pago fueron confirmadas.', 'paidParts'],
  ['pago pendiente; el plan se actualizara al completar todas las partes.', 'pendingParts'],
  ['la renovacion automatica no pudo cobrarse: no existe una autorizacion recurrente del proveedor.', 'noRecurringAuthorization'],
  ['el periodo contratado finalizo sin renovacion automatica.', 'endedNoRenewal'],
  ['solicitud enviada al proveedor externo.', 'requestSent'],
  ['pendiente de aprobacion; la nota de credito se aplicara al confirmarse el pago.', 'creditAppliedOnPayment'],
  ['actualizacion cubierta con saldo a favor.', 'updatePaidWithCredit'],
  ['solicitud enviada al proveedor para pagar la diferencia.', 'differenceRequestSent'],
]);

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

  t(key: string, values: Record<string, string | number> = {}): string {
    return this.localization.traducir(`private.subscription.${key}`)
      .replace(/\{(\w+)\}/g, (placeholder, name: string) => String(values[name] ?? placeholder));
  }

  textoRegistrado(value: string | null | undefined, fallback = ''): string {
    if (!value) return fallback ? this.t(fallback) : '—';
    const normalized = value.normalize('NFD').replace(/\p{Diacritic}/gu, '').trim().toLowerCase();
    const key = subscriptionRecordKeys.get(normalized);
    return key ? this.t(key) : value;
  }

  conceptoDocumento(concepto: string): string {
    const plan = /^Suscripci[oó]n TeamBalance - (.+)$/i.exec(concepto)?.[1];
    return plan ? this.t('initialPlanDocument', { plan }) : this.textoRegistrado(concepto);
  }

  detalleHistorial(tipo: string, detalle: string): string {
    if (tipo === 'Cancelación de renovación automática' || tipo === 'Cancelación de servicio') return detalle;

    const recurring = /^Cobro recurrente aprobado por (Stripe|Mercado Pago) \((?:suscripción|autorización) ([^)]+)\)\.$/.exec(detalle);
    if (recurring) return this.t('recurringChargeApproved', { provider: recurring[1], reference: recurring[2] });

    const match = /^(.*?)\s+Vigencia extendida hasta (\d{2})\/(\d{2})\/(\d{4})\.$/.exec(detalle);
    if (!match) return this.detalleEstadoProveedor(detalle);

    const [, descripcion, dia, mes, anio] = match;
    const date = this.localization.formatearFecha(new Date(Number(anio), Number(mes) - 1, Number(dia)));
    return `${this.detalleEstadoProveedor(descripcion)} ${this.t('extendedUntil', { date })}`;
  }

  private detalleEstadoProveedor(detalle: string): string {
    const status = /^(Stripe|Mercado Pago) informó el estado '([^']+)'\.$/.exec(detalle);
    return status ? this.t('providerStatus', { provider: status[1], status: this.textoRegistrado(status[2]) }) : this.textoRegistrado(detalle);
  }
  readonly suscripcion = signal<Suscripcion | null>(null);
  readonly cargando = signal(true);
  readonly error = signal('');
  readonly mensaje = signal('');
  readonly planes = signal<PlanComercial[]>([]);
  readonly historial = signal<OperacionSuscripcion[]>([]);
  readonly actualizacionPendiente = computed(() => this.historial().find(
    operacion => operacion.tipoOperacion === 'Actualización distribuida' && operacion.estado === 'Pendiente' && operacion.referencia,
  ));
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
  readonly cambioForm = this.formBuilder.nonNullable.group({ idPlanComercial: [0, [Validators.min(1)] ], proveedor: ['MercadoPago', [Validators.required]], usarSaldoAFavor: [false], modoPago: ['unico'], importeCredito: [0] });
  readonly partesPago = this.formBuilder.array([this.crearPartePago(), this.crearPartePago()]);
  readonly cotizacionPago = signal<CotizacionPagoSuscripcion | null>(null);
  readonly distribucionIntentada = signal(false);
  readonly renovacionMercadoPagoForm = this.formBuilder.nonNullable.group({ payerEmail: ['', [Validators.required, Validators.email]] });
  readonly bajaForm = this.formBuilder.nonNullable.group({ motivo: ['', [Validators.required, Validators.maxLength(1000)]] });
  readonly cancelacionServicioForm = this.formBuilder.nonNullable.group({ motivo: ['', [Validators.required, Validators.maxLength(1000)]] });
  constructor() { this.recargar(); }

  recargar(): void {
    this.cargando.set(true); this.error.set('');
    forkJoin({ suscripcion: this.suscripcionService.consultarActual(), planes: this.suscripcionService.consultarPlanes(), historial: this.suscripcionService.consultarHistorial(), documentos: this.suscripcionService.consultarCuentaCorriente() })
      .pipe(finalize(() => this.cargando.set(false)))
      .subscribe({ next: ({ suscripcion, planes, historial, documentos }) => { this.suscripcion.set(suscripcion); this.planes.set(planes); this.historial.set(historial); this.documentos.set(documentos); }, error: error => this.error.set(this.obtenerMensajeError(error, this.t('loadError'))) });
  }

  abrirActualizacion(): void { this.mostrandoActualizacion.set(true); this.mensaje.set(''); }
  cerrarActualizacion(): void { this.mostrandoActualizacion.set(false); this.cambioForm.reset({ idPlanComercial: 0, proveedor: 'MercadoPago', usarSaldoAFavor: false }); }
  alternarHistorial(): void { this.mostrandoHistorial.update(valor => !valor); }

  actualizar(): void {
    this.cambioForm.markAllAsTouched(); if (this.cambioForm.invalid) return;
    if (this.cambioForm.controls.modoPago.value === 'combinado') this.repartirImportes();
    this.distribucionIntentada.set(true);
    if (this.errorDistribucion()) return;
    this.procesando.set(true); this.error.set(''); this.mensaje.set('');
    const cambio = this.cambioForm.getRawValue();
    const plan = this.planReactivacionSeleccionado();
    const usarSaldoAFavor = cambio.usarSaldoAFavor && !!plan && this.creditoDisponible(plan) > 0;
    const pagos = cambio.modoPago === 'combinado' ? this.partesSolicitadas().filter(parte => parte.importe > 0) : undefined;
    this.suscripcionService.solicitarActualizacion(cambio.idPlanComercial, cambio.proveedor, usarSaldoAFavor, pagos, usarSaldoAFavor ? cambio.importeCredito : undefined).pipe(finalize(() => this.procesando.set(false))).subscribe({ next: respuesta => { if (respuesta.urlPago) { window.location.assign(respuesta.urlPago); return; } this.authService.establecerSuscripcionRestringida(false); this.mensaje.set(this.t('updatedCredit')); this.cerrarActualizacion(); this.recargar(); }, error: error => { this.error.set(this.obtenerMensajeError(error, this.t('prepareError'))); this.suscripcionService.consultarHistorial().subscribe(historial => this.historial.set(historial)); } });
  }

  cancelarRenovacion(): void {
    this.bajaForm.markAllAsTouched(); if (this.bajaForm.invalid) return;
    this.procesando.set(true); this.error.set('');
    this.suscripcionService.cancelarRenovacion(this.bajaForm.getRawValue().motivo).pipe(finalize(() => this.procesando.set(false))).subscribe({ next: resultado => { this.suscripcion.set(resultado.suscripcion ?? null); this.mensaje.set(this.textoRegistrado(resultado.mensaje)); this.cancelando.set(false); this.bajaForm.reset({ motivo: '' }); this.recargar(); }, error: error => this.error.set(this.obtenerMensajeError(error, this.t('cancelRenewalError'))) });
  }

  cancelarServicio(): void {
    this.cancelacionServicioForm.markAllAsTouched();
    if (this.cancelacionServicioForm.invalid || !this.suscripcion()) return;
    this.procesando.set(true); this.error.set(''); this.mensaje.set('');
    const suscripcionActual = this.suscripcion()!;
    const documento: DocumentoComercial = { id: 0, idAgencia: suscripcionActual.idAgencia, idSuscripcion: suscripcionActual.id, tipo: '', numero: '', concepto: '', importe: 0, saldoPendiente: 0, moneda: suscripcionActual.moneda || 'ARS', estado: '', fechaEmision: new Date().toISOString(), motivo: this.cancelacionServicioForm.getRawValue().motivo };
    this.suscripcionService.cancelarServicio(documento).pipe(finalize(() => this.procesando.set(false))).subscribe({ next: notaCredito => { this.authService.establecerSuscripcionRestringida(true); this.mensaje.set(this.t('cancelServiceDone', { number: notaCredito.numero, amount: this.localization.formatearMoneda(notaCredito.importe, notaCredito.moneda) })); this.cancelandoServicio.set(false); this.cancelacionServicioForm.reset({ motivo: '' }); this.recargar(); }, error: error => this.error.set(this.obtenerMensajeError(error, this.t('cancelServiceError'))) });
  }

  abrirReactivacionConCredito(): void {
    this.cambioForm.reset({ idPlanComercial: 0, proveedor: 'MercadoPago', usarSaldoAFavor: false });
    this.regularizandoConCredito.set(true);
  }

  cancelarReactivacionConCredito(): void {
    this.cambioForm.reset({ idPlanComercial: 0, proveedor: 'MercadoPago', usarSaldoAFavor: false });
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
      this.suscripcionService.solicitarReactivacionConCreditoParcial(solicitud).pipe(finalize(() => this.procesando.set(false))).subscribe({ next: respuesta => window.location.assign(respuesta.urlPago), error: error => this.error.set(this.obtenerMensajeError(error, this.t('differenceError'))) });
      return;
    }
    this.suscripcionService.reactivarConNotaCredito(documento).pipe(finalize(() => this.procesando.set(false))).subscribe({ next: factura => { this.authService.establecerSuscripcionRestringida(false); this.mensaje.set(this.t('reactivateCreditDone', { number: factura.numero })); this.cancelarReactivacionConCredito(); this.recargar(); }, error: error => this.error.set(this.obtenerMensajeError(error, this.t('applyCreditError'))) });
  }

  planReactivacionSeleccionado(): PlanComercial | undefined { return this.planes().find(plan => plan.id === this.cambioForm.controls.idPlanComercial.value); }

  creditoActualizacionAplicable(): number {
    const plan = this.planReactivacionSeleccionado();
    return plan && this.cambioForm.controls.usarSaldoAFavor.value ? Number(this.cambioForm.controls.importeCredito.value) || 0 : 0;
  }

  requierePagoActualizacion(): boolean {
    const plan = this.planReactivacionSeleccionado();
    return !plan || this.creditoActualizacionAplicable() < plan.precioVigente;
  }

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
    this.suscripcionService.reactivarRenovacion().pipe(finalize(() => this.procesando.set(false))).subscribe({ next: resultado => { this.suscripcion.set(resultado.suscripcion ?? null); this.mensaje.set(this.textoRegistrado(resultado.mensaje)); this.recargar(); }, error: error => this.error.set(this.obtenerMensajeError(error, this.t('reactivateRenewalError'))) });
  }

  abrirAutorizacionRenovacion(): void {
    this.error.set(''); this.mensaje.set(''); this.cargandoFormularioMp.set(true);
    this.suscripcionService.consultarConfiguracionRenovacion().pipe(finalize(() => this.cargandoFormularioMp.set(false))).subscribe({
      next: configuracion => {
        this.configuracionRenovacion.set(configuracion);
        this.mostrandoAutorizacion.set(true);
      },
      error: error => this.error.set(this.obtenerMensajeError(error, this.t('mpFormError')))
    });
  }

  solicitarRenovacionStripe(): void {
    this.procesando.set(true); this.error.set(''); this.mensaje.set('');
    this.suscripcionService.solicitarRenovacionStripe().pipe(finalize(() => this.procesando.set(false))).subscribe({ next: respuesta => window.location.assign(respuesta.urlPago), error: error => this.error.set(this.obtenerMensajeError(error, this.t('stripeSetupError'))) });
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
      next: resultado => {
        if (resultado.preapprovalId) {
          sessionStorage.setItem(`renovacion-mercadopago:${resultado.referenciaOperacion}`, resultado.preapprovalId);
        }
        window.location.assign(resultado.urlPago);
      },
      error: error => this.error.set(this.obtenerMensajeError(error, this.t('authorizationError')))
    });
  }

  private crearPartePago() {
    return this.formBuilder.nonNullable.group({ proveedor: ['MercadoPago'], medioPago: ['credit_card'], importe: [0] });
  }

  seleccionarPlanPago(): void {
    const plan = this.planReactivacionSeleccionado();
    this.cotizacionPago.set(null);
    this.distribucionIntentada.set(false);
    this.partesPago.controls.forEach(parte => parte.controls.importe.markAsPristine());
    this.ajustarCredito();
    if (!plan) return;
    this.suscripcionService.consultarCotizacionPago(plan.id).subscribe({
      next: cotizacion => { if (this.cambioForm.controls.idPlanComercial.value === plan.id) this.cotizacionPago.set(cotizacion); },
      error: error => this.error.set(this.obtenerMensajeError(error, this.t('quoteError'))),
    });
  }

  ajustarCredito(): void {
    const plan = this.planReactivacionSeleccionado();
    this.cambioForm.controls.importeCredito.setValue(plan && this.cambioForm.controls.usarSaldoAFavor.value ? Math.min(plan.precioVigente, this.creditoDisponible(plan)) : 0);
    this.repartirImportes();
  }

  importeExterno(): number {
    return Math.max(0, Math.round(((this.planReactivacionSeleccionado()?.precioVigente ?? 0) - this.creditoActualizacionAplicable()) * 100) / 100);
  }

  repartirImportes(): void {
    const total = Math.round(this.importeExterno() * 100);
    const importes = this.partesPago.controls.map(parte => parte.controls.importe);
    const manuales = importes.filter(importe => importe.dirty);
    const automaticos = importes.filter(importe => importe.pristine);
    const asignado = manuales.reduce((suma, importe) => suma + Math.max(0, Math.round(Number(importe.value) * 100) || 0), 0);
    const restante = Math.max(0, total - asignado);
    const base = Math.floor(restante / (automaticos.length || 1));
    const sobrante = restante % (automaticos.length || 1);
    automaticos.forEach((importe, index) => {
      importe.setValue((base + (index < sobrante ? 1 : 0)) / 100);
    });
  }

  restablecerReparto(): void {
    this.partesPago.controls.forEach(parte => parte.controls.importe.markAsPristine());
    this.repartirImportes();
  }

  agregarPartePago(): void {
    this.partesPago.push(this.crearPartePago());
    this.repartirImportes();
  }

  quitarPartePago(index: number): void {
    if (this.partesPago.length === 1) return;
    this.partesPago.removeAt(index);
    this.repartirImportes();
  }

  cambiarProveedorParte(index: number): void {
    const parte = this.partesPago.at(index);
    if (parte.controls.proveedor.value === 'Stripe' && parte.controls.medioPago.value === 'account_money') parte.controls.medioPago.setValue('credit_card');
  }

  partesSolicitadas(): DistribucionPagoSuscripcion[] {
    if (this.importeExterno() === 0) return [];
    return this.partesPago.getRawValue().map(parte => ({
      proveedor: parte.proveedor,
      medioPago: parte.medioPago,
      importe: Number(parte.importe),
    }));
  }

  errorDistribucion(): string {
    const plan = this.planReactivacionSeleccionado();
    if (!plan) return this.t('seleccionaUnaModalidad');
    const credito = this.creditoActualizacionAplicable();
    if (!Number.isFinite(credito) || credito < 0 || credito > Math.min(plan.precioVigente, this.creditoDisponible(plan)) || Math.abs(credito * 100 - Math.round(credito * 100)) > 0.00001)
      return this.t('creditValidation');
    if (this.cambioForm.controls.modoPago.value !== 'combinado' || this.importeExterno() === 0) return '';
    const valores = this.partesPago.getRawValue();
    if (valores.some(parte => !Number.isFinite(Number(parte.importe)) || Number(parte.importe) < 0 || Math.abs(Number(parte.importe) * 100 - Math.round(Number(parte.importe) * 100)) > 0.00001))
      return this.t('decimalValidation');
    const partes = this.partesSolicitadas();
    if (Math.round(partes.reduce((total, parte) => total + parte.importe, 0) * 100) !== Math.round(this.importeExterno() * 100))
      return this.t('totalValidation');
    return '';
  }

  importeMercadoPago(index: number): number {
    const cotizacion = this.cotizacionPago();
    if (!cotizacion) return 0;
    let acumulado = 0;
    let cobrado = 0;
    for (const [posicion, parte] of this.partesSolicitadas().entries()) {
      if (parte.proveedor !== 'MercadoPago') continue;
      acumulado += Math.round(parte.importe * 100);
      const total = Math.round(Math.round(cotizacion.importeMercadoPago * 100) * acumulado / Math.round(cotizacion.importePlan * 100));
      if (posicion === index) return (total - cobrado) / 100;
      cobrado = total;
    }
    return 0;
  }

  continuarPago(referencia: string): void {
    this.procesando.set(true);
    this.error.set('');
    this.suscripcionService.continuarActualizacion(referencia).pipe(finalize(() => this.procesando.set(false))).subscribe({
      next: resultado => {
        if (resultado.urlPagoSiguiente) { window.location.assign(resultado.urlPagoSiguiente); return; }
        if (resultado.pagoConfirmado) this.authService.establecerSuscripcionRestringida(false);
        this.mensaje.set(this.textoRegistrado(resultado.mensaje));
        this.recargar();
      },
      error: error => this.error.set(this.obtenerMensajeError(error, this.t('resumeError'))),
    });
  }

  private obtenerMensajeError(error: { error?: unknown }, mensajePredeterminado: string): string {
    if (typeof error.error === 'string' && error.error.trim()) {
      const message = this.textoRegistrado(error.error);
      return message !== error.error || this.localization.language() === 'es' ? message : mensajePredeterminado;
    }
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
