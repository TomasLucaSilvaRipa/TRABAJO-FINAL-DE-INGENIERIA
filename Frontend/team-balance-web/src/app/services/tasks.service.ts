import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { Proyecto, UsuarioOpcion } from './projects.service';
import { Skill } from './resources.service';
export interface EstadoTarea { id: number; nombre: string; orden: number; }
export interface Tarea { id: number; idProyecto: number; idEmpleadoAsignado?: number | null; idSkillRequerido?: number | null; idEstadoTarea: number; idTareaPredecesora?: number | null; titulo: string; descripcion?: string; estado?: string; prioridad?: string; complejidad?: string; fechaInicio?: string; deadline?: string; fechaFinReal?: string | null; seniorityRequerido?: string; checklistJson?: string | null; comentariosJson?: string | null; archivosAdjuntosJson?: string | null; bloqueada?: boolean; motivoBloqueo?: string | null; porcentajeAvance: number; horasEstimadas: number; activo: boolean; }
export interface FiltroTarea { idProyecto?: number | null; estado?: string | null; prioridad?: string | null; idEmpleadoAsignado?: number | null; idSkillRequerido?: number | null; deadlineDesde?: string | null; deadlineHasta?: string | null; }
export interface TareaOpciones { proyectos: Proyecto[]; estados: EstadoTarea[]; empleados: UsuarioOpcion[]; skills: Skill[]; }
@Injectable({ providedIn: 'root' })
export class TasksService {
  private readonly http = inject(HttpClient);
  private readonly url='/api/tareas';

  consultar(): Observable<Tarea[]> { return this.http.get<Tarea[]>(this.url); }
  filtrar(filtro: FiltroTarea): Observable<Tarea[]> { return this.http.post<Tarea[]>(`${this.url}/filtrar`, filtro); }

  consultarMias(): Observable<Tarea[]> {  return this.http.get<Tarea[]>(`${this.url}/mias`); }
  consultarTableroProyectoPropio(tarea: Partial<Tarea>): Observable<Tarea[]> { return this.http.post<Tarea[]>(`${this.url}/mias/tablero-proyecto`, tarea); }
  consultarPorPM(): Observable<Tarea[]> { return this.http.get<Tarea[]>(`${this.url}/proyectos-pm`); }

  guardarComentarios(tarea: Partial<Tarea>): Observable<boolean> {
    return this.http.patch<boolean>(`${this.url}/mias/comentarios`, tarea);
  }

  opciones(): Observable<TareaOpciones> { return this.http.get<TareaOpciones>(`${this.url}/opciones`); }

  guardar(tarea: Partial<Tarea>): Observable<Tarea> {
    return tarea.id ? this.http.put<Tarea>(this.url, tarea) : this.http.post<Tarea>(this.url, tarea);
  }

  cambiarEstado(tarea: Partial<Tarea>): Observable<boolean> {
    return this.http.patch<boolean>(`${this.url}/estado`, tarea);
  }

  cambiarEstadoPropio(tarea: Partial<Tarea>): Observable<boolean> {
    return this.http.patch<boolean>(`${this.url}/mias/estado`, tarea);
  }

  actualizarAvancePropio(tarea: Partial<Tarea>): Observable<Tarea> {
    return this.http.patch<Tarea>(`${this.url}/mias/avance`, tarea);
  }

  crearSkill(skill: Partial<Skill>): Observable<Skill> {
    return this.http.post<Skill>(`${this.url}/skills`, skill);
  }

  cambiarEstadoSkill(skill: Partial<Skill>): Observable<boolean>{
    return this.http.patch<boolean>(`${this.url}/skills/estado`, skill);
  }
}
