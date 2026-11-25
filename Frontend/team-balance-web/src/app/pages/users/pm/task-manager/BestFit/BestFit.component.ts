import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { timeout } from 'rxjs';
import { Tarea, TasksService } from '../../../../../services/tasks.service';
import { LocalizationService } from '../../../../../services/localization.service';
import { BestFitService, RecomendacionBestFit } from '../../../../../services/best-fit.service';

@Component({
  selector: 'app-best-fit',
  imports: [FormsModule, DatePipe],
  templateUrl: './BestFit.component.html',
  styleUrl: './BestFit.component.css',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class BestFit {
  private readonly tasksService = inject(TasksService);
  private readonly bestFitService = inject(BestFitService);
  readonly localization = inject(LocalizationService);
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
      const seniority = recomendacion.seniorityEmpleado?.replace(/\s+/g, '').toLowerCase();
      const coincideSeniority = !this.seniorityFiltro() || seniority === this.seniorityFiltro().toLowerCase();
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
      error: () => this.error.set(this.texto('loadError')),
    });
  }

  buscar(): void {
    const idTarea = this.tareaSeleccionada();
    if (!idTarea) { this.error.set(this.texto('selectTask')); return; }
    this.cargando.set(true);
    this.error.set('');
    this.mensaje.set('');
    this.detalle.set(null);
    this.bestFitService.sugerir({ idTarea }).pipe(timeout(15000)).subscribe({
      next: (recomendaciones: RecomendacionBestFit[]) => {
        if (this.tareaSeleccionada() !== idTarea) { return; }
        this.recomendaciones.set(recomendaciones);
        this.cargando.set(false);
        if (!recomendaciones.length) { this.mensaje.set(this.texto('empty')); }
      },
      error: error => {
        if (this.tareaSeleccionada() !== idTarea) return;
        this.cargando.set(false);
        const detalle = typeof error?.error === 'string' ? error.error : '';
        if (error?.status === 403) {
          const clave = /no pertenece a uno de tus proyectos/i.test(detalle)
            ? 'bestFit.taskUnavailable'
            : 'projectForm.notAllowed';
          this.error.set(this.localization.traducir(clave));
          return;
        }
        this.error.set(error?.name === 'TimeoutError'
          ? this.texto('searchTimeout')
          : detalle || error?.error?.message || error?.error?.title || this.texto('searchError'));
      },
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
        this.mensaje.set(this.texto('savedCandidate').replace('{name}', () => recomendacion.nombreEmpleado));
        this.recomendaciones.set(this.recomendaciones().map((item: RecomendacionBestFit) => ({ ...item, seleccionadaPorPM: item.seleccionadaPorPM || item.idEmpleadoSugerido === recomendacion.idEmpleadoSugerido, estado: item.idEmpleadoSugerido === recomendacion.idEmpleadoSugerido ? 'Recomendada' : item.estado })));
      },
      error: error => { if (this.tareaSeleccionada() === idTarea) { this.guardando.set(0); this.error.set(error?.name === 'TimeoutError' ? this.texto('saveTimeout') : typeof error?.error === 'string' ? error.error : error?.error?.message || this.texto('saveError')); } },
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
    if (tarea.seniorityRequerido) { return tarea.seniorityRequerido; }
    // Compatibilidad visual para tareas previas a la carga inicial de seniority.
    if (tarea.complejidad === 'Alta') { return 'Senior'; }
    if (tarea.complejidad === 'Media') { return 'Semisenior'; }
    return 'Junior';
  }

  aporteSemanalEstimado(recomendacion: RecomendacionBestFit): number {
    const aporte = recomendacion.horasDisponibles - recomendacion.horasComprometidasSemanales - recomendacion.margenHorasSemanales;
    return Math.max(0, Math.round(aporte * 100) / 100);
  }

  colorCompatibilidad(puntaje: number): string { return puntaje >= 80 ? '#39d7b0' : puntaje >= 60 ? '#f2bd4b' : '#ff6b82'; }

  estadoCompatibilidad(puntaje: number): string { return puntaje >= 80 ? this.texto('highMatch') : puntaje >= 60 ? this.texto('moderateMatch') : this.texto('needsReview'); }

  texto(clave: string): string {
    return this.localization.traducir(`bestFit.${clave}`);
  }

  seniorityTexto(seniority: string): string {
    return /^semi\s*senior$/i.test(seniority) ? this.texto('midLevel') : seniority;
  }

  textoMotor(valor: string | null | undefined, alternativa: string): string {
    if (!valor) return this.texto(alternativa);
    const frases = {
      'Cuenta con la skill requerida.': 'hasSkill',
      'Tiene margen operativo holgado para cumplir el deadline.': 'ampleCapacity',
      'Su historial comparable se mantuvo dentro de las horas estimadas.': 'historyOnBudget',
      'El margen operativo es limitado para absorber imprevistos.': 'limitedMargin',
      'Hay poca evidencia histórica comparable; se aplica un valor neutral.': 'limitedHistory',
      'En tareas comparables registró más horas que las estimadas.': 'historyOverBudget',
    };
    for (const [frase, clave] of Object.entries(frases)) {
      valor = valor.replace(frase, () => this.texto(clave));
    }
    return valor;
  }

  private esElegibleParaBestFit(tarea: Tarea): boolean {
    if (!tarea.fechaInicio || !tarea.deadline) { return false; }
    const deadline = new Date(tarea.deadline);
    const hoy = new Date();
    deadline.setHours(0, 0, 0, 0);
    hoy.setHours(0, 0, 0, 0);
    return tarea.activo && !!tarea.idSkillRequerido && !!this.nivelExperienciaConsiderado(tarea) && tarea.horasEstimadas > 0 && !Number.isNaN(deadline.getTime()) && deadline >= hoy;
  }
}
