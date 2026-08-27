import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { AuthPageLayoutComponent } from '../../../shared/layout/auth-page-layout/auth-page-layout.component';
import { AuthService } from '../../../services/auth.service';

@Component({
  selector: 'app-resend-validation',
  imports: [AuthPageLayoutComponent, ReactiveFormsModule, RouterLink],
  templateUrl: './resend-validation.component.html',
  styleUrl: '../../../shared/components/auth/auth-form.css',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ResendValidationComponent {
  private readonly formBuilder = inject(FormBuilder);
  private readonly authService = inject(AuthService);

  readonly submitting = signal(false);
  readonly completed = signal(false);
  readonly requestError = signal('');
  readonly resendForm = this.formBuilder.nonNullable.group({
    email: ['', [Validators.required, Validators.email]],
  });

  onSubmit(): void
  {
    this.requestError.set('');
    this.resendForm.markAllAsTouched();

    if (this.resendForm.invalid)
    {
      this.requestError.set('Ingresá un email laboral válido.');
      return;
    }

    this.submitting.set(true);

    this.authService.reenviarValidacion(this.resendForm.controls.email.value).subscribe({
      next: () => {
        this.submitting.set(false);
        this.completed.set(true);
      },
      error: () => {
        this.submitting.set(false);
        this.requestError.set('No pudimos procesar el reenvío. Intentá nuevamente en unos minutos.');
      },
    });
  }
}
