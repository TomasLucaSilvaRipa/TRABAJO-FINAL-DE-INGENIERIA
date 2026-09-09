import { ChangeDetectionStrategy, Component } from '@angular/core';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-impact-simulation',
  imports: [CommonModule],
  templateUrl: './impact-simulation.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ImpactSimulationComponent {
  readonly indicadores = [
    { nombre: 'Capacidad disponible', valor: '18 h', detalle: 'Esta semana', color: 'text-emerald-600' },
    { nombre: 'Riesgo de sobrecarga', valor: 'Bajo', detalle: 'Sin desvíos críticos', color: 'text-cyan-600' },
    { nombre: 'Tareas afectadas', valor: '0', detalle: 'Con la simulación actual', color: 'text-slate-700 dark:text-slate-200' },
  ];
}
