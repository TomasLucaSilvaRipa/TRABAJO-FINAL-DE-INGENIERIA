import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';

export interface PlanComercial {
  id: number;
  nombre: string;
  descripcion: string | null;
  periodicidad: string;
  precioVigente: number;
  moneda: string;
  duracionMeses: number;
  alcanceFuncional: string | null;
  condicionesRenovacion: string | null;
  activo: boolean;
  fechaVigenciaDesde: string;
  fechaVigenciaHasta: string | null;
}

@Injectable({ providedIn: 'root' })
export class PlanComercialService {
  private readonly http = inject(HttpClient);
  private readonly apiUrl = '/api/planes';

  consultarPlanesActivos(): Observable<PlanComercial[]> {
    return this.http.get<PlanComercial[]>(this.apiUrl);
  }

  consultarPlanes(): Observable<PlanComercial[]> {
    return this.http.get<PlanComercial[]>(`${this.apiUrl}/gestion`);
  }

  consultarPlan(id: number): Observable<PlanComercial> {
    return this.http.get<PlanComercial>(`${this.apiUrl}/${id}`);
  }

  registrarPlan(plan: PlanComercial): Observable<PlanComercial> {
    return this.http.post<PlanComercial>(this.apiUrl, plan);
  }

  modificarPlan(plan: PlanComercial): Observable<PlanComercial> {
    return this.http.put<PlanComercial>(`${this.apiUrl}/${plan.id}`, plan);
  }

  cambiarEstado(plan: PlanComercial, activo: boolean): Observable<PlanComercial> {
    return this.http.patch<PlanComercial>(`${this.apiUrl}/${plan.id}/estado`, { ...plan, activo });
  }

  obtenerFuncionalidades(plan: PlanComercial): string[] {
    return (plan.alcanceFuncional ?? '').split(';').map((funcionalidad) => funcionalidad.trim()).filter(Boolean);
  }
}
