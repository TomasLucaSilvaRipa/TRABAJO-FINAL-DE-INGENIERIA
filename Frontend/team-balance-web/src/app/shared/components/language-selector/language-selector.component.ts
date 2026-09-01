import { ChangeDetectionStrategy, Component,inject } from '@angular/core';
import { LocalizationService, LanguageCode } from '../../../services/localization.service';

@Component({
  selector: 'app-language-selector',
  imports: [],
  templateUrl: './language-selector.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class LanguageSelector {
  readonly localization = inject(LocalizationService);
  cambiarIdioma(event: Event):void {
    const select = event.target as HTMLSelectElement;
    this.localization.cambiarIdioma(select.value as LanguageCode);
  }

}
