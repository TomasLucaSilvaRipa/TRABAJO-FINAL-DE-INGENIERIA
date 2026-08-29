import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { finalize } from 'rxjs';
import { AuthService } from '../../../services/auth.service';
import { AuthPageLayoutComponent } from '../../../shared/layout/auth-page-layout/auth-page-layout.component';

@Component({
  selector: 'app-forgot-password',
  imports: [AuthPageLayoutComponent, ReactiveFormsModule, RouterLink],
  templateUrl: './forgot-password.component.html',
  styleUrl: '../../../shared/components/auth/auth-form.css',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ForgotPasswordComponent {
  private readonly formBuilder = inject(FormBuilder);
  private readonly authService = inject(AuthService);

  readonly submitting = signal(false);
  readonly requestError = signal('');
  readonly responseMessage = signal('');

  readonly forgotPasswordForm = this.formBuilder.nonNullable.group({
    email: ['', [Validators.required, Validators.email]],
  });

  onSubmit(): void {
    this.requestError.set('');
    this.responseMessage.set('');
    this.forgotPasswordForm.markAllAsTouched();

    if (this.forgotPasswordForm.invalid)
    {
      this.requestError.set('Ingresá un email válido.');
      return;
    }

    this.submitting.set(true);

    this.authService.solicitarRecuperoPassword(this.forgotPasswordForm.getRawValue().email)
      .pipe(finalize(() => this.submitting.set(false)))
      .subscribe({
        next: (respuesta) => this.responseMessage.set(respuesta.mensaje),
        error: (error) => this.requestError.set(typeof error.error === 'string' ? error.error : 'No fue posible procesar la solicitud. Intentá nuevamente.'),
      });
  }
}
