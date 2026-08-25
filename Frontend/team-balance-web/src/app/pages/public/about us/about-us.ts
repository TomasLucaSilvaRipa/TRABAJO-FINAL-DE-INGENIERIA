import { ChangeDetectionStrategy, Component } from '@angular/core';
import { RouterLink } from '@angular/router';

@Component({
  selector: 'app-us',
  imports: [RouterLink],
  templateUrl: './about-us.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AboutUsComponent {}
