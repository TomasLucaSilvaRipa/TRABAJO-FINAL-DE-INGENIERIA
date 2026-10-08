import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';

export type EstadoTicket = 'Pendiente' | 'En revisión' | 'Respondida' | 'Resuelta';
export interface TicketSoporte { id: number; idUsuario: number; idAgencia?: number | null; idSuscripcion?: number | null; categoria: string; asunto: string; descripcion: string; estado: EstadoTicket; fechaCreacion: string; fechaActualizacion?: string | null; nombreSolicitante: string; emailSolicitante?: string | null; nombreAgencia?: string | null; nombrePlan?: string | null; cantidadMensajes: number; }
export interface MensajeSoporte { id: number; idUsuario: number; nombreUsuario: string; esSoporte: boolean; mensaje: string; fecha: string; }
export interface DetalleTicket { consulta: TicketSoporte; mensajes: MensajeSoporte[]; }

@Injectable({ providedIn: 'root' })
export class HelpdeskService {
  private readonly http = inject(HttpClient); private readonly base = '/api/helpdesk';
  misConsultas(): Observable<TicketSoporte[]> { return this.http.get<TicketSoporte[]>(`${this.base}/mis-consultas`); }
  crear(categoria: string, asunto: string, descripcion: string): Observable<TicketSoporte> { return this.http.post<TicketSoporte>(`${this.base}/consultas`, { categoria, asunto, descripcion }); }
  detalle(id: number): Observable<DetalleTicket> { return this.http.get<DetalleTicket>(`${this.base}/consultas/${id}`); }
  enviarMensaje(id: number, mensaje: string, estado?: EstadoTicket): Observable<void> { return this.http.post<void>(`${this.base}/consultas/${id}/mensajes`, { mensaje, estado }); }
  resolver(id: number): Observable<void> { return this.http.post<void>(`${this.base}/consultas/${id}/cerrar`, {}); }
  cambiarEstado(id: number, estado: EstadoTicket): Observable<void> { return this.http.post<void>(`${this.base}/consultas/${id}/estado`, { estado }); }
  bandeja(estado?: string): Observable<TicketSoporte[]> { return this.http.get<TicketSoporte[]>(`${this.base}/bandeja`, { params: estado ? { estado } : {} }); }
}
