import { ChangeDetectionStrategy, Component } from '@angular/core';
import { RouterLink } from '@angular/router';
import { LocalizationService } from '../../../services/localization.service';
import {CultureCurrencyPipe} from '../../../pipes/culture-currency.pipe';
@Component({
  selector: 'app-plans',
  imports: [RouterLink, CultureCurrencyPipe],
  templateUrl: './plans.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class PlansComponent {}
