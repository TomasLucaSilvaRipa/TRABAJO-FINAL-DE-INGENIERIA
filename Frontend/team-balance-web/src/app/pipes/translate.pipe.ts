import { Pipe, type PipeTransform } from '@angular/core';
import { inject } from '@angular/core';
import { LanguageCode, LocalizationService } from '../services/localization.service';

@Pipe({
  name: 'appTranslatePipe',
  standalone: true,
  pure: true,
})
export class TranslatePipe implements PipeTransform {
  private readonly localization = inject(LocalizationService);
  transform(key: string, language: LanguageCode): string {
    return this.localization.traducir(key, language);
  }

}
