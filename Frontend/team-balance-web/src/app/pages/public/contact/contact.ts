import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { LocalizationService } from '../../../services/localization.service';

@Component({
  selector: 'app-contact',
  imports: [],
  templateUrl: './contact.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ContactComponent {
  readonly localization = inject(LocalizationService);
}
