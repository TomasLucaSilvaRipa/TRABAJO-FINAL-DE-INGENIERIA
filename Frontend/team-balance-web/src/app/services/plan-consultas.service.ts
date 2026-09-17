import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';

export interface ConsultaPlan { id: number; idPlanComercial: number; nombre: string; email: string; consulta: string; fechaAlta: string; activo: boolean; }

@Injectable({ providedIn: 'root' })
export class PlanConsultasService {
  private readonly http = inject(HttpClient);
  private readonly apiUrl = '/api/planes';
  consultar(idPlan: number): Observable<ConsultaPlan[]> { return this.http.get<ConsultaPlan[]>(`${this.apiUrl}/${idPlan}/consultas`); }
  registrar(idPlan: number, consulta: ConsultaPlan): Observable<ConsultaPlan> { return this.http.post<ConsultaPlan>(`${this.apiUrl}/${idPlan}/consultas`, consulta); }
}
