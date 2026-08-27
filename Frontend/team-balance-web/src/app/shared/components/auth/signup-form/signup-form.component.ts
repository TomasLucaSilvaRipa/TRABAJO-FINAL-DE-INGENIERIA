
import { ChangeDetectionStrategy, Component, DestroyRef, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { finalize } from 'rxjs';
import { AuthService, RegistryRequest } from '../../../../services/auth.service';
import { ContratacionService } from '../../../../services/contratacion.service';

@Component({
  selector: 'app-signup-form',
  imports: [ReactiveFormsModule, RouterLink],
  templateUrl: './signup-form.component.html',
  styleUrl: '../auth-form.css',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class SignupFormComponent {
  private readonly destroyRef = inject(DestroyRef);
  private readonly formBuilder = inject(FormBuilder);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly authService = inject(AuthService);
  private readonly contratacionService = inject(ContratacionService);

  readonly referenciaContratacion = signal('');
  readonly validatingContinuation = signal(true);
  readonly continuationError = signal('');
  readonly submitting = signal(false);
  readonly requestError = signal('');
  readonly registrationCompleted = signal(false);
  readonly showPassword = signal(false);
  readonly showConfirmPassword = signal(false);

  readonly registryForm = this.formBuilder.nonNullable.group({
    firstName: ['', [Validators.required, Validators.minLength(2)]],
    lastName: ['', [Validators.required, Validators.minLength(2)]],
    workEmail: ['', [Validators.required, Validators.email]],
    phone: ['', [Validators.required, Validators.minLength(6)]],
    password: ['', [Validators.required, Validators.minLength(8), Validators.pattern(/^(?=.*[A-Za-z])(?=.*\d).+$/)]],
    confirmPassword: ['', Validators.required],
  });

  constructor() {
    this.route.queryParamMap
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((params) => this.validarContinuidad(params.get('referencia')));
  }

  togglePasswordVisibility(): void {
    this.showPassword.update((visible) => !visible);
  }

  toggleConfirmPasswordVisibility(): void {
    this.showConfirmPassword.update((visible) => !visible);
  }

  onSignUp(): void {
    this.requestError.set('');
    this.registryForm.markAllAsTouched();

    if (this.registryForm.invalid) {
      return;
    }

    if (this.registryForm.controls.password.value !== this.registryForm.controls.confirmPassword.value) {
      this.requestError.set('Las contraseñas no coinciden.');
      return;
    }

    const form = this.registryForm.getRawValue();
    const referenciaContratacion = this.referenciaContratacion();

    if (!referenciaContratacion) {
      this.requestError.set('No pudimos validar la contratación asociada a este registro.');
      return;
    }

    const registryData: RegistryRequest = {
      referenciaContratacion,
      nombre: form.firstName,
      apellido: form.lastName,
      emailLaboral: form.workEmail,
      telefonoResponsable: form.phone,
      password: form.password,
    };

    this.submitting.set(true);

    this.authService.registrarAgencia(registryData)
      .pipe(finalize(() => this.submitting.set(false)))
      .subscribe({
        next: () => this.registrationCompleted.set(true),
        error: (error) => {
          this.requestError.set(
            typeof error.error === 'string'
              ? error.error
              : 'No pudimos registrar la agencia. Revisá los datos e intentá nuevamente.',
          );
        },
      });
  }

  hasError(controlName: keyof typeof this.registryForm.controls): boolean {
    const control = this.registryForm.controls[controlName];
    return control.touched && control.invalid;
  }

  private validarContinuidad(referencia: string | null): void {
    this.requestError.set('');
    this.continuationError.set('');

    if (!referencia) {
      void this.router.navigate(['/plans'], { replaceUrl: true });
      return;
    }

    this.validatingContinuation.set(true);

    this.contratacionService.consultarEstado(referencia)
      .pipe(finalize(() => this.validatingContinuation.set(false)))
      .subscribe({
        next: (contratacion) => {
          if (!contratacion.puedeRegistrar) {
            void this.router.navigate(['/pago/resultado'], {
              queryParams: { external_reference: referencia },
              replaceUrl: true,
            });
            return;
          }

          this.referenciaContratacion.set(contratacion.referencia);
        },
        error: () => {
          this.continuationError.set('No encontramos una contratación aprobada asociada a este registro.');
        },
      });
  }
}
