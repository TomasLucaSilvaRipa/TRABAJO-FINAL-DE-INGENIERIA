import { ChangeDetectionStrategy, Component } from '@angular/core';
import { RouterModule } from '@angular/router';

@Component({
  selector: 'app-availability',
  imports: [RouterModule],
  templateUrl: './availability.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AvailabilityComponent {}
