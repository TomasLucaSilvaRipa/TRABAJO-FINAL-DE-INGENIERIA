import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';

export interface ConsultaPlan { id: number; idPlanComercial: number; nombre: string; email: string; consulta: string; fechaAlta: string; activo: boolean; estado?: string; respuesta?: string | null; fechaRespuesta?: string | null; idUsuarioSoporte?: number | null; nombrePlan?: string | null; nombreRespondedor?: string | null; }

@Injectable({ providedIn: 'root' })
export class PlanConsultasService {
  private readonly http = inject(HttpClient);
  private readonly apiUrl = '/api/planes';
  consultar(idPlan: number): Observable<ConsultaPlan[]> {
    return this.http.get<ConsultaPlan[]>(`${this.apiUrl}/${idPlan}/consultas`);
  }
  registrar(consulta: ConsultaPlan): Observable<ConsultaPlan> {
    return this.http.post<ConsultaPlan>(`${this.apiUrl}/consultas`, consulta);
  }
}
