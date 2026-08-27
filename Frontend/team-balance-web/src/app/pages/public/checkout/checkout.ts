import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { ContratacionRequest, ContratacionService } from '../../../services/contratacion.service';

type BillingPeriod = 'monthly' | 'annual';

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

  readonly period = signal<BillingPeriod>('monthly');
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
    this.route.queryParamMap.subscribe((params) => {
      this.period.set(params.get('period') === 'annual' ? 'annual' : 'monthly');
    });
  }

  continueToPayment(): void {
    if (this.paymentIntegrationPending()) {
      return;
    }

    this.checkoutForm.markAllAsTouched();

    if (this.checkoutForm.invalid) {
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
      periodicidad: this.period() === 'annual' ? 'Anual' : 'Mensual',
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
