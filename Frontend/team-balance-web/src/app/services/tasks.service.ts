import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { Proyecto, UsuarioOpcion } from './projects.service';
import { AreaSkill, Skill } from './resources.service';
export interface EstadoTarea { id: number; idAgencia?: number; nombre: string; orden: number; esBase: boolean; esFinal: boolean; activo: boolean; }
export interface ArchivoAdjuntoTarea { id: number; idTarea: number; nombreOriginal: string; tipoContenido: string; tamanoBytes: number; fechaAlta: string; esImagen: boolean; }
export interface ConfiguracionAlertaAgencia { idAgencia: number; umbralConsumoHoras: number; notificacionInternaActiva: boolean; emailActivo: boolean; slackActivo: boolean; }
export interface AsignacionTarea { id?: number; idTarea?: number; idEmpleado: number; idUsuarioAsignador?: number; fechaDesde?: string; fechaHasta?: string | null; motivo?: string | null; horasAsignadas: number; activa?: boolean; }
export interface Tarea { id: number; idProyecto: number; idEmpleadoAsignado?: number | null; idSkillRequerido?: number | null; idEstadoTarea: number; idTareaPredecesora?: number | null; titulo: string; descripcion?: string; estado?: string; prioridad?: string; complejidad?: string; fechaInicio?: string; deadline?: string; fechaFinReal?: string | null; seniorityRequerido?: string; checklistJson?: string | null; comentariosJson?: string | null; archivosAdjuntosJson?: string | null; bloqueada?: boolean; motivoBloqueo?: string | null; porcentajeAvance: number; horasEstimadas: number; activo: boolean; asignaciones?: AsignacionTarea[]; }
export interface FiltroTarea { idProyecto?: number | null; estado?: string | null; prioridad?: string | null; idEmpleadoAsignado?: number | null; idSkillRequerido?: number | null; deadlineDesde?: string | null; deadlineHasta?: string | null; }
export interface TareaOpciones { proyectos: Proyecto[]; estados: EstadoTarea[]; empleados: UsuarioOpcion[]; skills: Skill[]; }
@Injectable({ providedIn: 'root' })
export class TasksService {
  private readonly http = inject(HttpClient);
  private readonly url='/api/tareas';

  consultar(): Observable<Tarea[]> { return this.http.get<Tarea[]>(this.url); }
  filtrar(filtro: FiltroTarea): Observable<Tarea[]> { return this.http.post<Tarea[]>(`${this.url}/filtrar`, filtro); }
  consultarTableroProyectos(filtro: FiltroTarea): Observable<Tarea[]> { return this.http.post<Tarea[]>(`${this.url}/tablero-proyectos`, filtro); }

  consultarMias(): Observable<Tarea[]> {  return this.http.get<Tarea[]>(`${this.url}/mias`); }
  consultarTableroProyectoPropio(tarea: Partial<Tarea>): Observable<Tarea[]> { return this.http.post<Tarea[]>(`${this.url}/mias/tablero-proyecto`, tarea); }
  consultarPorPM(): Observable<Tarea[]> { return this.http.get<Tarea[]>(`${this.url}/proyectos-pm`); }

  guardarComentarios(tarea: Partial<Tarea>): Observable<boolean> {
    return this.http.patch<boolean>(`${this.url}/mias/comentarios`, tarea);
  }

  opciones(): Observable<TareaOpciones> { return this.http.get<TareaOpciones>(`${this.url}/opciones`); }
  consultarEstados(): Observable<EstadoTarea[]> { return this.http.get<EstadoTarea[]>(`${this.url}/estados`); }
  guardarEstado(estado: Partial<EstadoTarea>): Observable<EstadoTarea> { return estado.id ? this.http.put<EstadoTarea>(`${this.url}/estados/${estado.id}`, estado) : this.http.post<EstadoTarea>(`${this.url}/estados`, estado); }
  desactivarEstado(id: number): Observable<boolean> { return this.http.delete<boolean>(`${this.url}/estados/${id}`); }
  consultarAdjuntos(idTarea: number): Observable<ArchivoAdjuntoTarea[]> { return this.http.get<ArchivoAdjuntoTarea[]>(`${this.url}/${idTarea}/adjuntos`); }
  subirAdjunto(idTarea: number, archivo: File): Observable<ArchivoAdjuntoTarea> { const formData = new FormData(); formData.append('archivo', archivo, archivo.name); return this.http.post<ArchivoAdjuntoTarea>(`${this.url}/${idTarea}/adjuntos`, formData); }
  descargarAdjunto(idTarea: number, idAdjunto: number): Observable<Blob> { return this.http.get(`${this.url}/${idTarea}/adjuntos/${idAdjunto}/contenido`, { responseType: 'blob' }); }
  eliminarAdjunto(idTarea: number, idAdjunto: number): Observable<boolean> { return this.http.delete<boolean>(`${this.url}/${idTarea}/adjuntos/${idAdjunto}`); }
  consultarConfiguracionAlertas(): Observable<ConfiguracionAlertaAgencia> { return this.http.get<ConfiguracionAlertaAgencia>('/api/alertas-operativas/configuracion'); }
  guardarConfiguracionAlertas(configuracion: Partial<ConfiguracionAlertaAgencia>): Observable<ConfiguracionAlertaAgencia> { return this.http.put<ConfiguracionAlertaAgencia>('/api/alertas-operativas/configuracion', configuracion); }
  consultarAsignaciones(idTarea: number): Observable<AsignacionTarea[]> { return this.http.get<AsignacionTarea[]>(`${this.url}/${idTarea}/asignaciones`); }

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

  consultarAreasSkills(): Observable<AreaSkill[]> { return this.http.get<AreaSkill[]>(`${this.url}/skills/areas`); }

  modificarSkill(skill: Partial<Skill>): Observable<Skill> { return this.http.put<Skill>(`${this.url}/skills`, skill); }

  cambiarEstadoSkill(skill: Partial<Skill>): Observable<boolean>{
    return this.http.patch<boolean>(`${this.url}/skills/estado`, skill);
  }
}
