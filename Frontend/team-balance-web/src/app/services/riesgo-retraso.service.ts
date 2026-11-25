import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import type { LocalizationService } from './localization.service';

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

export function traducirCausaRiesgo(descripcion: string, localization: LocalizationService): string {
  const causasFijas: Readonly<Record<string, string>> = {
    'El proyecto no tiene una fecha límite definida.': 'noDeadline',
    'No se registró disponibilidad para el equipo asignado.': 'noAvailability',
    'El proyecto no presenta señales críticas con la información disponible.': 'noCriticalSignals'
  };
  const traducir = (clave: string) => localization.traducir(`reports.riskCause.${clave}`);
  const clave = causasFijas[descripcion];
  if (clave) return traducir(clave);

  const bloqueadas = /^Hay (\d+) tarea\(s\) bloqueada\(s\) que requieren seguimiento\.$/.exec(descripcion);
  if (bloqueadas) {
    const cantidad = Number(bloqueadas[1]);
    return traducir(cantidad === 1 ? 'blockedOne' : 'blockedOther').replace('{count}', localization.formatearNumero(cantidad));
  }

  const sinAvance = /^No hay avance registrado en los últimos (\d+) días; la fecha se proyecta con la capacidad disponible\.$/.exec(descripcion);
  if (sinAvance) return traducir('noProgress').replace('{days}', localization.formatearNumero(Number(sinAvance[1])));

  const proyeccion = /^Con el ritmo y la capacidad actuales, la finalización se proyecta para el (\d{2})\/(\d{2})\/(\d{4}), (\d+) día\(s\) después del deadline\.$/.exec(descripcion);
  if (proyeccion) {
    const [, dia, mes, anio, demora] = proyeccion;
    const fecha = new Date(Number(anio), Number(mes) - 1, Number(dia));
    const dias = Number(demora);
    return traducir(dias === 1 ? 'delayOne' : 'delayOther')
      .replace('{date}', localization.formatearFecha(fecha))
      .replace('{days}', localization.formatearNumero(dias));
  }
  return descripcion;
}

export interface TareaRiesgoRetraso {
  idTarea: number;
  titulo: string;
  estado: string;
  bloqueada: boolean;
  fechaInicio?: string | null;
  deadline?: string | null;
  horasEstimadas: number;
  porcentajeAvance: number;
  horasRestantes: number;
}

export interface HitoTareaProyecto {
  idTarea: number;
  titulo: string;
  fechaInicio?: string | null;
  deadline?: string | null;
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
  fechaInicio?: string | null;
  deadline?: string | null;
  fechaEstimadaFinalizacion?: string | null;
  diasPosibleRetraso: number;
  horasRestantes: number;
  disponibilidadEquipo: number;
  ritmoAvance: number;
  nivelRiesgo: string;
  causas: CausaRiesgo[];
  tareasPendientes: TareaRiesgoRetraso[];
  hitosTareas: HitoTareaProyecto[];
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
