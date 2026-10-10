import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';

export interface OpcionEncuesta {
  id: number;
  idPreguntaEncuesta: number;
  textoEs: string;
  textoEn: string;
  orden: number;
}

export interface PreguntaEncuesta {
  id: number;
  idEncuesta: number;
  enunciadoEs: string;
  enunciadoEn: string;
  orden: number;
  opciones: OpcionEncuesta[];
}

export interface Encuesta {
  id: number;
  tituloEs: string;
  tituloEn: string;
  descripcionEs: string;
  descripcionEn: string;
  fechaInicio: string;
  fechaVencimiento: string;
  activo: boolean;
  cantidadRespuestas: number;
  preguntas: PreguntaEncuesta[];
}

export interface RespuestaPreguntaEncuesta {
  idPreguntaEncuesta: number;
  idOpcionEncuesta: number;
}

export interface RespuestaEncuesta {
  idEncuesta: number;
  identificadorParticipante: string;
  respuestas: RespuestaPreguntaEncuesta[];
}

export interface ResultadoOpcionEncuesta {
  idOpcionEncuesta: number;
  textoEs: string;
  textoEn: string;
  orden: number;
  cantidadRespuestas: number;
  porcentaje: number;
}

export interface ResultadoPreguntaEncuesta {
  idPreguntaEncuesta: number;
  enunciadoEs: string;
  enunciadoEn: string;
  orden: number;
  opciones: ResultadoOpcionEncuesta[];
}

export interface ResultadoEncuesta {
  idEncuesta: number;
  tituloEs: string;
  tituloEn: string;
  cantidadRespuestas: number;
  preguntas: ResultadoPreguntaEncuesta[];
}

@Injectable({ providedIn: 'root' })
export class SurveysService {
  private readonly http = inject(HttpClient);
  private readonly base = '/api/encuestas';
  private readonly participanteKey = 'teambalance.encuestas.participante';

  publicas(): Observable<Encuesta[]> { return this.http.get<Encuesta[]>(`${this.base}/publicas`); }
  publica(encuesta: Encuesta): Observable<Encuesta> { return this.http.get<Encuesta>(`${this.base}/publicas/${encuesta.id}`); }
  responder(respuesta: RespuestaEncuesta): Observable<void> { return this.http.post<void>(`${this.base}/publicas/${respuesta.idEncuesta}/respuestas`, respuesta); }
  gestion(): Observable<Encuesta[]> { return this.http.get<Encuesta[]>(`${this.base}/gestion`); }
  detalleGestion(encuesta: Encuesta): Observable<Encuesta> { return this.http.get<Encuesta>(`${this.base}/gestion/${encuesta.id}`); }
  guardar(encuesta: Encuesta): Observable<Encuesta> { return this.http.post<Encuesta>(`${this.base}/gestion`, encuesta); }
  baja(encuesta: Encuesta): Observable<void> { return this.http.post<void>(`${this.base}/gestion/${encuesta.id}/baja`, {}); }
  resultados(encuesta: Encuesta): Observable<ResultadoEncuesta> { return this.http.get<ResultadoEncuesta>(`${this.base}/gestion/${encuesta.id}/resultados`); }

  identificadorParticipante(): string {
    const identificadorExistente = localStorage.getItem(this.participanteKey);
    if (identificadorExistente) { return identificadorExistente; }
    const identificadorNuevo = crypto.randomUUID();
    localStorage.setItem(this.participanteKey, identificadorNuevo);
    return identificadorNuevo;
  }
}
