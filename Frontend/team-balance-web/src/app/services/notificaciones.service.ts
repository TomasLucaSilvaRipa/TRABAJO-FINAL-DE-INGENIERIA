import { inject, Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

export interface Notificacion { id: number; idUsuario: number; titulo: string; mensaje: string; fecha: string; leida: boolean; }

@Injectable({ providedIn: 'root' })
export class NotificacionesService {
  private readonly http = inject(HttpClient);
  consultar(): Observable<Notificacion[]> { return this.http.get<Notificacion[]>('/api/notificaciones'); }
  marcarLeida(id: number): Observable<void> { return this.http.patch<void>(`/api/notificaciones/${id}/leida`, {}); }
  marcarTodasLeidas(): Observable<void> { return this.http.patch<void>('/api/notificaciones/leer-todas', {}); }
}
