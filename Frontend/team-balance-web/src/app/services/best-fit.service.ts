import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';

export interface RecomendacionBestFit { id: number; idTarea: number; idEmpleadoSugerido: number; idUsuarioSolicitante: number; puntajeCompatibilidad: number; fechaGeneracion: string; seleccionadaPorPM: boolean; estado: string; activo: boolean; }

@Injectable({ providedIn: 'root' })
export class BestFitService {
  private readonly http = inject(HttpClient); private readonly url = '/api/best-fit';
  sugerir(recomendacion: Partial<RecomendacionBestFit>): Observable<RecomendacionBestFit[]> { return this.http.post<RecomendacionBestFit[]>(`${this.url}/sugerencias`, recomendacion); }
}
