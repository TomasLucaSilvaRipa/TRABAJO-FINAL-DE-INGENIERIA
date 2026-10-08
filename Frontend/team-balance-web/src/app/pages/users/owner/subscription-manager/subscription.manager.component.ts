import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { RouterModule } from '@angular/router';
import { finalize, forkJoin } from 'rxjs';
import { PlanComercial } from '../../../../services/plan-comercial.service';
import { ConfiguracionRenovacionAutomatica, OperacionSuscripcion, Suscripcion, SuscripcionService } from '../../../../services/suscripcion.service';
import { LocalizationService } from '../../../../services/localization.service';

@Component({
  selector: 'app-subscription',
  imports: [CommonModule, RouterModule, ReactiveFormsModule],
  templateUrl: './subscription-manager.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class Subscription {
  private readonly suscripcionService = inject(SuscripcionService);
  private readonly formBuilder = inject(FormBuilder);
  readonly localization = inject(LocalizationService);
  readonly suscripcion = signal<Suscripcion | null>(null);
  readonly cargando = signal(true);
  readonly error = signal('');
  readonly mensaje = signal('');
  readonly planes = signal<PlanComercial[]>([]);
  readonly historial = signal<OperacionSuscripcion[]>([]);
  readonly mostrandoHistorial = signal(false);
  readonly mostrandoActualizacion = signal(false);
  readonly procesando = signal(false);
  readonly cancelando = signal(false);
  readonly mostrandoAutorizacion = signal(false);
  readonly cargandoFormularioMp = signal(false);
  readonly configuracionRenovacion = signal<ConfiguracionRenovacionAutomatica | null>(null);
  readonly cambioForm = this.formBuilder.nonNullable.group({ idPlanComercial: [0, [Validators.min(1)] ] });
  readonly bajaForm = this.formBuilder.nonNullable.group({ motivo: ['', [Validators.required, Validators.maxLength(1000)]] });
  private cardForm: any;

  constructor() { this.recargar(); }

  recargar(): void {
    this.cargando.set(true); this.error.set('');
    forkJoin({ suscripcion: this.suscripcionService.consultarActual(), planes: this.suscripcionService.consultarPlanes(), historial: this.suscripcionService.consultarHistorial() })
      .pipe(finalize(() => this.cargando.set(false)))
      .subscribe({ next: ({ suscripcion, planes, historial }) => { this.suscripcion.set(suscripcion); this.planes.set(planes); this.historial.set(historial); }, error: error => this.error.set(error?.error?.message ?? error?.error ?? 'No se pudo consultar la suscripción de la agencia.') });
  }

  abrirActualizacion(): void { this.mostrandoActualizacion.set(true); this.mensaje.set(''); }
  cerrarActualizacion(): void { this.mostrandoActualizacion.set(false); this.cambioForm.reset({ idPlanComercial: 0 }); }
  alternarHistorial(): void { this.mostrandoHistorial.update(valor => !valor); }

  actualizar(): void {
    this.cambioForm.markAllAsTouched(); if (this.cambioForm.invalid) return;
    this.procesando.set(true); this.error.set(''); this.mensaje.set('');
    this.suscripcionService.solicitarActualizacion(this.cambioForm.getRawValue().idPlanComercial).pipe(finalize(() => this.procesando.set(false))).subscribe({ next: respuesta => window.location.assign(respuesta.urlPago), error: error => this.error.set(error?.error?.message ?? error?.error ?? 'No se pudo solicitar la actualización al proveedor.') });
  }

  cancelarRenovacion(): void {
    this.bajaForm.markAllAsTouched(); if (this.bajaForm.invalid) return;
    this.procesando.set(true); this.error.set('');
    this.suscripcionService.cancelarRenovacion(this.bajaForm.getRawValue().motivo).pipe(finalize(() => this.procesando.set(false))).subscribe({ next: resultado => { this.suscripcion.set(resultado.suscripcion ?? null); this.mensaje.set(resultado.mensaje); this.cancelando.set(false); this.bajaForm.reset({ motivo: '' }); this.recargar(); }, error: error => this.error.set(error?.error?.message ?? error?.error ?? 'No se pudo cancelar la renovación automática.') });
  }

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
        window.setTimeout(() => this.inicializarCardForm(configuracion), 0);
      },
      error: error => this.error.set(error?.error?.message ?? error?.error ?? 'No se pudo preparar el formulario seguro de Mercado Pago.')
    });
  }

  cerrarAutorizacionRenovacion(): void {
    this.cardForm?.unmount?.(); this.cardForm = undefined;
    this.mostrandoAutorizacion.set(false); this.configuracionRenovacion.set(null);
  }

  private async inicializarCardForm(configuracion: ConfiguracionRenovacionAutomatica): Promise<void> {
    try {
      await this.cargarSdkMercadoPago();
      this.cardForm?.unmount?.();
      const mp = new window.MercadoPago!(configuracion.publicKey, { locale: 'es-AR' });
      this.cardForm = mp.cardForm({
        amount: configuracion.importePrueba.toFixed(2),
        iframe: true,
        form: {
          id: 'tb-renovacion-card-form',
          cardholderName: { id: 'tb-cardholder-name', placeholder: 'Titular de la tarjeta' },
          cardholderEmail: { id: 'tb-cardholder-email', placeholder: 'email@testuser.com' },
          cardNumber: { id: 'tb-card-number', placeholder: 'Número de tarjeta' },
          cardExpirationDate: { id: 'tb-card-expiration', placeholder: 'MM/AA' },
          securityCode: { id: 'tb-card-security-code', placeholder: 'CVV' },
          installments: { id: 'tb-card-installments', placeholder: 'Cuotas' },
          identificationType: { id: 'tb-card-identification-type', placeholder: 'Tipo de documento' },
          identificationNumber: { id: 'tb-card-identification-number', placeholder: 'Número de documento' },
          issuer: { id: 'tb-card-issuer', placeholder: 'Banco emisor' },
        },
        callbacks: {
          onFormMounted: (error: unknown) => { if (error) this.error.set('No se pudo cargar el formulario de Mercado Pago.'); },
          onSubmit: (event: Event) => {
            event.preventDefault();
            const datos = this.cardForm.getCardFormData();
            if (!datos?.token || !datos?.cardholderEmail) { this.error.set('Completá los datos del medio de pago de prueba.'); return; }
            this.confirmarAutorizacion(datos.token, datos.cardholderEmail);
          },
        },
      });
    } catch {
      this.error.set('No se pudo cargar el formulario seguro de Mercado Pago. Revisá tu conexión e intentá nuevamente.');
    }
  }

  private confirmarAutorizacion(cardToken: string, payerEmail: string): void {
    this.procesando.set(true); this.error.set('');
    this.suscripcionService.autorizarRenovacion(cardToken, payerEmail).pipe(finalize(() => this.procesando.set(false))).subscribe({
      next: resultado => { this.suscripcion.set(resultado.suscripcion ?? null); this.mensaje.set(resultado.mensaje); this.cerrarAutorizacionRenovacion(); this.recargar(); },
      error: error => this.error.set(error?.error?.message ?? error?.error ?? 'No se pudo autorizar la renovación automática.')
    });
  }

  private cargarSdkMercadoPago(): Promise<void> {
    if (window.MercadoPago) return Promise.resolve();
    return new Promise((resolve, reject) => {
      const existente = document.querySelector<HTMLScriptElement>('script[data-team-balance-mp-sdk]');
      if (existente) { existente.addEventListener('load', () => resolve(), { once: true }); existente.addEventListener('error', () => reject(), { once: true }); return; }
      const script = document.createElement('script');
      script.src = 'https://sdk.mercadopago.com/js/v2'; script.async = true; script.dataset['teamBalanceMpSdk'] = 'true';
      script.onload = () => resolve(); script.onerror = () => reject(); document.head.appendChild(script);
    });
  }
}

declare global {
  interface Window { MercadoPago?: new (publicKey: string, options?: { locale?: string }) => any; }
}
