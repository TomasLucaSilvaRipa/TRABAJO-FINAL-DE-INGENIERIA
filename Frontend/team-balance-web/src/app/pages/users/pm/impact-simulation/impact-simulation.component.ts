import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Tarea, TareaOpciones, TasksService } from '../../../../services/tasks.service';
import { ImpactSimulationService, SimulacionImpacto } from '../../../../services/impact-simulation.service';

@Component({
  selector: 'app-impact-simulation',
  imports: [FormsModule],
  templateUrl: './impact-simulation.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ImpactSimulationComponent {
  private readonly tasksService = inject(TasksService); private readonly simulationService = inject(ImpactSimulationService);
  readonly tareas = signal<Tarea[]>([]); readonly empleados = signal<TareaOpciones['empleados']>([]); readonly idTarea = signal(0); readonly idEmpleado = signal(0); readonly resultado = signal<SimulacionImpacto | null>(null); readonly error = signal('');
  constructor() { this.tasksService.consultar().subscribe({ next: (tareas: Tarea[]) => this.tareas.set(tareas.filter((tarea: Tarea) => tarea.activo)), error: () => this.error.set('No se pudieron cargar las tareas.') }); this.tasksService.opciones().subscribe({ next: (opciones: TareaOpciones) => this.empleados.set(opciones.empleados), error: () => this.error.set('No se pudieron cargar los recursos.') }); }
  simular(): void { if (!this.idTarea() || !this.idEmpleado()) { this.error.set('Seleccioná una tarea y un recurso candidato.'); return; } this.error.set(''); this.simulationService.simular({ idTarea: this.idTarea(), idEmpleadoCandidato: this.idEmpleado() }).subscribe({ next: (resultado: SimulacionImpacto) => this.resultado.set(resultado), error: error => this.error.set(error?.error || 'No se pudo crear la simulación.') }); }
}
