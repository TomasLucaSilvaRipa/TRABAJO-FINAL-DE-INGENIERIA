import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { finalize } from 'rxjs';
import { AuthService } from '../../../services/auth.service';
import { AuthPageLayoutComponent } from '../../../shared/layout/auth-page-layout/auth-page-layout.component';
import { LocalizationService } from '../../../services/localization.service';
import { BackupEmailService } from '../../../services/backup-email.service';

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
  private readonly backupEmailService = inject(BackupEmailService);
  readonly localization = inject(LocalizationService);

  readonly submitting = signal(false);
  readonly requestError = signal('');
  readonly enviado = signal(false);
  readonly usandoCorreoRespaldo = signal(false);

  readonly forgotPasswordForm = this.formBuilder.nonNullable.group({
    email: ['', [Validators.required, Validators.email]],
  });

  onSubmit(): void {
    if (this.submitting()) return;
    this.requestError.set('');
    this.enviado.set(false);
    this.forgotPasswordForm.markAllAsTouched();

    if (this.forgotPasswordForm.invalid)
    {
      this.requestError.set('forgotPassword.invalidEmail');
      return;
    }

    this.submitting.set(true);

    const email = this.forgotPasswordForm.getRawValue().email;
    const solicitud = this.usandoCorreoRespaldo()
      ? this.backupEmailService.recuperar(email)
      : this.authService.solicitarRecuperoPassword(email);
    solicitud
      .pipe(finalize(() => this.submitting.set(false)))
      .subscribe({
        next: () => this.enviado.set(true),
        error: () => this.requestError.set('forgotPassword.error'),
      });
  }

  alternarCorreoRespaldo(): void {
    this.usandoCorreoRespaldo.update(valor => !valor);
    this.enviado.set(false);
    this.requestError.set('');
  }
}
