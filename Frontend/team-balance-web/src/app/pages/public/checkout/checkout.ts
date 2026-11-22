import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { HttpErrorResponse } from '@angular/common/http';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { ContratacionRequest, ContratacionService } from '../../../services/contratacion.service';
import { LocalizationService } from '../../../services/localization.service';
import { PlanComercial, PlanComercialService } from '../../../services/plan-comercial.service';
import type { CotizacionPagoSuscripcion, DistribucionPagoSuscripcion } from '../../../services/suscripcion.service';

@Component({
  selector: 'app-checkout',
  imports: [ReactiveFormsModule, RouterLink],
  templateUrl: './checkout.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})

export class CheckoutComponent {
  private readonly formBuilder = inject(FormBuilder);
  private readonly route = inject(ActivatedRoute);
  private readonly contratacionService = inject(ContratacionService);
  private readonly planService = inject(PlanComercialService);
  readonly localization = inject(LocalizationService);

  readonly plan = signal<PlanComercial | null>(null);
  readonly planLoading = signal(true);
  readonly planError = signal(false);
  readonly paymentIntegrationPending = signal(false);
  readonly reviewingCommercialData = signal(false);
  readonly cotizacion = signal<CotizacionPagoSuscripcion | null>(null);
  readonly errorPago = signal('');
  readonly partesPago = this.formBuilder.array([this.crearParte(), this.crearParte()]);

  readonly checkoutForm = this.formBuilder.nonNullable.group({
    commercialName: ['', Validators.required],
    legalName: ['', Validators.required],
    taxId: ['', Validators.required],
    taxCondition: ['', Validators.required],
    billingEmail: ['', [Validators.required, Validators.email]],
    phone: ['', Validators.required],
    firstName: ['', Validators.required],
    lastName: ['', Validators.required],
    workEmail: ['', [Validators.required, Validators.email]],
    role: ['', Validators.required],
    paymentProvider: ['MercadoPago', Validators.required],
    paymentMode: ['unico'],
    acceptsTerms: [false, Validators.requiredTrue],
  });

  constructor() {
    this.route.queryParamMap.subscribe((params) => this.loadPlan(Number(params.get('planId'))));
  }

  private loadPlan(id: number): void {
    this.checkoutForm.controls.paymentMode.setValue('unico');
    this.cotizacion.set(null);
    this.planLoading.set(true);
    this.planError.set(false);

    if (id <= 0) {
      this.planService.consultarPlanesActivos().subscribe({
        next: (plans) => {
          this.plan.set(plans[0] ?? null);
          this.planError.set(!this.plan());
          this.planLoading.set(false);
        },
        error: () => { this.planError.set(true); this.planLoading.set(false); },
      });
      return;
    }

    this.planService.consultarPlan(id).subscribe({
      next: (plan) => {
        this.plan.set(plan);
        this.planError.set(!this.plan());
        this.planLoading.set(false);
      },
      error: () => { this.planError.set(true); this.planLoading.set(false); },
    });
  }

  features(): string[] {
    const plan = this.plan();
    return plan ? this.planService.obtenerFuncionalidades(plan) : [];
  }

  featureLabel(feature: string): string {
    return this.localization.traducir(`plans.feature.${feature}`, undefined, feature);
  }

  planDescription(): string {
    const plan = this.plan();
    if (!plan) return '';
    return this.localization.traducir(`plans.description.${plan.nombre.toLowerCase()}`, undefined, plan.descripcion ?? '');
  }

  private crearParte() {
    return this.formBuilder.nonNullable.group({
      proveedor: ['MercadoPago'],
      medioPago: ['credit_card'],
      importe: [0],
    });
  }

  cambiarModoPago(): void {
    this.errorPago.set('');
    this.cotizacion.set(null);
    const plan = this.plan();
    if (!plan || this.checkoutForm.controls.paymentMode.value !== 'combinado') return;
    this.repartirImportes();
    this.contratacionService.consultarCotizacion(plan.id).subscribe({
      next: cotizacion => {
        if (this.plan()?.id === plan.id && this.checkoutForm.controls.paymentMode.value === 'combinado') {
          this.cotizacion.set(cotizacion);
          this.repartirImportes();
        }
      },
      error: () => {
        if (this.plan()?.id === plan.id && this.checkoutForm.controls.paymentMode.value === 'combinado') {
          this.errorPago.set('checkout.splitUnavailable');
        }
      },
    });
  }

  cambiarProveedor(index: number): void {
    const parte = this.partesPago.at(index).controls;
    if (parte.proveedor.value === 'Stripe' && parte.medioPago.value === 'account_money') {
      parte.medioPago.setValue('credit_card');
    }
  }

  cambiarCantidadPartes(index?: number): void {
    if (index === undefined) this.partesPago.push(this.crearParte());
    else if (this.partesPago.length > 2) this.partesPago.removeAt(index);
    this.repartirImportes();
  }

  repartirImportes(): void {
    const total = Math.round((this.cotizacion()?.importePlan ?? this.plan()?.precioVigente ?? 0) * 100);
    const controles = this.partesPago.controls.map(parte => parte.controls.importe);
    const automaticos = controles.filter(control => control.pristine);
    if (!automaticos.length) return;
    const asignado = controles.filter(control => control.dirty)
      .reduce((suma, control) => suma + Math.max(0, Math.round(Number(control.value) * 100) || 0), 0);
    const restante = Math.max(0, total - asignado);
    const base = Math.floor(restante / automaticos.length);
    const sobrante = restante % automaticos.length;
    automaticos.forEach((control, index) => control.setValue((base + (index < sobrante ? 1 : 0)) / 100));
  }

  repartirEnPartesIguales(): void {
    this.partesPago.controls.forEach(parte => parte.controls.importe.markAsPristine());
    this.repartirImportes();
  }

  importeMercadoPago(index: number): number {
    const cotizacion = this.cotizacion();
    if (!cotizacion?.importePlan) return 0;
    const partes = this.partesPago.getRawValue().slice(0, index + 1);
    const anteriores = partes.slice(0, -1).filter(parte => parte.proveedor === 'MercadoPago')
      .reduce((suma, parte) => suma + Math.round((Number(parte.importe) || 0) * 100), 0);
    const importe = Math.round((Number(partes[index]?.importe) || 0) * 100);
    const cotizar = (centavos: number) => Math.round(cotizacion.importeMercadoPago * centavos / cotizacion.importePlan);
    return (cotizar(anteriores + importe) - cotizar(anteriores)) / 100;
  }

  errorDistribucion(): string {
    if (this.checkoutForm.controls.paymentMode.value !== 'combinado' || !this.cotizacion()) return '';
    const pagos = this.partesPago.getRawValue();
    if (pagos.some(({ importe }) => !Number.isFinite(importe) || importe <= 0 || Math.abs(importe * 100 - Math.round(importe * 100)) > 0.00001)) {
      return 'checkout.splitPositive';
    }
    const total = pagos.reduce((suma, { importe }) => suma + Math.round(importe * 100), 0);
    return total === Math.round(this.cotizacion()!.importePlan * 100) ? '' : 'private.subscription.totalValidation';
  }

  continueToPayment(): void {
    if (this.paymentIntegrationPending()) {
      return;
    }

    this.checkoutForm.markAllAsTouched();

    if (this.checkoutForm.invalid || !this.plan() || !this.pagoListo()) {
      return;
    }


    this.reviewingCommercialData.set(true);
  }

  editCommercialData(): void {
    this.reviewingCommercialData.set(false);
  }

  confirmPayment(): void {
    if (this.paymentIntegrationPending() || this.checkoutForm.invalid || !this.plan() || !this.pagoListo()) return;
    this.errorPago.set('');
    this.paymentIntegrationPending.set(true);

    const form = this.checkoutForm.getRawValue();
    const pagos: DistribucionPagoSuscripcion[] | undefined = form.paymentMode === 'combinado'
      ? this.partesPago.getRawValue()
      : undefined;

    const contratacion: ContratacionRequest = {
      nombreComercialAgencia: form.commercialName,
      razonSocial: form.legalName,
      cuit: form.taxId,
      condicionFiscal: form.taxCondition,
      emailFacturacion: form.billingEmail,
      telefonoContacto: form.phone,
      nombreResponsable: form.firstName,
      apellidoResponsable: form.lastName,
      emailLaboralResponsable: form.workEmail,
      cargoResponsable: form.role,
      proveedorPagoSeleccionado: form.paymentProvider,
      idPlanComercial: this.plan()!.id,
      pagos,
    };

    this.contratacionService.contratar(contratacion)
    .subscribe({
      next: (respuesta) => {
        window.location.assign(respuesta.urlPago);
      },

      error: (error: HttpErrorResponse) => {
        this.paymentIntegrationPending.set(false);
        this.reviewingCommercialData.set(false);
        if (error.status === 409 && error.error?.code === 'datos_contratacion_en_uso') {
          this.errorPago.set('checkout.registrationConflict');
          return;
        }
        this.errorPago.set(pagos ? 'checkout.splitUnavailable' : 'checkout.paymentUnavailable');
      }
    });
  }

  pagoListo(): boolean {
    return this.checkoutForm.controls.paymentMode.value !== 'combinado' || (!!this.cotizacion() && !this.errorDistribucion());
  }
}
