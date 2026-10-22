
import { ChangeDetectionStrategy, Component, DestroyRef, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { catchError, debounceTime, EMPTY, finalize, of, switchMap, tap, distinctUntilChanged } from 'rxjs';
import { AuthService, EstadoTelegramRegistroResponse, PreparacionTelegramResponse } from '../../../../services/auth.service';
import { ContratacionService } from '../../../../services/contratacion.service';
import { CategoriaNoticia, NovedadesService } from '../../../../services/novedades.service';
import { PasswordSecurityService,PasswordEvaluation } from '../../../../services/security/password-security.service';
import { LocalizationService } from '../../../../services/localization.service';

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
  private readonly novedadesService = inject(NovedadesService);
  private readonly passwordSecurityService = inject(PasswordSecurityService);
  readonly localization = inject(LocalizationService);
  readonly passwordEvaluation = signal<PasswordEvaluation | null>(null);
  readonly referenciaContratacion = signal('');
  readonly validatingContinuation = signal(true);
  readonly continuationError = signal('');
  readonly submitting = signal(false);
  readonly requestError = signal('');
  readonly registrationCompleted = signal(false);
  readonly tokenGestionValidacion = signal('');
  readonly metodoValidacion = signal<'email' | 'telegram' | null>(null);
  readonly preparandoValidacion = signal(false);
  readonly validacionError = signal('');
  readonly preparacionTelegram = signal<PreparacionTelegramResponse | null>(null);
  readonly estadoTelegram = signal<EstadoTelegramRegistroResponse | null>(null);
  readonly codigoTelegram = signal('');
  readonly comandoTelegramCopiado = signal(false);
  readonly validacionCompletada = signal(false);
  readonly showPassword = signal(false);
  readonly showConfirmPassword = signal(false);
  readonly categoriasNewsletter = signal<CategoriaNoticia[]>([]);
  readonly categoriasSeleccionadas = signal<number[]>([]);

  readonly registryForm = this.formBuilder.nonNullable.group({
    firstName: ['', [Validators.required, Validators.minLength(2)]],
    lastName: ['', [Validators.required, Validators.minLength(2)]],
    workEmail: ['', [Validators.required, Validators.email]],
    phone: ['', [Validators.required, Validators.minLength(6)]],
    password: ['', [Validators.required, Validators.minLength(8), Validators.pattern(/^(?=.*[A-Za-z])(?=.*\d).+$/)]],
    confirmPassword: ['', Validators.required],
    acceptsTerms: [false, Validators.requiredTrue],
    recibirNewsletter: [false],
  });

  constructor() {
  this.route.queryParamMap.pipe(takeUntilDestroyed(this.destroyRef)).subscribe((params) => this.validarContinuidad(params.get('referencia')));
  this.novedadesService.categorias().pipe(catchError(() => of([]))).subscribe((categorias) => this.categoriasNewsletter.set(categorias));
  this.registryForm.controls.password.valueChanges.pipe(debounceTime(300), distinctUntilChanged(), switchMap(password => password ? this.passwordSecurityService.evaluar(password).pipe(catchError(() => of(null))) : of(null)), takeUntilDestroyed(this.destroyRef)).subscribe(resultado => this.passwordEvaluation.set(resultado));
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

    if (this.registryForm.controls.recibirNewsletter.value && this.categoriasSeleccionadas().length === 0) {
      this.requestError.set(this.localization.traducir('signup.newsletterCategoryRequired'));
      return;
    }

    const form = this.registryForm.getRawValue();
    const referenciaContratacion = this.referenciaContratacion();

    if (!referenciaContratacion) {
      this.requestError.set('No pudimos validar la contratación asociada a este registro.');
      return;
    }

    const agencia = {
      telefonoContacto: form.phone,
      usuarios: [
        {
          nombre: form.firstName,
          apellido: form.lastName,
          email: form.workEmail,
          passwordHash: form.password,
          aceptaTerminos: form.acceptsTerms,
        },
      ],
    };

    this.submitting.set(true);

    this.passwordSecurityService.evaluar(form.password)
      .pipe(
        tap((evaluacion) => this.passwordEvaluation.set(evaluacion)),
        switchMap((evaluacion) => {
          if (!evaluacion.valida) {
            this.requestError.set('La contraseña no cumple los requisitos de seguridad.');
            return EMPTY;
          }

          return this.authService.registrarAgencia(
            referenciaContratacion,
            agencia,
            form.recibirNewsletter,
            form.recibirNewsletter ? this.categoriasSeleccionadas() : [],
          );
        }),
        finalize(() => this.submitting.set(false)),
      )
      .subscribe({
        next: (respuesta) => {
          this.tokenGestionValidacion.set(respuesta.tokenGestion);
          sessionStorage.setItem(this.claveSesionValidacion(referenciaContratacion), respuesta.tokenGestion);
          this.registrationCompleted.set(true);
        },
        error: (error) => {
          this.requestError.set(
            typeof error.error === 'string'
              ? error.error
              : 'No pudimos registrar la agencia. Revisá los datos e intentá nuevamente.',
          );
        },
      });
  }

  elegirEmail(): void {
    this.preparandoValidacion.set(true); this.validacionError.set(''); this.metodoValidacion.set('email');
    this.authService.enviarValidacionEmailRegistro(this.tokenGestionValidacion()).pipe(finalize(() => this.preparandoValidacion.set(false))).subscribe({ next: () => undefined, error: error => this.validacionError.set(typeof error.error === 'string' ? error.error : 'No fue posible enviar el enlace de validación.') });
  }

  elegirTelegram(): void {
    this.preparandoValidacion.set(true); this.validacionError.set(''); this.comandoTelegramCopiado.set(false); this.metodoValidacion.set('telegram');
    this.authService.prepararValidacionTelegramRegistro(this.tokenGestionValidacion()).pipe(finalize(() => this.preparandoValidacion.set(false))).subscribe({
      next: preparacion => { this.preparacionTelegram.set(preparacion); window.open(preparacion.enlaceProfundo, '_blank', 'noopener'); this.actualizarEstadoTelegram(); },
      error: error => this.validacionError.set(typeof error.error === 'string' ? error.error : 'No fue posible iniciar la validación por Telegram.'),
    });
  }

  actualizarEstadoTelegram(): void {
    this.authService.consultarValidacionTelegramRegistro(this.tokenGestionValidacion()).subscribe({ next: estado => this.estadoTelegram.set(estado), error: () => this.validacionError.set('No fue posible actualizar el estado de Telegram.') });
  }

  confirmarTelegram(): void {
    const codigo = this.codigoTelegram().replace(/\s/g, '');
    if (!/^\d{6}$/.test(codigo)) { this.validacionError.set('Ingresá el código de seis dígitos enviado por Telegram.'); return; }
    this.preparandoValidacion.set(true); this.validacionError.set('');
    this.authService.confirmarValidacionTelegramRegistro(this.tokenGestionValidacion(), codigo).pipe(finalize(() => this.preparandoValidacion.set(false))).subscribe({ next: () => { sessionStorage.removeItem(this.claveSesionValidacion(this.referenciaContratacion())); this.validacionCompletada.set(true); }, error: error => this.validacionError.set(typeof error.error === 'string' ? error.error : 'No fue posible validar el código de Telegram.') });
  }

  reenviarTelegram(): void {
    this.preparandoValidacion.set(true); this.validacionError.set('');
    this.authService.reenviarValidacionTelegramRegistro(this.tokenGestionValidacion()).pipe(finalize(() => this.preparandoValidacion.set(false))).subscribe({ next: () => undefined, error: error => this.validacionError.set(typeof error.error === 'string' ? error.error : 'No fue posible reenviar el código de Telegram.') });
  }

  copiarComandoTelegram(): void {
    const comando = this.preparacionTelegram()?.comandoInicio;
    if (!comando) return;
    navigator.clipboard.writeText(comando)
      .then(() => this.comandoTelegramCopiado.set(true))
      .catch(() => this.validacionError.set('No fue posible copiar el comando. Copialo manualmente desde el recuadro.'));
  }

  volverAMetodosValidacion(): void {
    this.metodoValidacion.set(null); this.validacionError.set(''); this.codigoTelegram.set(''); this.comandoTelegramCopiado.set(false);
  }

  hasError(controlName: keyof typeof this.registryForm.controls): boolean {
    const control = this.registryForm.controls[controlName];
    return control.touched && control.invalid;
  }

  alternarCategoriaNewsletter(idCategoria: number): void {
    this.categoriasSeleccionadas.update((actuales) =>
      actuales.includes(idCategoria)
        ? actuales.filter((id) => id !== idCategoria)
        : [...actuales, idCategoria],
    );
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
          const tokenGestion = sessionStorage.getItem(this.claveSesionValidacion(contratacion.referencia));
          if (tokenGestion) { this.tokenGestionValidacion.set(tokenGestion); this.registrationCompleted.set(true); }
        },
        error: () => {
          this.continuationError.set('No encontramos una contratación aprobada asociada a este registro.');
        },
      });
  }

  private claveSesionValidacion(referenciaContratacion: string): string {
    return `registro-validacion-${referenciaContratacion}`;
  }
}
