import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { CommonModule, DatePipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { Tarea, TasksService } from '../../../../services/tasks.service';
import { Proyecto, ProjectsService } from '../../../../services/projects.service';

@Component({ selector: 'app-project-kanban', imports: [CommonModule, DatePipe, FormsModule, RouterLink], templateUrl: './project-kanban.component.html', changeDetection: ChangeDetectionStrategy.OnPush })
export class ProjectKanbanComponent {
  private readonly tasksService = inject(TasksService);
  private readonly projectsService = inject(ProjectsService);
  readonly tareas = signal<Tarea[]>([]);
  readonly proyectos = signal<Proyecto[]>([]);
  readonly proyectoSeleccionado = signal<number | null>(null);
  readonly error = signal('');
  readonly columnas = ['Pendiente', 'En progreso', 'Bloqueada', 'Finalizada'];

  constructor() {
    this.projectsService.consultar().subscribe({ next: (proyectos: Proyecto[]) => this.proyectos.set(proyectos), error: () => this.error.set('No se pudieron cargar los proyectos disponibles.') });
    this.cargarTareas();
  }

  cargarTareas(): void {
    this.error.set('');
    this.tasksService.filtrar({ idProyecto: this.proyectoSeleccionado() }).subscribe({ next: (tareas: Tarea[]) => this.tareas.set(tareas), error: error => this.error.set(typeof error?.error === 'string' ? error.error : error?.error?.message || 'No se pudieron cargar las tareas de los proyectos disponibles.') });
  }

  seleccionarProyecto(idProyecto: string): void {
    this.proyectoSeleccionado.set(idProyecto ? Number(idProyecto) : null);
    this.cargarTareas();
  }

  porEstado(estado: string): Tarea[] { return this.tareas().filter((tarea: Tarea) => (tarea.estado || 'Pendiente').toLowerCase() === estado.toLowerCase()); }
}
