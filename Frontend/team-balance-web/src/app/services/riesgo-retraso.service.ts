import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';

export interface FiltroRiesgoRetraso {
  idProyecto?: number | null;
  idCliente?: number | null;
  idPM?: number | null;
  nivelRiesgo?: string | null;
  diasHastaDeadlineMaximo?: number | null;
}

export interface CausaRiesgo {
  idElemento: number;
  tipoElemento: string;
  descripcion: string;
  nivel: string;
}

export interface TareaRiesgoRetraso {
  idTarea: number;
  titulo: string;
  estado: string;
  bloqueada: boolean;
  horasEstimadas: number;
  porcentajeAvance: number;
  horasRestantes: number;
}

export interface NuevoSeguimientoRiesgoRetraso {
  idProyecto: number;
  idTarea?: number | null;
  observacion: string;
  comunicarResponsable: boolean;
}

export interface DetalleRiesgoProyecto {
  idProyecto: number;
  nombreProyecto: string;
  nombreCliente: string;
  nombrePM: string;
  deadline?: string | null;
  fechaEstimadaFinalizacion?: string | null;
  diasPosibleRetraso: number;
  horasRestantes: number;
  disponibilidadEquipo: number;
  ritmoAvance: number;
  nivelRiesgo: string;
  causas: CausaRiesgo[];
  tareasPendientes: TareaRiesgoRetraso[];
}

export interface ResultadoRiesgoRetraso {
  proyectos: DetalleRiesgoProyecto[];
}

@Injectable({ providedIn: 'root' })
export class RiesgoRetrasoService {
  private readonly http = inject(HttpClient);
  private readonly url = '/api/riesgos-retraso';

  consultar(filtro: FiltroRiesgoRetraso): Observable<ResultadoRiesgoRetraso> {
    return this.http.post<ResultadoRiesgoRetraso>(`${this.url}/consultar`, filtro);
  }

  registrarSeguimiento(seguimiento: NuevoSeguimientoRiesgoRetraso): Observable<string> {
    return this.http.post(`${this.url}/seguimientos`, seguimiento, { responseType: 'text' });
  }
}
