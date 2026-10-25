import { inject, Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

export interface OperationalEmailPreferences {
  idUsuario: number;
  resumenDiarioActivo: boolean;
  horaResumenDiario: string;
  alertasDeadlineActivo: boolean;
  resumenSemanalActivo: boolean;
}

export interface TaskEmailAction {
  idUsuario: number;
  idTarea: number;
  accion: string;
  tituloTarea: string;
  proyecto: string;
  fechaExpiracion: string;
  disponible: boolean;
}

@Injectable({ providedIn: 'root' })
export class OperationalEmailService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = '/api/correos-operativos';
  getPreferences(): Observable<OperationalEmailPreferences> { return this.http.get<OperationalEmailPreferences>(`${this.baseUrl}/preferencias`); }
  savePreferences(preferences: OperationalEmailPreferences): Observable<OperationalEmailPreferences> { return this.http.put<OperationalEmailPreferences>(`${this.baseUrl}/preferencias`, preferences); }
  getAction(token: string): Observable<TaskEmailAction> { return this.http.get<TaskEmailAction>(`${this.baseUrl}/acciones/${encodeURIComponent(token)}`); }
  executeAction(token: string): Observable<TaskEmailAction> { return this.http.post<TaskEmailAction>(`${this.baseUrl}/acciones/${encodeURIComponent(token)}`, {}); }
}
