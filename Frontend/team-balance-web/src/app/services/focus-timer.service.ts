import { Injectable, signal } from '@angular/core';
import { Tarea } from './tasks.service';

@Injectable({ providedIn: 'root' })
export class FocusTimerService {
  readonly tarea = signal<Tarea | null>(null);
  readonly segundos = signal(0);
  readonly activo = signal(false);
  private intervalo: ReturnType<typeof setInterval> | null = null;

  iniciar(tarea: Tarea): void {
    this.detenerIntervalo();
    this.tarea.set(tarea);
    this.segundos.set(0);
    this.activo.set(true);
    this.intervalo = setInterval(() => this.segundos.update(segundos => segundos + 1), 1000);
  }

  detener(): { tarea: Tarea | null; segundos: number } {
    const resultado = { tarea: this.tarea(), segundos: this.segundos() };
    this.detenerIntervalo();
    this.tarea.set(null);
    this.segundos.set(0);
    this.activo.set(false);
    return resultado;
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
