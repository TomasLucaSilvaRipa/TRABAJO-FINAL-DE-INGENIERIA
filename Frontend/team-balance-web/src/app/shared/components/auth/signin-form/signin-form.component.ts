
import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { finalize } from 'rxjs';
import { AuthService } from '../../../../services/auth.service';

@Component({
  selector: 'app-signin-form',
  imports: [RouterLink, ReactiveFormsModule],
  templateUrl: './signin-form.component.html',
  styleUrl: '../auth-form.css',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class SigninFormComponent {
  private readonly formBuilder = inject(FormBuilder);
  private readonly router = inject(Router);
  private readonly authService = inject(AuthService);

  showPassword = false;
  readonly submitting = signal(false);
  readonly requestError = signal('');

  readonly signInForm = this.formBuilder.nonNullable.group({
    email: ['', [Validators.required, Validators.email]],
    password: ['', Validators.required],
    mantenerSesion: [false],
  });

  togglePasswordVisibility(): void {
    this.showPassword = !this.showPassword;
  }

  onSignIn(): void {
    this.requestError.set('');
    this.signInForm.markAllAsTouched();

    if (this.signInForm.invalid)
    {
      this.requestError.set('Ingresá un email válido y tu contraseña.');
      return;
    }

    const form = this.signInForm.getRawValue();
    this.submitting.set(true);

    this.authService.logIn(
      {
        email: form.email,
        passwordHash: form.password,
      },
      form.mantenerSesion,
    )
      .pipe(finalize(() => this.submitting.set(false)))
      .subscribe({
        next: (respuesta) => {
          localStorage.setItem('token', respuesta.accessToken);
          localStorage.setItem('tokenExpiresAt', respuesta.expiresAt);
          void this.router.navigate(['/dashboard']);
        },
        error: (error) => {
          this.requestError.set(
            typeof error.error === 'string'
              ? error.error
              : 'No fue posible iniciar sesión. Intentá nuevamente.',
          );
        },
      });
  }
}
