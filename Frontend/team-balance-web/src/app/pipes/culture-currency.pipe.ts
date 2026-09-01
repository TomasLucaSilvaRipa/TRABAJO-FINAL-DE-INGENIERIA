import { Pipe, type PipeTransform } from '@angular/core';
import { inject } from '@angular/core';
import { LocalizationService } from '../services/localization.service';

@Pipe({
  name: 'appCultureCurrencyPipe',
  standalone: true,
  pure: true,
})

export class CultureCurrencyPipe implements PipeTransform {
  private readonly localization = inject(LocalizationService);
  transform(value: number | null | undefined,currency:string='USD'): string {
    if (value === null || value === undefined) {
      return '-';
    }
    return this.localization.formatearMoneda(value, currency);
  }
}
