import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';

export interface ConsultaContacto { nombre: string; organizacion?: string; email: string; mensaje: string; }
export interface ConsultaContactoRespuesta { mensaje: string; }

@Injectable({ providedIn: 'root' })
export class ContactService {
  private readonly http = inject(HttpClient);
  enviar(consulta: ConsultaContacto): Observable<ConsultaContactoRespuesta> { return this.http.post<ConsultaContactoRespuesta>('/api/contacto', consulta); }
}
