import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';

export interface Skill { id: number; nombre: string; categoria?: string; activo: boolean; }
export interface EmpleadoSkill { id?: number; idEmpleado?: number; idSkill: number; nivel?: string; activo?: boolean; }
export interface DisponibilidadBase { id?: number; idEmpleado?: number; horaInicio: string; horaFin: string; horasSemanales: number; observacion?: string; activo?: boolean; }
export interface FichaEmpleado { id?: number; empleadoSkills: EmpleadoSkill[]; disponibilidadBase: DisponibilidadBase; }
export interface EmpleadoDisponibilidad { id?: number; disponibilidadBase: DisponibilidadBase; }

@Injectable({ providedIn: 'root' })
export class ResourcesService {
  private readonly http = inject(HttpClient); private readonly url = '/api/recursos';
  consultarSkills(): Observable<Skill[]> { return this.http.get<Skill[]>(`${this.url}/skills`); }
  registrarSkill(skill: Partial<Skill>): Observable<Skill> { return this.http.post<Skill>(`${this.url}/skills`, skill); }
  cambiarEstadoSkill(skill: Partial<Skill>): Observable<boolean> { return this.http.patch<boolean>(`${this.url}/skills/${skill.id}/estado`, skill); }
  consultarFichaEmpleado(idUsuario: number): Observable<FichaEmpleado> { return this.http.get<FichaEmpleado>(`${this.url}/empleados/${idUsuario}/ficha`); }
  guardarFichaEmpleado(ficha: FichaEmpleado): Observable<void> { return this.http.put<void>(`${this.url}/empleados/${ficha.id}/ficha`, ficha); }
  consultarMiDisponibilidad(): Observable<EmpleadoDisponibilidad> { return this.http.get<EmpleadoDisponibilidad>(`${this.url}/disponibilidad`); }
  guardarMiDisponibilidad(empleado: EmpleadoDisponibilidad): Observable<void> { return this.http.put<void>(`${this.url}/disponibilidad`, empleado); }
}
