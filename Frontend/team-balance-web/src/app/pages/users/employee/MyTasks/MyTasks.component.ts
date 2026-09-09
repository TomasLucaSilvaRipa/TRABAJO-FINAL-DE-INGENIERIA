import { ChangeDetectionStrategy, Component } from '@angular/core';

@Component({
  selector: 'app-my-tasks.component',
  imports: [],
  templateUrl: './MyTasks.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class MyTasksComponent {}
