import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';

export interface AreaSkill { id: number; nombre: string; activo: boolean; }
export interface Skill { id: number; nombre: string; categoria?: string; idAreaSkill?: number | null; nombreArea?: string | null; activo: boolean; }
export interface EmpleadoSkill { id?: number; idEmpleado?: number; idSkill: number; nivel?: string; activo?: boolean; }
export interface DisponibilidadBase { id?: number; idEmpleado?: number; horaInicio: string; horaFin: string; horasSemanales: number; observacion?: string; activo?: boolean; }
export interface FichaEmpleado { id?: number; empleadoSkills: EmpleadoSkill[]; disponibilidadBase: DisponibilidadBase; }
export interface EmpleadoDisponibilidad { id?: number; disponibilidadBase: DisponibilidadBase; }
export interface AusenciaEmpleado { id?: number; idEmpleado?: number; nombreEmpleado?: string; tipoPeriodo: string; fechaInicioSolicitada: string; fechaFinSolicitada: string; fechaInicioAprobada?: string; fechaFinAprobada?: string; horasNoDisponiblesSolicitadas?: number | null; horasNoDisponiblesAprobadas?: number | null; motivo?: string; motivoResolucion?: string; comprobanteUrl?: string; estado?: string; fechaSolicitud?: string; activo?: boolean; }
export interface TareaDisponibilidadAfectada { idTarea: number; titulo: string; nombreProyecto: string; deadline?: string; }
export interface ResultadoGestionDisponibilidad { ausencia: AusenciaEmpleado; tareasAfectadas: TareaDisponibilidadAfectada[]; }
export interface ResolucionAusenciaEmpleado { idAusencia?: number; fechaInicioAprobada?: string | null; fechaFinAprobada?: string | null; horasNoDisponiblesAprobadas?: number | null; motivoResolucion?: string; }
export interface EmpleadoDisponibilidadOpcion { idUsuario: number; nombreCompleto: string; }

@Injectable({ providedIn: 'root' })
export class ResourcesService {
  private readonly http = inject(HttpClient); private readonly url = '/api/recursos';
  consultarSkills(): Observable<Skill[]> { return this.http.get<Skill[]>(`${this.url}/skills`); }
  consultarAreasSkills(): Observable<AreaSkill[]> { return this.http.get<AreaSkill[]>(`${this.url}/skills/areas`); }
  registrarSkill(skill: Partial<Skill>): Observable<Skill> { return this.http.post<Skill>(`${this.url}/skills`, skill); }
  cambiarEstadoSkill(skill: Partial<Skill>): Observable<boolean> { return this.http.patch<boolean>(`${this.url}/skills/${skill.id}/estado`, skill); }
  consultarFichaEmpleado(idUsuario: number): Observable<FichaEmpleado> { return this.http.get<FichaEmpleado>(`${this.url}/empleados/${idUsuario}/ficha`); }
  registrarExportacionFichaEmpleado(idUsuario: number): Observable<void> { return this.http.post<void>(`${this.url}/empleados/${idUsuario}/ficha/exportacion`, {}); }
  guardarFichaEmpleado(ficha: FichaEmpleado): Observable<void> { return this.http.put<void>(`${this.url}/empleados/${ficha.id}/ficha`, ficha); }
  consultarMiDisponibilidad(): Observable<EmpleadoDisponibilidad> { return this.http.get<EmpleadoDisponibilidad>(`${this.url}/disponibilidad`); }
  guardarMiDisponibilidad(disponibilidad: DisponibilidadBase): Observable<void> { return this.http.put<void>(`${this.url}/disponibilidad`, disponibilidad); }
  consultarMisAusencias(): Observable<AusenciaEmpleado[]> { return this.http.get<AusenciaEmpleado[]>(`${this.url}/ausencias`); }
  registrarAusencia(ausencia: AusenciaEmpleado): Observable<ResultadoGestionDisponibilidad> { return this.http.post<ResultadoGestionDisponibilidad>(`${this.url}/ausencias`, ausencia); }
  consultarSolicitudesPendientes(): Observable<AusenciaEmpleado[]> { return this.http.get<AusenciaEmpleado[]>(`${this.url}/ausencias/pendientes`); }
  consultarEmpleadosDisponibilidad(): Observable<EmpleadoDisponibilidadOpcion[]> { return this.http.get<EmpleadoDisponibilidadOpcion[]>(`${this.url}/ausencias/empleados`); }
  aprobarAusencia(idAusencia: number, resolucion: ResolucionAusenciaEmpleado): Observable<ResultadoGestionDisponibilidad> { return this.http.post<ResultadoGestionDisponibilidad>(`${this.url}/ausencias/${idAusencia}/aprobar`, resolucion); }
  aprobarParcialmenteAusencia(idAusencia: number, resolucion: ResolucionAusenciaEmpleado): Observable<ResultadoGestionDisponibilidad> { return this.http.post<ResultadoGestionDisponibilidad>(`${this.url}/ausencias/${idAusencia}/aprobar-parcial`, resolucion); }
  rechazarAusencia(idAusencia: number, resolucion: ResolucionAusenciaEmpleado): Observable<AusenciaEmpleado> { return this.http.post<AusenciaEmpleado>(`${this.url}/ausencias/${idAusencia}/rechazar`, resolucion); }
  registrarAusenciaDirecta(ausencia: AusenciaEmpleado): Observable<ResultadoGestionDisponibilidad> { return this.http.post<ResultadoGestionDisponibilidad>(`${this.url}/ausencias/directa`, ausencia); }
}
