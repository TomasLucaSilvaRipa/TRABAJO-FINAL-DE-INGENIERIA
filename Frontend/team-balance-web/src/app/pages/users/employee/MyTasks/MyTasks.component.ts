import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { RouterModule } from '@angular/router';
import { LocalizationService } from '../../../../services/localization.service';
import { Tarea, TasksService } from '../../../../services/tasks.service';

@Component({
  selector: 'app-my-tasks.component',
  imports: [CommonModule, RouterModule],
  templateUrl: './MyTasks.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class MyTasksComponent {
  private readonly tasksService = inject(TasksService);
  readonly localization = inject(LocalizationService);
  readonly tareas = signal<Tarea[]>([]); readonly cargando = signal(true); readonly error = signal('');
  constructor() { this.tasksService.consultarMias().subscribe({ next: (tareas: Tarea[]) => { this.tareas.set(tareas); this.cargando.set(false); }, error: () => { this.error.set('No se pudieron cargar tus tareas asignadas.'); this.cargando.set(false); } }); }
}
