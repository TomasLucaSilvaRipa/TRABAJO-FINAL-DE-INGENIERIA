import { Pipe, type PipeTransform } from '@angular/core';
import { inject } from '@angular/core';
import { LocalizationService } from '../services/localization.service';

@Pipe({
  name: 'appCultureDatePipe',
  standalone: true,
  pure: true,
})

export class CultureDatePipePipe implements PipeTransform {
  private readonly localization = inject(LocalizationService);
  transform(value: Date | string | null | undefined): string {
    if (!value) {
      return '-';
    }
    return this.localization.formatearFechaHora(value);
  }
}
