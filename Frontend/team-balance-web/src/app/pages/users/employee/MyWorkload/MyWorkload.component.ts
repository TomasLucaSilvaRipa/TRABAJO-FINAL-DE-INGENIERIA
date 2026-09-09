import { ChangeDetectionStrategy, Component } from '@angular/core';

@Component({
  selector: 'app-my-workload',
  imports: [],
  templateUrl: './MyWorkload.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class MyWorkload {}
