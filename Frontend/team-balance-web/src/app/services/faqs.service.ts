import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';

export enum CategoriaPreguntaFrecuente {
  PrimerosPasos = 1,
  ProyectosYTareas = 2,
  EquipoYDisponibilidad = 3,
  CuentaYSuscripcion = 4,
  General = 5
}

export interface PreguntaFrecuente {
  id: number;
  categoria: CategoriaPreguntaFrecuente;
  preguntaEs: string;
  respuestaEs: string;
  preguntaEn: string;
  respuestaEn: string;
  orden: number;
  activo: boolean;
}

@Injectable({ providedIn: 'root' })
export class FaqsService {
  private readonly http = inject(HttpClient);
  private readonly base = '/api/faqs';

  publicas(): Observable<PreguntaFrecuente[]> { return this.http.get<PreguntaFrecuente[]>(`${this.base}/publicas`); }
  gestion(): Observable<PreguntaFrecuente[]> { return this.http.get<PreguntaFrecuente[]>(`${this.base}/gestion`); }
  guardar(pregunta: PreguntaFrecuente): Observable<PreguntaFrecuente> { return this.http.post<PreguntaFrecuente>(this.base, pregunta); }
  baja(pregunta: PreguntaFrecuente): Observable<void> { return this.http.post<void>(`${this.base}/${pregunta.id}/baja`, {}); }
}
