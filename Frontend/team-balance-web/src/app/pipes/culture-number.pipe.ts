import { Pipe, type PipeTransform } from '@angular/core';
import {inject} from '@angular/core';
import { LocalizationService } from '../services/localization.service';

@Pipe({
  name: 'appCultureNumberPipe',
  standalone: true,
  pure: true,
})

export class CultureNumberPipePipe implements PipeTransform {
  private readonly localization = inject(LocalizationService);
  transform(value: number | null | undefined): string {
    if (value === null || value === undefined) {
      return '-';
    }
    return this.localization.formatearNumero(value);
  }

}
