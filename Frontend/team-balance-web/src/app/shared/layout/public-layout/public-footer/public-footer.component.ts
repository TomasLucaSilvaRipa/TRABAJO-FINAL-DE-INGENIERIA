import { ChangeDetectionStrategy, Component } from '@angular/core';
import { RouterLink } from '@angular/router';

@Component({
  selector: 'app-public-footer',
  imports: [RouterLink],
  templateUrl: './public-footer.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class PublicFooterComponent {}
