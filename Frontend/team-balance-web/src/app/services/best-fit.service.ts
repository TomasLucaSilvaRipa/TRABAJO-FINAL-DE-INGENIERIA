import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';

export interface RecomendacionBestFit { id: number; idTarea: number; idEmpleadoSugerido: number; idUsuarioSolicitante: number; puntajeCompatibilidad: number; fechaGeneracion: string; seleccionadaPorPM: boolean; estado?: string; activo: boolean; nombreEmpleado: string; seniorityEmpleado: string; horasDisponibles: number; horasComprometidasSemanales: number; cargaActualPorcentaje: number; margenHorasSemanales: number; puntajeSkill: number; puntajeSeniority: number; puntajeCapacidad: number; puntajeCargaActual: number; puntajeHistorial: number; cantidadTareasHistoricasComparables: number; detalleCompatibilidad: string; fortalezas: string; advertencias: string; }

@Injectable({ providedIn: 'root' })
export class BestFitService {
  private readonly http = inject(HttpClient); private readonly url = '/api/best-fit';
  sugerir(recomendacion: Partial<RecomendacionBestFit>): Observable<RecomendacionBestFit[]> { return this.http.post<RecomendacionBestFit[]>(`${this.url}/sugerencias`, recomendacion); }
  guardarRecomendacion(recomendacion: RecomendacionBestFit): Observable<boolean> { return this.http.patch<boolean>(`${this.url}/seleccion`, recomendacion); }
  consultarRecomendaciones(idTarea: number): Observable<RecomendacionBestFit[]> { return this.http.get<RecomendacionBestFit[]>(`${this.url}/tareas/${idTarea}/recomendaciones`); }
}
