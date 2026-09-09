import { ChangeDetectionStrategy, Component } from '@angular/core';

@Component({
  selector: 'app-kanban-board',
  imports: [],
  templateUrl: './KanbanBoard.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class KanbanBoard {}
