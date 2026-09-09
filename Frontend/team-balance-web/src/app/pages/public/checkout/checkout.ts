import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { ContratacionRequest, ContratacionService } from '../../../services/contratacion.service';
import { LocalizationService } from '../../../services/localization.service';
import { PlanComercial, PlanComercialService } from '../../../services/plan-comercial.service';

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
    acceptsTerms: [false, Validators.requiredTrue],
  });

  constructor() {
    this.route.queryParamMap.subscribe((params) => this.loadPlan(Number(params.get('planId'))));
  }

  private loadPlan(id: number): void {
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

  continueToPayment(): void {
    if (this.paymentIntegrationPending()) {
      return;
    }

    this.checkoutForm.markAllAsTouched();

    if (this.checkoutForm.invalid || !this.plan()) {
      return;
    }


    this.paymentIntegrationPending.set(true);

    const form = this.checkoutForm.getRawValue();

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
    };

    this.contratacionService.contratar(contratacion)
    .subscribe({
      next: (respuesta) => {
        window.location.assign(respuesta.urlPago);
      },

      error: (error) => {
        console.error('Error completo:', error);
        console.error('Respuesta backend:', error.error);
        this.paymentIntegrationPending.set(false);
      }
    });
  }
}
