import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { ReactiveFormsModule, FormBuilder, Validators } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { finalize } from 'rxjs';
import { AuthService } from '../../../services/auth.service';
import { PageBreadcrumbComponent } from '../../../shared/components/common/page-breadcrumb/page-breadcrumb.component';

@Component({
  selector: 'app-change-password',
  imports: [PageBreadcrumbComponent, ReactiveFormsModule, RouterLink],
  templateUrl: './change-password.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ChangePasswordComponent {
  private readonly formBuilder = inject(FormBuilder);
  private readonly authService = inject(AuthService);

  readonly submitting = signal(false);
  readonly requestError = signal('');
  readonly responseMessage = signal('');

  readonly changePasswordForm = this.formBuilder.nonNullable.group({
    passwordActual: ['', Validators.required],
    password: ['', [Validators.required, Validators.minLength(8)]],
    repetirPassword: ['', Validators.required],
  });

  onSubmit(): void {
    this.requestError.set('');
    this.responseMessage.set('');
    this.changePasswordForm.markAllAsTouched();

    const form = this.changePasswordForm.getRawValue();

    if (this.changePasswordForm.invalid || form.password !== form.repetirPassword)
    {
      this.requestError.set('La nueva contraseña debe tener al menos 8 caracteres y ambas contraseñas deben coincidir.');
      return;
    }

    this.submitting.set(true);

    this.authService.cambiarPassword(form.passwordActual, form.password)
      .pipe(finalize(() => this.submitting.set(false)))
      .subscribe({
        next: (respuesta) => {
          this.authService.logOut();
          this.responseMessage.set(respuesta.mensaje);
        },
        error: (error) => this.requestError.set(typeof error.error === 'string' ? error.error : 'No fue posible modificar la contraseña. Intentá nuevamente.'),
      });
  }
}
