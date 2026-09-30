import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';

export interface RegistroHora { id?: number; idTarea: number; idEmpleado?: number; fecha: string; cantidadHoras: number; descripcion?: string; activo?: boolean; }

@Injectable({ providedIn: 'root' })
export class RegistroHorasService {
  private readonly http = inject(HttpClient);
  private readonly url = '/api/registro-horas';
  consultarPropios(): Observable<RegistroHora[]> { return this.http.get<RegistroHora[]>(this.url); }
  registrar(registro: RegistroHora): Observable<RegistroHora> { return this.http.post<RegistroHora>(this.url, registro); }
}
