import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { PlanComercial } from './plan-comercial.service';

export interface Suscripcion { id: number; idAgencia: number; idPlanComercial: number; referenciaExterna?: string | null; estado: string; fechaAlta: string; fechaVencimiento: string; fechaProximaRenovacion?: string | null; renovacionAutomatica: boolean; importeVigente: number; activo: boolean; fechaBaja?: string | null; nombrePlan?: string | null; periodicidadPlan?: string | null; moneda?: string | null; referenciaRenovacionProveedor?: string | null; estadoRenovacionProveedor?: string | null; proveedorRenovacion?: string | null; fechaSincronizacionRenovacion?: string | null; }
export interface OperacionSuscripcion { id: number; tipoOperacion: string; modalidad?: string | null; importe?: number | null; moneda?: string | null; proveedor?: string | null; referencia?: string | null; estado: string; detalle?: string | null; fecha: string; }
export interface DocumentoComercial { id: number; idAgencia: number; idSuscripcion?: number | null; idDocumentoOrigen?: number | null; idPlanComercial?: number | null; tipo: string; numero: string; concepto: string; importe: number; saldoPendiente: number; moneda: string; estado: string; fechaEmision: string; fechaVencimiento?: string | null; motivo?: string | null; }
export interface InicioActualizacionSuscripcion { urlPago: string; referenciaOperacion: string; importe: number; moneda: string; fechaAplicacion: string; preapprovalId?: string | null; }
export interface ResultadoGestionSuscripcion { mensaje: string; estado: string; suscripcion?: Suscripcion | null; urlPagoSiguiente?: string | null; referenciaOperacion?: string | null; pagoConfirmado?: boolean; pagosCompletados?: number; pagosTotales?: number; }
export interface DistribucionPagoSuscripcion { proveedor: string; medioPago: string; importe: number; }
export interface CotizacionPagoSuscripcion { importePlan: number; monedaPlan: string; importeMercadoPago: number; monedaMercadoPago: string; }
export interface ConfiguracionRenovacionAutomatica { publicKey: string; importePrueba: number; monedaPrueba: string; nombrePlan: string; fechaPrimerCobro: string; }
export interface ReactivacionConCreditoRequest { idPlanComercial: number; proveedor: 'MercadoPago' | 'Stripe'; }

@Injectable({ providedIn: 'root' })
export class SuscripcionService {
  private readonly http = inject(HttpClient);
  private readonly url = '/api/suscripciones';
  consultarActual(): Observable<Suscripcion> { return this.http.get<Suscripcion>(`${this.url}/actual`); }
  consultarPlanes(): Observable<PlanComercial[]> { return this.http.get<PlanComercial[]>(`${this.url}/planes`); }
  consultarHistorial(): Observable<OperacionSuscripcion[]> { return this.http.get<OperacionSuscripcion[]>(`${this.url}/historial`); }
  consultarCuentaCorriente(): Observable<DocumentoComercial[]> { return this.http.get<DocumentoComercial[]>(`${this.url}/cuenta-corriente`); }
  consultarConfiguracionRenovacion(): Observable<ConfiguracionRenovacionAutomatica> { return this.http.get<ConfiguracionRenovacionAutomatica>(`${this.url}/renovacion-automatica/configuracion`); }
  solicitarActualizacion(idPlanComercial: number, proveedor: string, usarSaldoAFavor = false, pagos?: DistribucionPagoSuscripcion[], importeCredito?: number): Observable<InicioActualizacionSuscripcion> { return this.http.post<InicioActualizacionSuscripcion>(`${this.url}/actualizar`, { idPlanComercial, proveedor, usarSaldoAFavor, pagos, importeCredito }); }
  consultarCotizacionPago(idPlan: number): Observable<CotizacionPagoSuscripcion> { return this.http.get<CotizacionPagoSuscripcion>(`${this.url}/planes/${idPlan}/cotizacion-pago`); }
  continuarActualizacion(referencia: string): Observable<ResultadoGestionSuscripcion> { return this.http.post<ResultadoGestionSuscripcion>(`${this.url}/operaciones/${encodeURIComponent(referencia)}/continuar`, {}); }
  verificarActualizacion(referenciaOperacion: string, paymentId: string): Observable<ResultadoGestionSuscripcion> { return this.http.post<ResultadoGestionSuscripcion>(`${this.url}/operaciones/${encodeURIComponent(referenciaOperacion)}/verificar-pago`, { paymentId }); }
  cancelarRenovacion(motivo: string): Observable<ResultadoGestionSuscripcion> { return this.http.post<ResultadoGestionSuscripcion>(`${this.url}/cancelar-renovacion`, { motivo }); }
  cancelarServicio(documento: DocumentoComercial): Observable<DocumentoComercial> { return this.http.post<DocumentoComercial>(`${this.url}/cancelar-servicio`, documento); }
  reactivarConNotaCredito(documento: DocumentoComercial): Observable<DocumentoComercial> { return this.http.post<DocumentoComercial>(`${this.url}/reactivar-con-nota-credito`, documento); }
  solicitarReactivacionConCreditoParcial(solicitud: ReactivacionConCreditoRequest): Observable<InicioActualizacionSuscripcion> { return this.http.post<InicioActualizacionSuscripcion>(`${this.url}/reactivar-con-nota-credito/diferencia`, solicitud); }
  reactivarRenovacion(): Observable<ResultadoGestionSuscripcion> { return this.http.post<ResultadoGestionSuscripcion>(`${this.url}/reactivar-renovacion`, {}); }
  autorizarRenovacion(payerEmail: string): Observable<InicioActualizacionSuscripcion> { return this.http.post<InicioActualizacionSuscripcion>(`${this.url}/renovacion-automatica/autorizar`, { payerEmail }); }
  verificarRenovacionMercadoPago(referenciaOperacion: string, preapprovalId = ''): Observable<ResultadoGestionSuscripcion> { return this.http.post<ResultadoGestionSuscripcion>(`${this.url}/renovacion-automatica/verificar-mercado-pago`, { referenciaOperacion, paymentId: preapprovalId }); }
  solicitarRenovacionStripe(): Observable<InicioActualizacionSuscripcion> { return this.http.post<InicioActualizacionSuscripcion>(`${this.url}/renovacion-automatica/stripe`, {}); }
  verificarRenovacionStripe(sessionId: string): Observable<ResultadoGestionSuscripcion> { return this.http.post<ResultadoGestionSuscripcion>(`${this.url}/renovacion-automatica/verificar-stripe`, { paymentId: sessionId }); }
}
