
import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { HttpErrorResponse } from '@angular/common/http';
import { finalize } from 'rxjs';
import { AuthService, DesafioDosFactoresResponse, LogInResponse } from '../../../../services/auth.service';
import { RecaptchaService } from '../../../../services/recaptcha.service';
import { LocalizationService } from '../../../../services/localization.service';

@Component({
  selector: 'app-signin-form',
  imports: [DatePipe, RouterLink, ReactiveFormsModule],
  templateUrl: './signin-form.component.html',
  styleUrl: '../auth-form.css',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class SigninFormComponent {
  private readonly formBuilder = inject(FormBuilder);
  private readonly authService = inject(AuthService);
  private readonly recaptchaService = inject(RecaptchaService);
  private readonly route = inject(ActivatedRoute);
  readonly localization = inject(LocalizationService);

  showPassword = false;
  readonly submitting = signal(false);
  readonly requestError = signal('');
  readonly desafioDosFactores = signal<DesafioDosFactoresResponse | null>(null);
  readonly codigoDosFactores = signal('');

  readonly signInForm = this.formBuilder.nonNullable.group({
    email: ['', [Validators.required, Validators.email]],
    password: ['', Validators.required],
    mantenerSesion: [false],
  });

  constructor() {
    if (this.route.snapshot.queryParamMap.get('motivo') === 'agencia-inactiva')
    {
      this.requestError.set('La agencia no tiene una suscripción activa. Contactá al dueño para reactivar el servicio.');
    }
  }

  togglePasswordVisibility(): void {
    this.showPassword = !this.showPassword;
  }

  async onSignIn(): Promise<void> {
    this.requestError.set('');
    this.signInForm.markAllAsTouched();

    if (this.signInForm.invalid)
    {
      this.requestError.set(this.localization.traducir('login.invalid'));
      return;
    }

    const form = this.signInForm.getRawValue();
    this.submitting.set(true);

    try {
      const recaptchaToken = await this.recaptchaService.ejecutarLogin();
      this.authService.logIn( { email: form.email, passwordHash: form.password, recaptchaToken, }, form.mantenerSesion,)
      .pipe(finalize(() => this.submitting.set(false))).subscribe({
          next: (respuesta) => {
            if ('requiereDosFactores' in respuesta) { this.desafioDosFactores.set(respuesta); return; }
            this.completarInicioSesion(respuesta, form.mantenerSesion);
          },
          error: (error) => { this.requestError.set( typeof error.error === 'string' ? error.error : 'No fue posible iniciar sesión. Intentá nuevamente.',);},
        });
    } catch (error) {
      this.submitting.set(false);
      this.requestError.set(error instanceof Error ? error.message : 'No fue posible validar la verificación de seguridad.');
    }
  }

  confirmarDosFactores(): void {
    const desafio = this.desafioDosFactores();
    const codigo = this.codigoDosFactores().replace(/\s/g, '');
    if (!desafio || !/^\d{6}$/.test(codigo)) { this.requestError.set('Ingresá el código de seis dígitos de Google Authenticator.'); return; }
    this.submitting.set(true); this.requestError.set('');
    this.authService.confirmarInicioSesionDosFactores(desafio.tokenDesafio, codigo).pipe(finalize(() => this.submitting.set(false))).subscribe({
      next: respuesta => this.completarInicioSesion(respuesta, this.signInForm.controls.mantenerSesion.value),
      error: error => this.requestError.set(typeof error.error === 'string' ? error.error : 'No fue posible verificar el código.'),
    });
  }

  volverAlInicioSesion(): void { this.desafioDosFactores.set(null); this.codigoDosFactores.set(''); this.requestError.set(''); }

  private completarInicioSesion(respuesta: LogInResponse, mantenerSesion: boolean): void {
    this.authService.guardarSesion(respuesta, mantenerSesion);
    this.submitting.set(true);
    this.authService.validarAccesoAgencia().pipe(finalize(() => this.submitting.set(false))).subscribe({
      next: () => {
        window.location.assign('/dashboard');
      },
      error: (error: HttpErrorResponse) => {
        if (error.status === 403 && error.error?.code === 'SUBSCRIPTION_EXPIRED')
        {
          if (this.authService.tienePermiso('GestionarSuscripcion'))
          {
            this.authService.establecerSuscripcionRestringida(true);
            window.location.assign('/dashboard/suscripcion');
            return;
          }

          this.authService.logOut();
          this.requestError.set('La agencia no tiene una suscripción activa. Contactá al dueño para reactivar el servicio.');
          return;
        }

        this.authService.logOut();
        this.requestError.set('No fue posible comprobar el acceso de la agencia. Intentá nuevamente.');
      },
    });
  }
}
