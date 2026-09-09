import { ChangeDetectionStrategy, Component } from '@angular/core';

@Component({
  selector: 'app-task-manager.component',
  imports: [],
  templateUrl: './task-manager.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class TaskManagerComponent {}
