import { ChangeDetectionStrategy, Component, inject, input, signal } from '@angular/core';
import { LanguageCode, LocalizationService } from '../../../services/localization.service';

@Component({
  selector: 'app-language-selector',
  imports: [],
  templateUrl: './language-selector.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class LanguageSelector {
  readonly variant = input<'light' | 'dark'>('light');
  readonly compact = input(false);

  readonly localization = inject(LocalizationService);
  protected readonly abierto = signal(false);

  protected alternar(): void {
    this.abierto.update((valor) => !valor);
  }

  protected cambiarIdioma(idioma: LanguageCode): void {
    this.localization.cambiarIdioma(idioma);
    this.abierto.set(false);
  }

  protected cerrar(): void {
    this.abierto.set(false);
  }

  protected etiquetaIdioma(): string {
    return this.localization.language() === 'es' ? 'ES' : 'EN';
  }
}
