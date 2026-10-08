import { inject, Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

export interface RespaldoBaseDatos {
  id: number; nombreArchivo: string; rutaArchivo: string; fechaInicio: string; fechaFin: string | null; estado: string;
  verificado: boolean; tamanoBytes: number | null; mensaje: string | null; fechaExpiracion: string;
}

export interface PruebaRestauracion { id: number; idRespaldo: number; baseDatosDestino: string; fechaInicio: string; fechaFin: string | null; estado: string; mensaje: string | null; }

export interface EstadoRespaldos { respaldos: RespaldoBaseDatos[]; pruebasRestauracion: PruebaRestauracion[]; rpoHoras: number; rtoHoras: number; retencionDias: number; }

@Injectable({ providedIn: 'root' })
export class RespaldosService {
  private readonly http = inject(HttpClient);
  consultar(): Observable<EstadoRespaldos> { return this.http.get<EstadoRespaldos>('/api/respaldos'); }
  crear(): Observable<RespaldoBaseDatos> { return this.http.post<RespaldoBaseDatos>('/api/respaldos', {}); }
  restaurarEnAislado(idRespaldo: number, confirmacion: string): Observable<PruebaRestauracion> { return this.http.post<PruebaRestauracion>(`/api/respaldos/${idRespaldo}/restauracion-aislada`, { confirmacion }); }
  restaurarProduccion(idRespaldo: number, confirmacion: string): Observable<{ mensaje: string }> { return this.http.post<{ mensaje: string }>(`/api/respaldos/${idRespaldo}/restauracion-produccion`, { confirmacion }); }
}
