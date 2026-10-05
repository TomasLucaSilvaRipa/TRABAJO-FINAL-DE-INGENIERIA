import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { CommonModule, DatePipe } from '@angular/common';
import { RouterLink } from '@angular/router';
import { Tarea, TasksService } from '../../../../services/tasks.service';

@Component({ selector: 'app-project-kanban', imports: [CommonModule, DatePipe, RouterLink], templateUrl: './project-kanban.component.html', changeDetection: ChangeDetectionStrategy.OnPush })
export class ProjectKanbanComponent {
  private readonly tasksService = inject(TasksService); readonly tareas = signal<Tarea[]>([]); readonly error = signal(''); readonly columnas = ['Pendiente', 'En progreso', 'Bloqueada', 'Finalizada'];
  constructor() { this.tasksService.consultarPorPM().subscribe({ next: (tareas: Tarea[]) => this.tareas.set(tareas), error: error => this.error.set(typeof error?.error === 'string' ? error.error : error?.error?.message || 'No se pudieron cargar las tareas de tus proyectos.') }); }
  porEstado(estado: string): Tarea[] { return this.tareas().filter((tarea: Tarea) => (tarea.estado || 'Pendiente').toLowerCase() === estado.toLowerCase()); }
}
