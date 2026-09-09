import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { RouterLink } from '@angular/router';
import { LocalizationService } from '../../../services/localization.service';

@Component({
  selector: 'app-us',
  imports: [RouterLink],
  templateUrl: './about-us.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AboutUsComponent {
  readonly localization = inject(LocalizationService);
}
