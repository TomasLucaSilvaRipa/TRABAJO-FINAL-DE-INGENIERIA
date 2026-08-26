import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, RouterLink } from '@angular/router';

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
    paymentProvider: ['mercado-pago', Validators.required],
    acceptsTerms: [false, Validators.requiredTrue],
  });

  constructor() {
    this.route.queryParamMap.subscribe((params) => {
      this.period.set(params.get('period') === 'annual' ? 'annual' : 'monthly');
    });
  }

  continueToPayment(): void {
    this.checkoutForm.markAllAsTouched();

    if (this.checkoutForm.invalid) {
      return;
    }

    this.paymentIntegrationPending.set(true);
  }
}
