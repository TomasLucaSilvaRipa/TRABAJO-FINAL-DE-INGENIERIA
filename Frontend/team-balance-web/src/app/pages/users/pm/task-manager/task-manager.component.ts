import { ChangeDetectionStrategy, Component } from '@angular/core';
import { RouterModule } from '@angular/router';

@Component({
  selector: 'app-task-manager.component',
  imports: [RouterModule],
  templateUrl: './task-manager.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class TaskManagerComponent {}
