import { ChangeDetectionStrategy, Component } from '@angular/core';
import { RouterModule } from '@angular/router';

@Component({
  selector: 'app-subscription',
  imports: [RouterModule],
  templateUrl: './subscription-manager.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class Subscription {}
