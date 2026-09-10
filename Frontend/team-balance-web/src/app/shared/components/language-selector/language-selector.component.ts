import { ChangeDetectionStrategy, Component, inject, input } from '@angular/core';
import { LanguageCode, LocalizationService } from '../../../services/localization.service';

@Component({
  selector: 'app-language-selector',
  imports: [],
  templateUrl: './language-selector.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class LanguageSelector {
  readonly variant = input<'light' | 'dark'>('light');

  readonly localization = inject(LocalizationService);

  cambiarIdioma(event: Event): void {
    const select = event.target as HTMLSelectElement;
    this.localization.cambiarIdioma(select.value as LanguageCode);
  }
}
