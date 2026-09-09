import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { RouterLink } from '@angular/router';
import { LocalizationService } from '../../../../services/localization.service';

@Component({
  selector: 'app-public-footer',
  imports: [RouterLink],
  templateUrl: './public-footer.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class PublicFooterComponent {
  readonly localization = inject(LocalizationService);
}
