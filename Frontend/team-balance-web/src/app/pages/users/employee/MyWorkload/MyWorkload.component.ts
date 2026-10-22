import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { DecimalPipe } from '@angular/common';
import { AnaliticaOperativaService, OcupacionEmpleado } from '../../../../services/analitica-operativa.service';

@Component({
  selector: 'app-my-workload',
  imports: [DecimalPipe],
  templateUrl: './MyWorkload.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class MyWorkload {
  private readonly analiticaService = inject(AnaliticaOperativaService);

  readonly carga = signal<OcupacionEmpleado | null>(null);
  readonly cargando = signal(true);
  readonly error = signal<string | null>(null);
  readonly porcentajeVisual = computed(() => Math.min(100, Math.max(0, this.carga()?.porcentajeOcupacion ?? 0)));
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
