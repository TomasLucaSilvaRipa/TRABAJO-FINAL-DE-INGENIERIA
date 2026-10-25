import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, Validators } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { EMPTY, finalize, switchMap } from 'rxjs';
import { AuthService, EstadoDosFactoresResponse, EstadoTelegramResponse, PreparacionTelegramResponse, PreparacionTotpResponse } from '../../../services/auth.service';
import { PageBreadcrumbComponent } from '../../../shared/components/common/page-breadcrumb/page-breadcrumb.component';
import { LocalizationService } from '../../../services/localization.service';
import { PasswordEvaluation, PasswordSecurityService } from '../../../services/security/password-security.service';
import QRCode from 'qrcode';

@Component({
  selector: 'app-change-password',
  imports: [DatePipe, PageBreadcrumbComponent, ReactiveFormsModule, RouterLink],
  templateUrl: './change-password.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ChangePasswordComponent {
  private readonly formBuilder = inject(FormBuilder);
  private readonly authService = inject(AuthService);
  private readonly passwordSecurityService = inject(PasswordSecurityService);
  readonly localization = inject(LocalizationService);

  readonly submitting = signal(false);
  readonly requestError = signal('');
  readonly responseMessage = signal('');
  readonly estadoDosFactores = signal<EstadoDosFactoresResponse | null>(null);
  readonly preparacionTotp = signal<PreparacionTotpResponse | null>(null);
  readonly qrTotp = signal('');
  readonly codigoTotp = signal('');
  readonly codigosRecuperacion = signal<string[]>([]);
  readonly procesandoTotp = signal(false);
  readonly errorTotp = signal('');
  readonly mostrandoDesactivacionTotp = signal(false);
  readonly codigoDesactivacionTotp = signal('');
  readonly estadoTelegram = signal<EstadoTelegramResponse | null>(null);
  readonly preparacionTelegram = signal<PreparacionTelegramResponse | null>(null);
  readonly codigoTelegram = signal('');
  readonly procesandoTelegram = signal(false);
  readonly errorTelegram = signal('');
  readonly esperandoDesvinculacionTelegram = signal(false);

  readonly changePasswordForm = this.formBuilder.nonNullable.group({
    passwordActual: ['', Validators.required],
    password: ['', [Validators.required, Validators.minLength(8)]],
    repetirPassword: ['', Validators.required],
  });

  constructor() { this.consultarDosFactores(); this.consultarTelegram(); }

  consultarDosFactores(): void {
    this.authService.consultarDosFactores().subscribe({ next: estado => this.estadoDosFactores.set(estado), error: () => this.errorTotp.set('No fue posible consultar el estado de Google Authenticator.') });
  }

  async activarTotp(): Promise<void> {
    this.procesandoTotp.set(true); this.errorTotp.set(''); this.codigosRecuperacion.set([]);
    this.authService.prepararTotp().pipe(finalize(() => this.procesandoTotp.set(false))).subscribe({
      next: async preparacion => {
        this.preparacionTotp.set(preparacion);
        try { this.qrTotp.set(await QRCode.toDataURL(preparacion.uriOtpAuth, { width: 232, margin: 1, errorCorrectionLevel: 'M' })); }
        catch { this.errorTotp.set('No fue posible generar el código QR. Usá la clave manual para configurar la aplicación.'); }
      },
      error: error => this.errorTotp.set(typeof error.error === 'string' ? error.error : 'No fue posible preparar Google Authenticator.'),
    });
  }

  confirmarTotp(): void {
    const codigo = this.codigoTotp().replace(/\s/g, '');
    if (!/^\d{6}$/.test(codigo)) { this.errorTotp.set('Ingresá el código de seis dígitos de la aplicación.'); return; }
    this.procesandoTotp.set(true); this.errorTotp.set('');
    this.authService.confirmarTotp(codigo).pipe(finalize(() => this.procesandoTotp.set(false))).subscribe({
      next: respuesta => { this.codigosRecuperacion.set(respuesta.codigosRecuperacion); this.preparacionTotp.set(null); this.qrTotp.set(''); this.codigoTotp.set(''); this.consultarDosFactores(); },
      error: error => this.errorTotp.set(typeof error.error === 'string' ? error.error : 'No fue posible confirmar Google Authenticator.'),
    });
  }

  cancelarTotp(): void { this.preparacionTotp.set(null); this.qrTotp.set(''); this.codigoTotp.set(''); this.errorTotp.set(''); }

  desactivarTotp(): void {
    const codigo = this.codigoDesactivacionTotp().replace(/\s/g, '');
    if (!codigo) { this.errorTotp.set('Ingresá un código de Authenticator o un código de recuperación.'); return; }
    this.procesandoTotp.set(true); this.errorTotp.set('');
    this.authService.desactivarTotp(codigo).pipe(finalize(() => this.procesandoTotp.set(false))).subscribe({
      next: () => { this.mostrandoDesactivacionTotp.set(false); this.codigoDesactivacionTotp.set(''); this.consultarDosFactores(); },
      error: error => this.errorTotp.set(typeof error.error === 'string' ? error.error : 'No fue posible desactivar Google Authenticator.'),
    });
  }

  consultarTelegram(): void {
    this.authService.consultarTelegram().subscribe({ next: estado => this.estadoTelegram.set(estado), error: () => this.errorTelegram.set('No fue posible consultar el estado de Telegram.') });
  }

  vincularTelegram(): void {
    this.procesandoTelegram.set(true); this.errorTelegram.set(''); this.codigoTelegram.set('');
    this.authService.prepararVinculacionTelegram().pipe(finalize(() => this.procesandoTelegram.set(false))).subscribe({
      next: preparacion => { this.preparacionTelegram.set(preparacion); window.open(preparacion.enlaceProfundo, '_blank', 'noopener'); this.consultarTelegram(); },
      error: error => this.errorTelegram.set(typeof error.error === 'string' ? error.error : 'No fue posible iniciar la vinculación con Telegram.'),
    });
  }

  confirmarTelegram(): void {
    const codigo = this.codigoTelegram().replace(/\s/g, '');
    if (!/^\d{6}$/.test(codigo)) { this.errorTelegram.set('Ingresá el código de seis dígitos enviado por Telegram.'); return; }
    this.procesandoTelegram.set(true); this.errorTelegram.set('');
    this.authService.confirmarVinculacionTelegram(codigo).pipe(finalize(() => this.procesandoTelegram.set(false))).subscribe({
      next: () => { this.preparacionTelegram.set(null); this.codigoTelegram.set(''); this.consultarTelegram(); },
      error: error => this.errorTelegram.set(typeof error.error === 'string' ? error.error : 'No fue posible confirmar el código de Telegram.'),
    });
  }

  reenviarTelegram(): void {
    this.procesandoTelegram.set(true); this.errorTelegram.set('');
    this.authService.reenviarCodigoTelegram().pipe(finalize(() => this.procesandoTelegram.set(false))).subscribe({ next: () => undefined, error: error => this.errorTelegram.set(typeof error.error === 'string' ? error.error : 'No fue posible reenviar el código.') });
  }

  solicitarDesvinculacionTelegram(): void {
    this.procesandoTelegram.set(true); this.errorTelegram.set('');
    this.authService.prepararDesvinculacionTelegram().pipe(finalize(() => this.procesandoTelegram.set(false))).subscribe({ next: () => { this.esperandoDesvinculacionTelegram.set(true); this.codigoTelegram.set(''); }, error: error => this.errorTelegram.set(typeof error.error === 'string' ? error.error : 'No fue posible solicitar la desvinculación.') });
  }

  confirmarDesvinculacionTelegram(): void {
    const codigo = this.codigoTelegram().replace(/\s/g, '');
    if (!/^\d{6}$/.test(codigo)) { this.errorTelegram.set('Ingresá el código de seis dígitos enviado por Telegram.'); return; }
    this.procesandoTelegram.set(true); this.errorTelegram.set('');
    this.authService.confirmarDesvinculacionTelegram(codigo).pipe(finalize(() => this.procesandoTelegram.set(false))).subscribe({ next: () => { this.esperandoDesvinculacionTelegram.set(false); this.codigoTelegram.set(''); this.consultarTelegram(); }, error: error => this.errorTelegram.set(typeof error.error === 'string' ? error.error : 'No fue posible desvincular Telegram.') });
  }

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

    this.passwordSecurityService.evaluar(form.password)
      .pipe(
        switchMap((evaluacion) => {
          if (!evaluacion.valida) {
            this.requestError.set(this.mensajePoliticaPassword(evaluacion));
            return EMPTY;
          }
          return this.authService.cambiarPassword(form.passwordActual, form.password);
        }),
        finalize(() => this.submitting.set(false)),
      )
      .subscribe({
        next: (respuesta) => {
          this.authService.logOut();
          this.responseMessage.set(respuesta.mensaje);
        },
        error: (error) => this.requestError.set(typeof error.error === 'string' ? error.error : 'No fue posible modificar la contraseña. Intentá nuevamente.'),
      });
  }

  private mensajePoliticaPassword(evaluacion: PasswordEvaluation): string {
    const faltantes: string[] = [];
    if (!evaluacion.requisitos.longitud) faltantes.push('al menos 8 caracteres');
    if (!evaluacion.requisitos.mayuscula) faltantes.push('una mayúscula');
    if (!evaluacion.requisitos.minuscula) faltantes.push('una minúscula');
    if (!evaluacion.requisitos.numero) faltantes.push('un número');
    if (!evaluacion.requisitos.caracterEspecial) faltantes.push('un signo');
    return `La nueva contraseña debe incluir ${faltantes.join(', ')}.`;
  }
}
