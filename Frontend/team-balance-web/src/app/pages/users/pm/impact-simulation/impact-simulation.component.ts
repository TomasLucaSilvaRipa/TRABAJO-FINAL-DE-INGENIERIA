import { DatePipe } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute } from '@angular/router';
import { BestFitService, RecomendacionBestFit } from '../../../../services/best-fit.service';
import { ImpactSimulationService, SimulacionImpacto } from '../../../../services/impact-simulation.service';
import { LocalizationService } from '../../../../services/localization.service';
import { Tarea, TareaOpciones, TasksService } from '../../../../services/tasks.service';
import { EscenarioVisualSimulacion, SimulationImpactChartComponent } from '../../../../shared/components/analytics/simulation-impact-chart/simulation-impact-chart.component';

@Component({
  selector: 'app-impact-simulation',
  imports: [DatePipe, FormsModule, SimulationImpactChartComponent],
  templateUrl: './impact-simulation.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ImpactSimulationComponent {
  private readonly simulationService = inject(ImpactSimulationService);
  private readonly tasksService = inject(TasksService);
  private readonly bestFitService = inject(BestFitService);
  private readonly route = inject(ActivatedRoute);
  readonly localization = inject(LocalizationService);

  readonly tareas = signal<Tarea[]>([]);
  readonly empleados = signal<TareaOpciones['empleados']>([]);
  readonly escenarios = signal<SimulacionImpacto[]>([]);
  readonly recomendaciones = signal<RecomendacionBestFit[]>([]);
  readonly idTarea = signal(0);
  readonly idEmpleado = signal(0);
  readonly cargando = signal(false);
  readonly cargandoConservadas = signal(false);
  readonly error = signal('');
  readonly mensaje = signal('');
  readonly Math = Math;
  readonly escenariosVisuales = computed<EscenarioVisualSimulacion[]>(() => this.escenarios().map(escenario => ({
    idEmpleado: escenario.idEmpleadoCandidato,
    nombreEmpleado: this.nombreEmpleado(escenario.idEmpleadoCandidato, escenario.nombreEmpleado),
    nombreTarea: escenario.nombreTarea ?? this.tareas().find(tarea => tarea.id === escenario.idTarea)?.titulo ?? this.texto('selectedTask'),
    nombreProyecto: escenario.nombreProyecto ?? this.texto('project'),
    ocupacionActual: Number(escenario.porcentajeOcupacionActual ?? 0),
    ocupacionProyectada: Number(escenario.porcentajeOcupacionProyectado ?? 0),
    cargaActual: Number(escenario.cargaActual ?? 0),
    cargaProyectada: Number(escenario.cargaProyectada ?? 0),
    disponibilidadRestante: Number(escenario.disponibilidadRestante ?? 0),
    impacto: this.impactoTraducido(escenario.impactoOperativo),
    conservado: escenario.id > 0,
  })));

  constructor() {
    this.cargarTareas();
    this.cargarEmpleados();
  }

  seleccionarTarea(idTarea: number): void {
    this.idTarea.set(idTarea);
    this.escenarios.set([]);
    this.recomendaciones.set([]);
    this.mensaje.set('');
    this.error.set('');
    if (idTarea) { this.cargarConservadas(idTarea); }
  }

  simular(): void {
    if (!this.idTarea() || !this.idEmpleado()) {
      this.error.set('private.impactSimulation.selectTaskAndCandidate');
      return;
    }

    this.calcularEscenario(this.idEmpleado());
  }

  usarRecursoBestFit(): void {
    if (!this.idTarea()) {
      this.error.set('private.impactSimulation.selectTaskForBestFit');
      return;
    }

    this.limpiarMensajes();
    this.cargando.set(true);

    this.bestFitService.sugerir({ idTarea: this.idTarea() }).subscribe({
      next: (recomendaciones: RecomendacionBestFit[]) => {
        this.recomendaciones.set(recomendaciones);
        this.cargando.set(false);

        if (!recomendaciones.length) {
          this.error.set('private.impactSimulation.noBestFitCandidates');
          return;
        }

        this.idEmpleado.set(recomendaciones[0].idEmpleadoSugerido);
        this.calcularEscenario(recomendaciones[0].idEmpleadoSugerido);
      },
      error: (respuesta) => {
        this.cargando.set(false);
        this.error.set(this.obtenerMensajeError(respuesta, 'bestFitError'));
      },
    });
  }

  conservar(escenario: SimulacionImpacto): void {
    this.limpiarMensajes();
    this.cargando.set(true);

    this.simulationService.conservar(escenario).subscribe({
      next: (resultado: SimulacionImpacto) => {
        this.reemplazarEscenario(resultado);
        this.cargando.set(false);
        this.mensaje.set('private.impactSimulation.saveSuccess');
      },
      error: (respuesta) => {
        this.cargando.set(false);
        this.error.set(this.obtenerMensajeError(respuesta, 'saveError'));
      },
    });
  }

  descartar(escenario: SimulacionImpacto): void {
    this.limpiarMensajes();
    this.cargando.set(true);

    this.simulationService.descartar(escenario).subscribe({
      next: () => {
        this.escenarios.update((escenarios: SimulacionImpacto[]) => escenarios.filter((item: SimulacionImpacto) => item.idEmpleadoCandidato !== escenario.idEmpleadoCandidato));
        this.cargando.set(false);
        this.mensaje.set('private.impactSimulation.discardSuccess');
      },
      error: (respuesta) => {
        this.cargando.set(false);
        this.error.set(this.obtenerMensajeError(respuesta, 'discardError'));
      },
    });
  }

  nombreEmpleado(idEmpleado: number, nombreGuardado?: string): string {
    const empleado = this.empleados().find((item) => item.id === idEmpleado);
    return empleado ? `${empleado.nombre} ${empleado.apellido}` : nombreGuardado || this.texto('selectedCandidate');
  }

  horasEstimadasTarea(escenario: SimulacionImpacto): number { return Number(this.tareas().find(tarea => tarea.id === escenario.idTarea)?.horasEstimadas ?? 0); }

  texto(clave: string): string {
    return this.localization.traducir(`private.impactSimulation.${clave}`);
  }

  impactoTraducido(valor?: string | null): string {
    const claves: Record<string, string> = {
      'sobrecarga': 'overload',
      'capacidad comprometida': 'capacityAtRisk',
      'viable': 'viable'
    };
    return this.texto(claves[valor?.trim().toLowerCase() ?? ''] ?? 'unclassified');
  }

  prioridadTraducida(valor?: string | null): string {
    const claves: Record<string, string> = { baja: 'low', media: 'medium', alta: 'high' };
    const clave = claves[valor?.trim().toLowerCase() ?? ''];
    return clave ? this.localization.traducir(`taskManager.${clave}`) : valor || this.texto('undefined');
  }

  nivelCandidato(escenario: SimulacionImpacto): string {
    if (!escenario.tieneSkillRequerida) return this.texto('skillMissing');
    const nivel = escenario.nivelSkillCandidato?.trim();
    const claves: Record<string, string> = {
      'básico': 'levelBasic', basico: 'levelBasic', medio: 'levelIntermediate',
      intermedio: 'levelIntermediate', avanzado: 'levelAdvanced'
    };
    const clave = claves[nivel?.toLowerCase() ?? ''];
    return clave ? this.texto(clave) : nivel || this.texto('noSkillLevel');
  }

  advertencias(escenario: SimulacionImpacto): string[] {
    try {
      const advertencias: unknown = JSON.parse(escenario.advertenciasJson || '[]');
      if (!Array.isArray(advertencias)) return [];
      return advertencias.filter((item): item is string => typeof item === 'string').map(item => {
        const ausencia = /^El empleado tiene (\d+) día\(s\) de ausencia aprobada/i.exec(item);
        if (ausencia) return this.texto('warningAbsence').replace('{days}', ausencia[1]);
        const skill = /^El empleado no tiene registrada la skill requerida:\s*(.*)\.$/i.exec(item);
        if (skill) return this.texto('warningSkill').replace('{skill}', () => skill[1]);
        if (item.startsWith('La asignación supera')) return this.texto('warningOverload');
        if (item.startsWith('La asignación deja')) return this.texto('warningCapacity');
        if (item.startsWith('El deadline')) return this.texto('warningDeadline');
        if (item.startsWith('Las horas estimadas')) return this.texto('warningDeadlineCapacity');
        return item;
      });
    }
    catch {
      return [];
    }
  }

  compatibilidad(idEmpleado: number): RecomendacionBestFit | undefined {
    return this.recomendaciones().find((item: RecomendacionBestFit) => item.idEmpleadoSugerido === idEmpleado);
  }

  private cargarTareas(): void {
    this.simulationService.consultarTareasDisponibles().subscribe({
      next: (tareas: Tarea[]) => {
        this.tareas.set(tareas);
        const idTareaRuta = Number(this.route.snapshot.queryParamMap.get('tarea'));

        if (idTareaRuta && tareas.some((tarea: Tarea) => tarea.id === idTareaRuta)) {
          this.idTarea.set(idTareaRuta);
          this.cargarConservadas(idTareaRuta);
        }
      },
      error: (respuesta) => this.error.set(this.obtenerMensajeError(respuesta, 'loadTasksError')),
    });
  }

  private cargarEmpleados(): void {
    this.tasksService.opciones().subscribe({
      next: (opciones: TareaOpciones) => this.empleados.set(opciones.empleados),
      error: (respuesta) => this.error.set(this.obtenerMensajeError(respuesta, 'loadCandidatesError')),
    });
  }

  private cargarConservadas(idTarea: number): void {
    this.cargandoConservadas.set(true);
    this.simulationService.consultarConservadas(idTarea).subscribe({
      next: escenarios => { this.escenarios.set(escenarios); this.cargandoConservadas.set(false); },
      error: respuesta => { this.cargandoConservadas.set(false); this.error.set(this.obtenerMensajeError(respuesta, 'loadSavedError')); },
    });
  }

  private calcularEscenario(idEmpleado: number): void {
    this.limpiarMensajes();
    this.cargando.set(true);

    this.simulationService.calcular({ idTarea: this.idTarea(), idEmpleadoCandidato: idEmpleado }).subscribe({
      next: (resultado: SimulacionImpacto) => {
        this.reemplazarEscenario(resultado);
        this.cargando.set(false);
      },
      error: (respuesta) => {
        this.cargando.set(false);
        this.error.set(this.obtenerMensajeError(respuesta, 'calculateError'));
      },
    });
  }

  private reemplazarEscenario(resultado: SimulacionImpacto): void {
    this.escenarios.update((escenarios: SimulacionImpacto[]) => {
      const sinMismoCandidato = escenarios.filter((item: SimulacionImpacto) => item.idEmpleadoCandidato !== resultado.idEmpleadoCandidato);
      return [...sinMismoCandidato, resultado];
    });
  }

  private limpiarMensajes(): void {
    this.error.set('');
    this.mensaje.set('');
  }

  private obtenerMensajeError(respuesta: HttpErrorResponse, alternativa: string): string {
    const detalle = typeof respuesta.error === 'string' ? respuesta.error : '';
    if (respuesta.status === 0) return 'projectForm.connection';
    if (/sesi[oó]n ya no es v[aá]lida/i.test(detalle) || (respuesta.status === 401 && !detalle)) return 'projectForm.invalidSession';
    if (alternativa === 'bestFitError' && respuesta.status === 403 && /no pertenece a uno de tus proyectos/i.test(detalle)) return 'bestFit.taskUnavailable';
    if (/tarea no est[aá] disponible|no pertenece a uno de tus proyectos/i.test(detalle)) return 'private.impactSimulation.taskUnavailable';
    if (respuesta.status === 401 || respuesta.status === 403) return 'projectForm.notAllowed';
    if (/seleccion[aá] una tarea pendiente/i.test(detalle)) return 'private.impactSimulation.selectTaskAndCandidate';
    if (/tarea seleccionada no es v[aá]lida/i.test(detalle)) return 'private.impactSimulation.invalidTask';
    if (/no se encontr[oó] (el escenario|una simulaci[oó]n activa)/i.test(detalle)) return 'private.impactSimulation.scenarioNotFound';
    if (/skill requerida/i.test(detalle)) return 'private.impactSimulation.bestFitSkillRequired';
    if (/seniority requerido/i.test(detalle)) return 'private.impactSimulation.bestFitSeniorityRequired';
    if (/fecha de inicio/i.test(detalle)) return 'private.impactSimulation.bestFitStartRequired';
    if (/horas estimadas/i.test(detalle)) return 'private.impactSimulation.bestFitHoursRequired';
    if (/deadline.*venci[oó]/i.test(detalle)) return 'private.impactSimulation.warningDeadline';
    if (/debe tener un deadline/i.test(detalle)) return 'private.impactSimulation.bestFitDeadlineRequired';
    return `private.impactSimulation.${alternativa}`;
  }
}
