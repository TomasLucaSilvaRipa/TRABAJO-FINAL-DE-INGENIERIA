import { ChangeDetectionStrategy, Component, ElementRef, inject, signal, viewChild } from '@angular/core';
import { ReactiveFormsModule, FormBuilder, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { EMPTY, finalize, switchMap } from 'rxjs';
import { AuthService, EstadoDosFactoresResponse, EstadoTelegramResponse, PreparacionTelegramResponse, PreparacionTotpResponse } from '../../../services/auth.service';
import { PageBreadcrumbComponent } from '../../../shared/components/common/page-breadcrumb/page-breadcrumb.component';
import { LocalizationService } from '../../../services/localization.service';
import { PasswordEvaluation, PasswordSecurityService } from '../../../services/security/password-security.service';
import QRCode from 'qrcode';
import { RemoteSessionsComponent } from './remote-sessions.component';
import { BackupEmailComponent } from './backup-email.component';

@Component({
  selector: 'app-change-password',
  imports: [PageBreadcrumbComponent, ReactiveFormsModule, RouterLink, RemoteSessionsComponent, BackupEmailComponent],
  templateUrl: './change-password.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ChangePasswordComponent {
  private readonly formBuilder = inject(FormBuilder);
  private readonly authService = inject(AuthService);
  private readonly router = inject(Router);
  private readonly passwordSecurityService = inject(PasswordSecurityService);
  private readonly campoPasswordActual = viewChild<ElementRef<HTMLInputElement>>('campoPasswordActual');
  readonly localization = inject(LocalizationService);

  readonly submitting = signal(false);
  readonly cerrandoSesion = signal(false);
  readonly errorCierreSesion = signal(false);
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

  private mensajeError(error: unknown, clave: string): string {
    const mensajeServidor = (error as { error?: unknown } | null)?.error;
    if (this.localization.language() === 'es' && typeof mensajeServidor === 'string') return mensajeServidor;
    return this.localization.traducir(clave);
  }

  constructor() { this.consultarDosFactores(); this.consultarTelegram(); }

  vencimientoSesionActual(): string | null {
    const vencimiento = this.authService.obtenerVencimientoToken();
    return vencimiento && Number.isFinite(Date.parse(vencimiento)) ? vencimiento : null;
  }

  cerrarSesionActual(): void {
    if (this.cerrandoSesion() || this.submitting()) return;

    this.cerrandoSesion.set(true);
    this.errorCierreSesion.set(false);
    this.authService.cerrarSesion().pipe(finalize(() => this.cerrandoSesion.set(false))).subscribe({
      next: () => {
        this.authService.logOut();
        void this.router.navigate(['/signin']);
      },
      error: () => this.errorCierreSesion.set(true),
    });
  }

  irACambioDePassword(): void {
    this.campoPasswordActual()?.nativeElement.focus();
  }

  consultarDosFactores(): void {
    this.authService.consultarDosFactores().subscribe({ next: estado => this.estadoDosFactores.set(estado), error: error => this.errorTotp.set(this.mensajeError(error, 'private.security.error.consultTotp')) });
  }

  async activarTotp(): Promise<void> {
    this.procesandoTotp.set(true); this.errorTotp.set(''); this.codigosRecuperacion.set([]);
    this.authService.prepararTotp().pipe(finalize(() => this.procesandoTotp.set(false))).subscribe({
      next: async preparacion => {
        this.preparacionTotp.set(preparacion);
        try { this.qrTotp.set(await QRCode.toDataURL(preparacion.uriOtpAuth, { width: 232, margin: 1, errorCorrectionLevel: 'M' })); }
        catch { this.errorTotp.set(this.localization.traducir('private.security.error.qrGeneration')); }
      },
      error: error => this.errorTotp.set(this.mensajeError(error, 'private.security.error.prepareTotp')),
    });
  }

  confirmarTotp(): void {
    const codigo = this.codigoTotp().replace(/\s/g, '');
    if (!/^\d{6}$/.test(codigo)) { this.errorTotp.set(this.localization.traducir('private.security.error.enterSixDigitCode')); return; }
    this.procesandoTotp.set(true); this.errorTotp.set('');
    this.authService.confirmarTotp(codigo).pipe(finalize(() => this.procesandoTotp.set(false))).subscribe({
      next: respuesta => { this.codigosRecuperacion.set(respuesta.codigosRecuperacion); this.preparacionTotp.set(null); this.qrTotp.set(''); this.codigoTotp.set(''); this.consultarDosFactores(); },
      error: error => this.errorTotp.set(this.mensajeError(error, 'private.security.error.confirmTotp')),
    });
  }

  cancelarTotp(): void { this.preparacionTotp.set(null); this.qrTotp.set(''); this.codigoTotp.set(''); this.errorTotp.set(''); }

  desactivarTotp(): void {
    const codigo = this.codigoDesactivacionTotp().replace(/\s/g, '');
    if (!codigo) { this.errorTotp.set(this.localization.traducir('private.security.error.enterAuthenticatorOrRecovery')); return; }
    this.procesandoTotp.set(true); this.errorTotp.set('');
    this.authService.desactivarTotp(codigo).pipe(finalize(() => this.procesandoTotp.set(false))).subscribe({
      next: () => { this.mostrandoDesactivacionTotp.set(false); this.codigoDesactivacionTotp.set(''); this.consultarDosFactores(); },
      error: error => this.errorTotp.set(this.mensajeError(error, 'private.security.error.disableTotp')),
    });
  }

  consultarTelegram(): void {
    this.authService.consultarTelegram().subscribe({ next: estado => this.estadoTelegram.set(estado), error: error => this.errorTelegram.set(this.mensajeError(error, 'private.security.error.consultTelegram')) });
  }

  vincularTelegram(): void {
    this.procesandoTelegram.set(true); this.errorTelegram.set(''); this.codigoTelegram.set('');
    this.authService.prepararVinculacionTelegram().pipe(finalize(() => this.procesandoTelegram.set(false))).subscribe({
      next: preparacion => { this.preparacionTelegram.set(preparacion); window.open(preparacion.enlaceProfundo, '_blank', 'noopener'); this.consultarTelegram(); },
      error: error => this.errorTelegram.set(this.mensajeError(error, 'private.security.error.linkTelegram')),
    });
  }

  confirmarTelegram(): void {
    const codigo = this.codigoTelegram().replace(/\s/g, '');
    if (!/^\d{6}$/.test(codigo)) { this.errorTelegram.set(this.localization.traducir('private.security.error.enterTelegramCode')); return; }
    this.procesandoTelegram.set(true); this.errorTelegram.set('');
    this.authService.confirmarVinculacionTelegram(codigo).pipe(finalize(() => this.procesandoTelegram.set(false))).subscribe({
      next: () => { this.preparacionTelegram.set(null); this.codigoTelegram.set(''); this.consultarTelegram(); },
      error: error => this.errorTelegram.set(this.mensajeError(error, 'private.security.error.confirmTelegram')),
    });
  }

  reenviarTelegram(): void {
    this.procesandoTelegram.set(true); this.errorTelegram.set('');
    this.authService.reenviarCodigoTelegram().pipe(finalize(() => this.procesandoTelegram.set(false))).subscribe({ next: () => undefined, error: error => this.errorTelegram.set(this.mensajeError(error, 'private.security.error.resendTelegram')) });
  }

  solicitarDesvinculacionTelegram(): void {
    this.procesandoTelegram.set(true); this.errorTelegram.set('');
    this.authService.prepararDesvinculacionTelegram().pipe(finalize(() => this.procesandoTelegram.set(false))).subscribe({ next: () => { this.esperandoDesvinculacionTelegram.set(true); this.codigoTelegram.set(''); }, error: error => this.errorTelegram.set(this.mensajeError(error, 'private.security.error.prepareUnlinkTelegram')) });
  }

  confirmarDesvinculacionTelegram(): void {
    const codigo = this.codigoTelegram().replace(/\s/g, '');
    if (!/^\d{6}$/.test(codigo)) { this.errorTelegram.set(this.localization.traducir('private.security.error.enterTelegramCode')); return; }
    this.procesandoTelegram.set(true); this.errorTelegram.set('');
    this.authService.confirmarDesvinculacionTelegram(codigo).pipe(finalize(() => this.procesandoTelegram.set(false))).subscribe({ next: () => { this.esperandoDesvinculacionTelegram.set(false); this.codigoTelegram.set(''); this.consultarTelegram(); }, error: error => this.errorTelegram.set(this.mensajeError(error, 'private.security.error.unlinkTelegram')) });
  }

  onSubmit(): void {
    if (this.cerrandoSesion() || this.submitting()) return;

    this.requestError.set('');
    this.responseMessage.set('');
    this.changePasswordForm.markAllAsTouched();

    const form = this.changePasswordForm.getRawValue();

    if (this.changePasswordForm.invalid || form.password !== form.repetirPassword)
    {
      this.requestError.set(this.localization.traducir('security.mismatch'));
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
        next: () => {
          this.authService.logOut();
          this.responseMessage.set(this.localization.traducir('security.changed'));
        },
        error: (error) => this.requestError.set(this.mensajeError(error, 'security.error')),
      });
  }

  private mensajePoliticaPassword(evaluacion: PasswordEvaluation): string {
    const faltantes: string[] = [];
    if (!evaluacion.requisitos.longitud) faltantes.push(this.localization.traducir('private.security.password.minimumLength'));
    if (!evaluacion.requisitos.mayuscula) faltantes.push(this.localization.traducir('private.security.password.uppercase'));
    if (!evaluacion.requisitos.minuscula) faltantes.push(this.localization.traducir('private.security.password.lowercase'));
    if (!evaluacion.requisitos.numero) faltantes.push(this.localization.traducir('private.security.password.number'));
    if (!evaluacion.requisitos.caracterEspecial) faltantes.push(this.localization.traducir('private.security.password.specialCharacter'));
    return this.localization.traducir('private.security.password.requirements').replace('{requirements}', faltantes.join(', '));
  }
}
