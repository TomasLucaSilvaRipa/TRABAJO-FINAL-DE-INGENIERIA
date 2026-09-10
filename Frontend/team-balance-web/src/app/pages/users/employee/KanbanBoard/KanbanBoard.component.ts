import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { Tarea, TasksService } from '../../../../services/tasks.service';
import { LocalizationService } from '../../../../services/localization.service';

@Component({
  selector: 'app-kanban-board',
  imports: [CommonModule],
  templateUrl: './KanbanBoard.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class KanbanBoard {
  private readonly tasksService = inject(TasksService);
  readonly localization = inject(LocalizationService);
  readonly tareas = signal<Tarea[]>([]); readonly cargando = signal(true); readonly error = signal('');
  readonly columnas = ['Pendiente', 'En curso', 'En revisión', 'Finalizado'];
  constructor() { this.tasksService.consultarMias().subscribe({ next: (tareas: Tarea[]) => { this.tareas.set(tareas); this.cargando.set(false); }, error: () => { this.error.set('No se pudo cargar el tablero personal.'); this.cargando.set(false); } }); }
  tareasPorEstado(estado: string): Tarea[] { return this.tareas().filter((tarea: Tarea) => (tarea.estado || 'Pendiente').toLowerCase() === estado.toLowerCase()); }
}
