import { inject, Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';

export interface Bitacora {
  id?: number;
  idUsuario?: number | null;
  idAgencia?: number | null;
  entidad?: string | null;
  idEntidad?: number | null;
  accion: string;
  mensaje: string;
  resultado?: string | null;
  criticidad?: string | null;
  modulo?: string | null;
  direccionIP?: string | null;
  fechaHora: string;
}

export interface FiltroBitacora {
  idAgencia?: number | null;
  desde?: string | null;
  hasta?: string | null;
  idUsuario?: number | null;
  entidad?: string | null;
  accion?: string | null;
  resultado?: string | null;
  criticidad?: string | null;
  modulo?: string | null;
}

@Injectable({
  providedIn: 'root',
})
export class LogService {

  private readonly http = inject(HttpClient);

  private readonly logApiUrl = '/api/bitacora';

  logs(filtros: FiltroBitacora = {}): Observable<Bitacora[]> {

    let params = new HttpParams();

    Object.entries(filtros).forEach(([clave, valor]) => {

      if (
        valor !== null &&
        valor !== undefined &&
        valor !== ''
      ) {
        params = params.set(clave, String(valor));
      }

    });

    return this.http.get<Bitacora[]>(
      this.logApiUrl,
      { params }
    );
  }
}
