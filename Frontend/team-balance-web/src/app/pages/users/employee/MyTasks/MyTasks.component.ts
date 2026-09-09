import { ChangeDetectionStrategy, Component } from '@angular/core';
import { RouterModule } from '@angular/router';

@Component({
  selector: 'app-my-tasks.component',
  imports: [RouterModule],
  templateUrl: './MyTasks.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class MyTasksComponent {}
