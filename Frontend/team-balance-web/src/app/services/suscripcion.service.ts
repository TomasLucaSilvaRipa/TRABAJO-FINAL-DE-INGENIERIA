import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { PlanComercial } from './plan-comercial.service';

export interface Suscripcion { id: number; idAgencia: number; idPlanComercial: number; referenciaExterna?: string | null; estado: string; fechaAlta: string; fechaVencimiento: string; fechaProximaRenovacion?: string | null; renovacionAutomatica: boolean; importeVigente: number; activo: boolean; nombrePlan?: string | null; periodicidadPlan?: string | null; moneda?: string | null; referenciaRenovacionProveedor?: string | null; estadoRenovacionProveedor?: string | null; fechaSincronizacionRenovacion?: string | null; }
export interface OperacionSuscripcion { id: number; tipoOperacion: string; modalidad?: string | null; importe?: number | null; moneda?: string | null; proveedor?: string | null; referencia?: string | null; estado: string; detalle?: string | null; fecha: string; }
export interface InicioActualizacionSuscripcion { urlPago: string; referenciaOperacion: string; importe: number; moneda: string; fechaAplicacion: string; }
export interface ResultadoGestionSuscripcion { mensaje: string; estado: string; suscripcion?: Suscripcion | null; }
export interface ConfiguracionRenovacionAutomatica { publicKey: string; importePrueba: number; monedaPrueba: string; nombrePlan: string; }

@Injectable({ providedIn: 'root' })
export class SuscripcionService {
  private readonly http = inject(HttpClient);
  private readonly url = '/api/suscripciones';
  consultarActual(): Observable<Suscripcion> { return this.http.get<Suscripcion>(`${this.url}/actual`); }
  consultarPlanes(): Observable<PlanComercial[]> { return this.http.get<PlanComercial[]>(`${this.url}/planes`); }
  consultarHistorial(): Observable<OperacionSuscripcion[]> { return this.http.get<OperacionSuscripcion[]>(`${this.url}/historial`); }
  consultarConfiguracionRenovacion(): Observable<ConfiguracionRenovacionAutomatica> { return this.http.get<ConfiguracionRenovacionAutomatica>(`${this.url}/renovacion-automatica/configuracion`); }
  solicitarActualizacion(idPlanComercial: number): Observable<InicioActualizacionSuscripcion> { return this.http.post<InicioActualizacionSuscripcion>(`${this.url}/actualizar`, { idPlanComercial }); }
  verificarActualizacion(referenciaOperacion: string, paymentId: string): Observable<ResultadoGestionSuscripcion> { return this.http.post<ResultadoGestionSuscripcion>(`${this.url}/operaciones/${encodeURIComponent(referenciaOperacion)}/verificar-pago`, { paymentId }); }
  cancelarRenovacion(motivo: string): Observable<ResultadoGestionSuscripcion> { return this.http.post<ResultadoGestionSuscripcion>(`${this.url}/cancelar-renovacion`, { motivo }); }
  reactivarRenovacion(): Observable<ResultadoGestionSuscripcion> { return this.http.post<ResultadoGestionSuscripcion>(`${this.url}/reactivar-renovacion`, {}); }
  autorizarRenovacion(cardToken: string, payerEmail: string): Observable<ResultadoGestionSuscripcion> { return this.http.post<ResultadoGestionSuscripcion>(`${this.url}/renovacion-automatica/autorizar`, { cardToken, payerEmail }); }
}
