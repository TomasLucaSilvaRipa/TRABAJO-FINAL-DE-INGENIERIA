import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { Tarea } from './tasks.service';

export interface SimulacionImpacto {
  id: number;
  idTarea: number;
  idEmpleadoCandidato: number;
  cargaActual?: number;
  cargaProyectada?: number;
  disponibilidadRestante?: number;
  porcentajeOcupacionActual?: number;
  porcentajeOcupacionProyectado?: number;
  generaSobrecarga: boolean;
  advertenciasJson?: string;
  impactoOperativo?: string;
  horasTarea?: number;
  capacidadSemanal?: number;
  diasAusenciaProximaSemana?: number;
  deadlineTarea?: string;
  prioridadTarea?: string;
  nombreTarea?: string;
  nombreProyecto?: string;
}

@Injectable({ providedIn: 'root' })
export class ImpactSimulationService {
  private readonly http = inject(HttpClient);
  private readonly url = '/api/simulaciones-impacto';

  consultarTareasDisponibles(): Observable<Tarea[]> {
    return this.http.get<Tarea[]>(`${this.url}/tareas`);
  }

  calcular(simulacion: Partial<SimulacionImpacto>): Observable<SimulacionImpacto> {
    return this.http.post<SimulacionImpacto>(`${this.url}/calcular`, simulacion);
  }

  conservar(simulacion: Partial<SimulacionImpacto>): Observable<SimulacionImpacto> {
    return this.http.post<SimulacionImpacto>(this.url, simulacion);
  }

  descartar(simulacion: Partial<SimulacionImpacto>): Observable<boolean> {
    return this.http.patch<boolean>(`${this.url}/descartar`, simulacion);
  }
}
