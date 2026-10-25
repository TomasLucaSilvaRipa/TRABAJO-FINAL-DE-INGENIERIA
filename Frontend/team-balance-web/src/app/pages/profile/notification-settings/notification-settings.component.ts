import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { firstValueFrom } from 'rxjs';
import { AuthService } from '../../../services/auth.service';
import { LocalizationService } from '../../../services/localization.service';
import { OperationalEmailPreferences, OperationalEmailService } from '../../../services/operational-email.service';

@Component({
  selector: 'app-notification-settings', standalone: true, imports: [FormsModule],
  templateUrl: './notification-settings.component.html', styleUrl: './notification-settings.component.css',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class NotificationSettingsComponent {
  private readonly service = inject(OperationalEmailService);
  readonly auth = inject(AuthService);
  readonly localization = inject(LocalizationService);
  readonly loading = signal(true); readonly saving = signal(false); readonly error = signal<string | null>(null); readonly success = signal(false);
  preferences: OperationalEmailPreferences = { idUsuario: 0, resumenDiarioActivo: false, horaResumenDiario: '09:00', alertasDeadlineActivo: false, resumenSemanalActivo: false };

  constructor() { void this.load(); }
  get isEmployee(): boolean { return this.auth.tieneTipoUsuario('Empleado'); }
  get isOwner(): boolean { return this.auth.tieneTipoUsuario('Dueno'); }

  async save(): Promise<void> {
    this.saving.set(true); this.error.set(null); this.success.set(false);
    try {
      const payload = { ...this.preferences, horaResumenDiario: this.normalizeTime(this.preferences.horaResumenDiario) };
      this.preferences = await firstValueFrom(this.service.savePreferences(payload));
      this.preferences.horaResumenDiario = this.preferences.horaResumenDiario.slice(0, 5);
      this.success.set(true);
    } catch (error: unknown) { this.error.set(this.message(error)); }
    finally { this.saving.set(false); }
  }

  private async load(): Promise<void> {
    try { this.preferences = await firstValueFrom(this.service.getPreferences()); this.preferences.horaResumenDiario = this.preferences.horaResumenDiario.slice(0, 5); }
    catch (error: unknown) { this.error.set(this.message(error)); }
    finally { this.loading.set(false); }
  }
  private normalizeTime(value: string): string { return /^\d{2}:\d{2}$/.test(value) ? `${value}:00` : value; }
  private message(error: unknown): string {
    const candidate = error as { error?: string | { message?: string }; message?: string };
    return typeof candidate.error === 'string' ? candidate.error : candidate.error?.message || candidate.message || this.localization.traducir('private.emailSettings.error');
  }
}
