import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';

export interface FiltroReporteEjecutivo {
  fechaDesde?: string | null;
  fechaHasta?: string | null;
  idCliente?: number | null;
  idProyecto?: number | null;
  idResponsable?: number | null;
  estadoProyecto?: string | null;
  alcanceGeneral: boolean;
}

export interface ConfiguracionReporteEjecutivo {
  titulo: string;
  filtro: FiltroReporteEjecutivo;
  incluirOcupacionOperativa: boolean;
  incluirRiesgoRetraso: boolean;
  incluirDesvioHoras: boolean;
  incluirEficienciaPerfiles: boolean;
  incluirConvenienciaOperativa: boolean;
  incluirTodasLasSecciones: boolean;
}

export interface IndicadorReporteEjecutivo {
  nombre: string;
  valor: number;
  unidad: string;
  estadoVisual: string;
  descripcion: string;
}

export interface SeccionReporteEjecutivo {
  titulo: string;
  descripcion: string;
  orden: number;
  indicadores: IndicadorReporteEjecutivo[];
}

export interface ReporteEjecutivo {
  idReporte: number;
  titulo: string;
  fechaDesde?: string | null;
  fechaHasta?: string | null;
  fechaGeneracion: string;
  secciones: SeccionReporteEjecutivo[];
}

@Injectable({ providedIn: 'root' })
export class ReporteEjecutivoService {
  private readonly http = inject(HttpClient);
  private readonly url = '/api/reportes-ejecutivos';

  previsualizar(configuracion: ConfiguracionReporteEjecutivo): Observable<ReporteEjecutivo> {
    return this.http.post<ReporteEjecutivo>(`${this.url}/previsualizar`, configuracion);
  }
}
