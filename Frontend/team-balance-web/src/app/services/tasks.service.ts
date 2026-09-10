import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { Proyecto, UsuarioOpcion } from './projects.service';
import { Skill } from './resources.service';
export interface EstadoTarea { id: number; nombre: string; orden: number; }
export interface Tarea { id: number; idProyecto: number; idEmpleadoAsignado?: number | null; idSkillRequerido?: number | null; idEstadoTarea: number; titulo: string; descripcion?: string; prioridad?: string; complejidad?: string; fechaInicio?: string; deadline?: string; seniorityRequerido?: string; porcentajeAvance: number; horasEstimadas: number; activo: boolean; }
export interface TareaOpciones { proyectos: Proyecto[]; estados: EstadoTarea[]; empleados: UsuarioOpcion[]; skills: Skill[]; }
@Injectable({ providedIn: 'root' })
export class TasksService { private readonly http = inject(HttpClient); private readonly url='/api/tareas'; consultar(): Observable<Tarea[]> { return this.http.get<Tarea[]>(this.url); } opciones(): Observable<TareaOpciones> { return this.http.get<TareaOpciones>(`${this.url}/opciones`); } guardar(tarea: Partial<Tarea>): Observable<Tarea> { return tarea.id ? this.http.put<Tarea>(`${this.url}/${tarea.id}`, tarea) : this.http.post<Tarea>(this.url, tarea); } cambiarEstado(id: number, activo: boolean): Observable<boolean> { return this.http.patch<boolean>(`${this.url}/${id}/estado?activo=${activo}`, {}); } crearSkill(skill: Partial<Skill>): Observable<Skill> { return this.http.post<Skill>(`${this.url}/skills`, skill); } cambiarEstadoSkill(id: number, activo: boolean): Observable<boolean> { return this.http.patch<boolean>(`${this.url}/skills/${id}/estado?activo=${activo}`, {}); } }
