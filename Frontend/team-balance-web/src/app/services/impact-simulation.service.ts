import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';

export interface SimulacionImpacto { id: number; idTarea: number; idEmpleadoCandidato: number; cargaActual?: number; cargaProyectada?: number; disponibilidadRestante?: number; porcentajeOcupacionActual?: number; porcentajeOcupacionProyectado?: number; generaSobrecarga: boolean; impactoOperativo?: string; }
@Injectable({ providedIn: 'root' }) export class ImpactSimulationService { private readonly http = inject(HttpClient); simular(simulacion: Partial<SimulacionImpacto>): Observable<SimulacionImpacto> { return this.http.post<SimulacionImpacto>('/api/simulaciones-impacto', simulacion); } }
