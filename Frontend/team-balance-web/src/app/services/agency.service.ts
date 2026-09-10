import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';

export interface Agencia { id: number; nombreComercial: string; razonSocial?: string | null; cuit: string; condicionFiscal?: string | null; emailContacto: string; telefonoContacto?: string | null; fechaAlta: string; estado: string; activo: boolean; }
export interface Suscripcion { id: number; idAgencia: number; idPlanComercial: number; referenciaExterna?: string | null; estado: string; fechaAlta: string; fechaVencimiento: string; fechaProximaRenovacion?: string | null; renovacionAutomatica: boolean; importeVigente: number; activo: boolean; nombrePlan?: string | null; periodicidadPlan?: string | null; moneda?: string | null; }

@Injectable({ providedIn: 'root' })
export class AgencyService {
  private readonly http = inject(HttpClient);
  private readonly url = '/api/agencias';
  consultarActual(): Observable<Agencia> { return this.http.get<Agencia>(`${this.url}/actual`); }
  modificarActual(agencia: Partial<Agencia>): Observable<Agencia> { return this.http.put<Agencia>(`${this.url}/actual`, agencia); }
  consultarSuscripcion(): Observable<Suscripcion | null> { return this.http.get<Suscripcion | null>(`${this.url}/suscripcion`); }
}
