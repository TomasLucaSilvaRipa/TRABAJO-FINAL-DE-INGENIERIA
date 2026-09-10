import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { RouterModule } from '@angular/router';
import { LocalizationService } from '../../../../services/localization.service';

@Component({
  selector: 'app-availability',
  imports: [RouterModule],
  templateUrl: './availability.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AvailabilityComponent { readonly localization = inject(LocalizationService); }
