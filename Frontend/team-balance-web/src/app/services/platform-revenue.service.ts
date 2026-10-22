import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

export interface PlatformRevenueByAgency {
  idAgencia: number;
  agencia: string;
  hoy: number;
  semana: number;
  mes: number;
  anio: number;
  totalHistorico: number;
  cobrosHistoricos: number;
}

export interface PlatformRevenueByCurrency {
  moneda: string;
  hoy: number;
  semana: number;
  mes: number;
  anio: number;
  totalHistorico: number;
  cobrosHistoricos: number;
  agencias: PlatformRevenueByAgency[];
}

export interface PlatformRevenueSummary {
  fechaCorte: string;
  monedas: PlatformRevenueByCurrency[];
}

@Injectable({ providedIn: 'root' })
export class PlatformRevenueService {
  private readonly http = inject(HttpClient);
  private readonly url = '/api/ingresos-plataforma';

  consultarResumen(): Observable<PlatformRevenueSummary> {
    return this.http.get<PlatformRevenueSummary>(`${this.url}/resumen`);
  }
}
