import { DatePipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute } from '@angular/router';
import { BestFitService, RecomendacionBestFit } from '../../../../services/best-fit.service';
import { ImpactSimulationService, SimulacionImpacto } from '../../../../services/impact-simulation.service';
import { Tarea, TareaOpciones, TasksService } from '../../../../services/tasks.service';

@Component({
  selector: 'app-impact-simulation',
  imports: [DatePipe, FormsModule],
  templateUrl: './impact-simulation.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ImpactSimulationComponent {
  private readonly simulationService = inject(ImpactSimulationService);
  private readonly tasksService = inject(TasksService);
  private readonly bestFitService = inject(BestFitService);
  private readonly route = inject(ActivatedRoute);

  readonly tareas = signal<Tarea[]>([]);
  readonly empleados = signal<TareaOpciones['empleados']>([]);
  readonly escenarios = signal<SimulacionImpacto[]>([]);
  readonly recomendaciones = signal<RecomendacionBestFit[]>([]);
  readonly idTarea = signal(0);
  readonly idEmpleado = signal(0);
  readonly cargando = signal(false);
  readonly error = signal('');
  readonly mensaje = signal('');
  readonly Math = Math;

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
  }

  simular(): void {
    if (!this.idTarea() || !this.idEmpleado()) {
      this.error.set('Seleccioná una tarea pendiente y un recurso candidato.');
      return;
    }

    this.calcularEscenario(this.idEmpleado());
  }

  usarRecursoBestFit(): void {
    if (!this.idTarea()) {
      this.error.set('Seleccioná una tarea antes de consultar Best Fit.');
      return;
    }

    this.limpiarMensajes();
    this.cargando.set(true);

    this.bestFitService.sugerir({ idTarea: this.idTarea() }).subscribe({
      next: (recomendaciones: RecomendacionBestFit[]) => {
        this.recomendaciones.set(recomendaciones);
        this.cargando.set(false);

        if (!recomendaciones.length) {
          this.error.set('Best Fit no encontró recursos compatibles para esta tarea.');
          return;
        }

        this.idEmpleado.set(recomendaciones[0].idEmpleadoSugerido);
        this.calcularEscenario(recomendaciones[0].idEmpleadoSugerido);
      },
      error: (respuesta) => {
        this.cargando.set(false);
        this.error.set(this.obtenerMensajeError(respuesta, 'No se pudieron obtener las sugerencias de Best Fit.'));
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
        this.mensaje.set('Simulación generada correctamente. La tarea no fue asignada ni modificada.');
      },
      error: (respuesta) => {
        this.cargando.set(false);
        this.error.set(this.obtenerMensajeError(respuesta, 'No se pudo conservar la simulación.'));
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
        this.mensaje.set('El escenario fue descartado. La tarea y las cargas reales permanecen sin cambios.');
      },
      error: (respuesta) => {
        this.cargando.set(false);
        this.error.set(this.obtenerMensajeError(respuesta, 'No se pudo descartar la simulación.'));
      },
    });
  }

  nombreEmpleado(idEmpleado: number): string {
    const empleado = this.empleados().find((item) => item.id === idEmpleado);
    return empleado ? `${empleado.nombre} ${empleado.apellido}` : 'Recurso seleccionado';
  }

  advertencias(escenario: SimulacionImpacto): string[] {
    try {
      const advertencias = JSON.parse(escenario.advertenciasJson || '[]');
      return Array.isArray(advertencias) ? advertencias : [];
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
        }
      },
      error: (respuesta) => this.error.set(this.obtenerMensajeError(respuesta, 'No se pudieron cargar las tareas pendientes del PM.')),
    });
  }

  private cargarEmpleados(): void {
    this.tasksService.opciones().subscribe({
      next: (opciones: TareaOpciones) => this.empleados.set(opciones.empleados),
      error: (respuesta) => this.error.set(this.obtenerMensajeError(respuesta, 'No se pudieron cargar los recursos.')),
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
        this.error.set(this.obtenerMensajeError(respuesta, 'No se pudo calcular el impacto.'));
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

  private obtenerMensajeError(respuesta: any, mensajePredeterminado: string): string {
    return typeof respuesta?.error === 'string' ? respuesta.error : mensajePredeterminado;
  }
}
