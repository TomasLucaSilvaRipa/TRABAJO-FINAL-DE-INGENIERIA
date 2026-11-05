import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { DecimalPipe } from '@angular/common';
import { AnaliticaOperativaService, OcupacionEmpleado } from '../../../../services/analitica-operativa.service';
import { LocalizationService } from '../../../../services/localization.service';

@Component({
  selector: 'app-my-workload',
  imports: [DecimalPipe],
  templateUrl: './MyWorkload.component.html',
  styleUrl: './MyWorkload.component.css',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class MyWorkload {
  readonly localization = inject(LocalizationService);
  estadoTraducido(estado: string): string {
    const claves: Record<string, string> = { Subutilizado: 'underutilized', Balanceado: 'balanced', Sobrecargado: 'overloaded', 'Crítico': 'critical' };
    return claves[estado] ? this.localization.traducir(`private.workload.state.${claves[estado]}`) : this.localization.traducirEstado(estado);
  }
  traducirCausa(texto: string): string {
    if (texto === 'No tenés tareas planificables asignadas durante esta semana.') return this.localization.traducir('private.workload.noTasks');
    const sobrecarga = texto.match(/^La tarea '(.*)' concentra una sobrecarga en al menos un día hábil\.$/);
    const principal = texto.match(/^La tarea '(.*)' concentra la mayor parte de tu carga semanal\.$/);
    const coincidencia = sobrecarga || principal;
    return coincidencia ? this.localization.traducir(sobrecarga ? 'private.workload.dailyOverload' : 'private.workload.mainTask').replace('{task}', coincidencia[1]) : texto;
  }
  traducirAccion(texto: string): string {
    if (texto === 'Revisá tus prioridades con tu responsable si necesitás redistribuir trabajo.') return this.localization.traducir('private.workload.reviewPriorities');
    if (texto === 'Tu carga se actualiza al cambiar tus tareas, disponibilidad o ausencias.') return this.localization.traducir('private.workload.updates');
    return texto;
  }
  private readonly analiticaService = inject(AnaliticaOperativaService);

  readonly carga = signal<OcupacionEmpleado | null>(null);
  readonly cargando = signal(true);
  readonly error = signal<string | null>(null);
  readonly porcentajeVisual = computed(() => Math.min(100, Math.max(0, this.carga()?.porcentajeOcupacion ?? 0)));
  readonly anguloOcupacion = computed(() => `${this.porcentajeVisual() * 3.6}deg`);
  readonly colorOcupacion = computed(() => {
    const porcentaje = this.porcentajeVisual();
    const matiz = Math.max(0, 145 - porcentaje * 1.45);
    return `hsl(${matiz} 82% 55%)`;
  });
  readonly claseEstado = computed(() => {
    switch (this.carga()?.estadoCarga) {
      case 'Crítico': return 'tb-status--danger';
      case 'Sobrecargado': return 'tb-status--warning';
      case 'Balanceado': return 'tb-status--success';
      default: return 'tb-status--neutral';
    }
  });

  constructor() {
    this.consultarCarga();
  }

  consultarCarga(): void {
    this.cargando.set(true);
    this.error.set(null);
    this.analiticaService.consultarMiCargaOperativa().subscribe({
      next: carga => {
        this.carga.set(carga);
        this.cargando.set(false);
      },
      error: error => {
        this.error.set(error?.error || 'No pudimos cargar tu información operativa. Intentá nuevamente.');
        this.cargando.set(false);
      }
    });
  }
}
