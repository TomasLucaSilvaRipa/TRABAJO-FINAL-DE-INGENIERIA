import { Pipe, type PipeTransform } from '@angular/core';
import { inject } from '@angular/core';
import { LocalizationService } from '../services/localization.service';

@Pipe({
  name: 'appCultureDatePipe',
  standalone: true,
  pure: false,
})

export class CultureDatePipe implements PipeTransform {
  private readonly localization = inject(LocalizationService);
  transform(value: Date | string | null | undefined, includeTime = false): string {
    if (!value) {
      return '-';
    }
    return includeTime ? this.localization.formatearFechaHora(value) : this.localization.formatearFecha(value);
  }
}
