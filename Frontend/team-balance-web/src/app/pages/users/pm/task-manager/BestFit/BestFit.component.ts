import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Tarea, TasksService } from '../../../../../services/tasks.service';
import { BestFitService, RecomendacionBestFit } from '../../../../../services/best-fit.service';

@Component({
  selector: 'app-best-fit',
  imports: [FormsModule, DatePipe],
  templateUrl: './BestFit.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class BestFit {
  private readonly tasksService = inject(TasksService); private readonly bestFitService = inject(BestFitService);
  readonly tareas = signal<Tarea[]>([]); readonly recomendaciones = signal<RecomendacionBestFit[]>([]); readonly tareaSeleccionada = signal(0); readonly cargando = signal(false); readonly asignando = signal(0); readonly error = signal(''); readonly mensaje = signal('');
  constructor() { this.tasksService.consultar().subscribe({ next: (tareas: Tarea[]) => this.tareas.set(tareas.filter((tarea: Tarea) => tarea.activo)), error: () => this.error.set('No se pudieron cargar las tareas.') }); }
  buscar(): void { const idTarea = this.tareaSeleccionada(); if (!idTarea) { this.error.set('Seleccioná una tarea.'); return; } this.cargando.set(true); this.error.set(''); this.bestFitService.sugerir({ idTarea }).subscribe({ next: (recomendaciones: RecomendacionBestFit[]) => { this.recomendaciones.set(recomendaciones); this.cargando.set(false); }, error: error => { this.error.set(typeof error?.error === 'string' ? error.error : error?.error?.message || error?.error?.title || 'No se pudieron generar sugerencias.'); this.cargando.set(false); } }); }
  asignar(recomendacion: RecomendacionBestFit): void { this.asignando.set(recomendacion.id); this.error.set(''); this.bestFitService.confirmarAsignacion(recomendacion).subscribe({ next: () => { this.asignando.set(0); this.mensaje.set(`${recomendacion.nombreEmpleado} fue asignado a la tarea.`); this.recomendaciones.set(this.recomendaciones().map(item => ({ ...item, seleccionadaPorPM: item.id === recomendacion.id, estado: item.id === recomendacion.id ? 'Confirmada' : 'Descartada' }))); }, error: error => { this.asignando.set(0); this.error.set(typeof error?.error === 'string' ? error.error : error?.error?.message || 'No se pudo confirmar la asignación.'); } }); }
}
