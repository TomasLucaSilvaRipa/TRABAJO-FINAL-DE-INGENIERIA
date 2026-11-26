import { computed, Injectable, signal } from '@angular/core';
import { Tarea } from './tasks.service';
import { HORAS_MAXIMAS_JORNADA } from './registro-horas.service';

export const SEGUNDOS_MAXIMOS_FOCO = HORAS_MAXIMAS_JORNADA * 3600;
const SEGUNDOS_PARA_PAUSA = 3600;

@Injectable({ providedIn: 'root' })
export class FocusTimerService {
  readonly tarea = signal<Tarea | null>(null);
  readonly segundos = signal(0);
  readonly activo = signal(false);
  readonly avisoPausaDescartado = signal(false);
  readonly mostrarAvisoPausa = computed(() =>
    this.activo() && this.segundos() >= SEGUNDOS_PARA_PAUSA && !this.avisoPausaDescartado()
  );
  private intervalo: ReturnType<typeof setInterval> | null = null;
  private inicio = 0;

  iniciar(tarea: Tarea): void {
    this.detenerIntervalo();
    this.tarea.set(tarea);
    this.segundos.set(0);
    this.avisoPausaDescartado.set(false);
    this.inicio = Date.now();
    this.activo.set(true);
    this.intervalo = setInterval(() => {
      const segundos = this.duracionActual();
      this.segundos.set(segundos);
      if (segundos >= SEGUNDOS_MAXIMOS_FOCO) { this.detenerIntervalo(); }
    }, 1000);
  }

  detener(): { tarea: Tarea | null; segundos: number } {
    const resultado = { tarea: this.tarea(), segundos: this.activo() ? this.duracionActual() : 0 };
    this.detenerIntervalo();
    this.tarea.set(null);
    this.segundos.set(0);
    this.activo.set(false);
    return resultado;
  }

  private duracionActual(): number {
    return Math.min(SEGUNDOS_MAXIMOS_FOCO, Math.max(this.segundos(), Math.floor((Date.now() - this.inicio) / 1000)));
  }

  formatoDuracion(): string {
    const total = this.segundos();
    const horas = Math.floor(total / 3600).toString().padStart(2, '0');
    const minutos = Math.floor((total % 3600) / 60).toString().padStart(2, '0');
    const segundos = (total % 60).toString().padStart(2, '0');
    return `${horas}:${minutos}:${segundos}`;
  }

  private detenerIntervalo(): void {
    if (this.intervalo) {
      clearInterval(this.intervalo);
      this.intervalo = null;
    }
  }
}
