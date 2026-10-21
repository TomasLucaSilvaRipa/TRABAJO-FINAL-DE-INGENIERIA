import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { timeout } from 'rxjs';
import { Tarea, TasksService } from '../../../../../services/tasks.service';
import { BestFitService, RecomendacionBestFit } from '../../../../../services/best-fit.service';

@Component({
  selector: 'app-best-fit',
  imports: [FormsModule, DatePipe],
  templateUrl: './BestFit.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class BestFit {
  private readonly tasksService = inject(TasksService);
  private readonly bestFitService = inject(BestFitService);
  readonly tareas = signal<Tarea[]>([]);
  readonly recomendaciones = signal<RecomendacionBestFit[]>([]);
  readonly tareaSeleccionada = signal(0);
  readonly cargando = signal(false);
  readonly guardando = signal(0);
  readonly seniorityFiltro = signal('');
  readonly disponibilidadMinima = signal<number | null>(null);
  readonly cargaMaxima = signal<number | null>(null);
  readonly ordenamiento = signal<'compatibilidad' | 'disponibilidad' | 'carga'>('compatibilidad');
  readonly detalle = signal<RecomendacionBestFit | null>(null);
  readonly error = signal('');
  readonly mensaje = signal('');
  readonly tareaActual = computed(() => this.tareas().find((tarea: Tarea) => tarea.id === this.tareaSeleccionada()) ?? null);
  readonly recomendacionesVisibles = computed(() => {
    let resultado = this.recomendaciones().filter((recomendacion: RecomendacionBestFit) => {
      const coincideSeniority = !this.seniorityFiltro() || recomendacion.seniorityEmpleado === this.seniorityFiltro();
      const coincideDisponibilidad = this.disponibilidadMinima() === null || recomendacion.horasDisponibles >= this.disponibilidadMinima()!;
      const coincideCarga = this.cargaMaxima() === null || recomendacion.cargaActualPorcentaje <= this.cargaMaxima()!;
      return coincideSeniority && coincideDisponibilidad && coincideCarga;
    });
    if (this.ordenamiento() === 'disponibilidad') { return resultado.sort((primero: RecomendacionBestFit, segundo: RecomendacionBestFit) => segundo.horasDisponibles - primero.horasDisponibles); }
    if (this.ordenamiento() === 'carga') { return resultado.sort((primero: RecomendacionBestFit, segundo: RecomendacionBestFit) => primero.cargaActualPorcentaje - segundo.cargaActualPorcentaje); }
    return resultado.sort((primero: RecomendacionBestFit, segundo: RecomendacionBestFit) => segundo.puntajeCompatibilidad - primero.puntajeCompatibilidad);
  });

  constructor() {
    this.tasksService.consultarPorPM().subscribe({
      next: (tareas: Tarea[]) => this.tareas.set(tareas.filter((tarea: Tarea) => this.esElegibleParaBestFit(tarea))),
      error: () => this.error.set('No se pudieron cargar las tareas disponibles de tus proyectos.'),
    });
  }

  buscar(): void {
    const idTarea = this.tareaSeleccionada();
    if (!idTarea) { this.error.set('Seleccioná una tarea.'); return; }
    this.cargando.set(true);
    this.error.set('');
    this.mensaje.set('');
    this.detalle.set(null);
    this.bestFitService.sugerir({ idTarea }).pipe(timeout(15000)).subscribe({
      next: (recomendaciones: RecomendacionBestFit[]) => {
        if (this.tareaSeleccionada() !== idTarea) { return; }
        this.recomendaciones.set(recomendaciones);
        this.cargando.set(false);
        if (!recomendaciones.length) { this.mensaje.set('No hay empleados que puedan asumir esta tarea sin comprometer el deadline.'); }
      },
      error: error => { if (this.tareaSeleccionada() === idTarea) { this.error.set(error?.name === 'TimeoutError' ? 'La búsqueda demoró demasiado. Verificá que la API esté iniciada e intentá nuevamente.' : typeof error?.error === 'string' ? error.error : error?.error?.message || error?.error?.title || 'No se pudieron generar sugerencias.'); this.cargando.set(false); } },
    });
  }

  conservar(recomendacion: RecomendacionBestFit): void {
    const idTarea = recomendacion.idTarea;
    this.guardando.set(recomendacion.idEmpleadoSugerido);
    this.error.set('');
    this.bestFitService.guardarRecomendacion(recomendacion).pipe(timeout(15000)).subscribe({
      next: () => {
        if (this.tareaSeleccionada() !== idTarea) { return; }
        this.guardando.set(0);
        this.mensaje.set(`${recomendacion.nombreEmpleado} quedó registrado como recurso recomendado. La tarea todavía no fue asignada.`);
        this.recomendaciones.set(this.recomendaciones().map((item: RecomendacionBestFit) => ({ ...item, seleccionadaPorPM: item.seleccionadaPorPM || item.idEmpleadoSugerido === recomendacion.idEmpleadoSugerido, estado: item.idEmpleadoSugerido === recomendacion.idEmpleadoSugerido ? 'Recomendada' : item.estado })));
      },
      error: error => { if (this.tareaSeleccionada() === idTarea) { this.guardando.set(0); this.error.set(error?.name === 'TimeoutError' ? 'La operación demoró demasiado. Verificá que la API esté iniciada e intentá nuevamente.' : typeof error?.error === 'string' ? error.error : error?.error?.message || 'No se pudo conservar la recomendación.'); } },
    });
  }

  limpiarFiltros(): void {
    this.seniorityFiltro.set('');
    this.disponibilidadMinima.set(null);
    this.cargaMaxima.set(null);
  }

  seleccionarTarea(idTarea: number): void {
    this.tareaSeleccionada.set(idTarea);
    this.recomendaciones.set([]);
    this.detalle.set(null);
    this.error.set('');
    this.mensaje.set('');
    this.cargando.set(false);
    this.guardando.set(0);
  }

  nivelExperienciaConsiderado(tarea: Tarea | null): string {
    if (!tarea) { return ''; }
    if (tarea.complejidad === 'Alta') { return 'Senior'; }
    if (tarea.complejidad === 'Media') { return 'Semi Senior'; }
    return 'Junior';
  }

  aporteSemanalEstimado(recomendacion: RecomendacionBestFit): number {
    const aporte = recomendacion.horasDisponibles - recomendacion.horasComprometidasSemanales - recomendacion.margenHorasSemanales;
    return Math.max(0, Math.round(aporte * 100) / 100);
  }

  private esElegibleParaBestFit(tarea: Tarea): boolean {
    if (!tarea.fechaInicio || !tarea.deadline) { return false; }
    const deadline = new Date(tarea.deadline);
    const hoy = new Date();
    deadline.setHours(0, 0, 0, 0);
    hoy.setHours(0, 0, 0, 0);
    return tarea.activo && !!tarea.idSkillRequerido && !!tarea.complejidad && tarea.horasEstimadas > 0 && !Number.isNaN(deadline.getTime()) && deadline >= hoy;
  }
}
