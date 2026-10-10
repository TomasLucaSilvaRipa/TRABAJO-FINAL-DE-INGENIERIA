import { inject, Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';

export enum ResultadoBitacora {
  Pendiente = 1,
  Exitoso = 2,
  Parcial = 3,
  Denegado = 4,
  Error = 5
}

export enum CriticidadBitacora {
  Informacion = 1,
  Advertencia = 2,
  Critico = 3
}

export enum ModuloBitacora {
  General = 1,
  Contratacion = 2,
  PreguntasFrecuentes = 3,
  HelpDesk = 4,
  Kanban = 5,
  Novedades = 6,
  Planes = 7,
  Planificacion = 8,
  Proyectos = 9,
  Recursos = 10,
  Registro = 11,
  Respaldos = 12,
  Seguridad = 13,
  Suscripcion = 14,
  Usuarios = 15,
  Operadores = 16,
  Encuestas = 17
}

export interface Bitacora {
  id?: number;
  idUsuario?: number | null;
  idAgencia?: number | null;
  entidad?: string | null;
  idEntidad?: number | null;
  accion: string;
  mensaje: string;
  resultado?: ResultadoBitacora | null;
  criticidad?: CriticidadBitacora | null;
  modulo?: ModuloBitacora | null;
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
  resultado?: ResultadoBitacora | null;
  criticidad?: CriticidadBitacora | null;
  modulo?: ModuloBitacora | null;
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
