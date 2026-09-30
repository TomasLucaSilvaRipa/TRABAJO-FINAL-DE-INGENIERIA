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
  readonly columnas = ['Pendiente', 'En progreso', 'Bloqueada', 'Finalizada'];
  constructor() { this.tasksService.consultarMias().subscribe({ next: (tareas: Tarea[]) => { this.tareas.set(tareas); this.cargando.set(false); }, error: () => { this.error.set('No se pudo cargar el tablero personal.'); this.cargando.set(false); } }); }
  tareasPorEstado(estado: string): Tarea[] { return this.tareas().filter((tarea: Tarea) => (tarea.estado || 'Pendiente').toLowerCase() === estado.toLowerCase()); }
  cambiarEstado(tarea: Tarea, estado: string): void { this.tasksService.cambiarEstadoPropio({ id: tarea.id, estado }).subscribe({ next: () => { const tareas = this.tareas().map(item => item.id === tarea.id ? { ...item, estado } : item); this.tareas.set(tareas); }, error: error => this.error.set(error?.error || 'No se pudo actualizar el estado de la tarea.') }); }
  siguienteEstado(tarea: Tarea): string | null { const indice = this.columnas.indexOf(tarea.estado || 'Pendiente'); return indice >= 0 && indice < this.columnas.length - 1 ? this.columnas[indice + 1] : null; }
}
