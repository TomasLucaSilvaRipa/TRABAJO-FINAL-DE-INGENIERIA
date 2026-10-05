import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
export interface Cliente { id: number; nombre: string; razonSocial?: string; email?: string; telefono?: string; activo: boolean; }
export interface UsuarioOpcion { id: number; nombre: string; apellido: string; email: string; }
export interface TareaProyecto { id: number; titulo: string; estado?: string; deadline?: string; activo: boolean; bloqueada: boolean; }
export interface Proyecto { id: number; idCliente: number; idPMResponsable: number; nombre: string; descripcion?: string; fechaInicio?: string; deadline?: string; horasEstimadasTotales: number; estado: string; activo: boolean; tareas?: TareaProyecto[]; }
export interface FiltroProyecto { idCliente?: number | null; idPMResponsable?: number | null; estado?: string | null; fechaDesde?: string | null; fechaHasta?: string | null; }
export interface ProyectoOpciones { clientes: Cliente[]; responsables: UsuarioOpcion[]; }
@Injectable({ providedIn: 'root' })
export class ProjectsService {
  private readonly http = inject(HttpClient);
  private readonly url = '/api/proyectos';

  consultar(): Observable<Proyecto[]> { return this.http.get<Proyecto[]>(this.url); }
  consultarDetalle(proyecto: Proyecto): Observable<Proyecto> { return this.http.post<Proyecto>(`${this.url}/detalle`, proyecto); }
  filtrar(filtro: FiltroProyecto): Observable<Proyecto[]> { return this.http.post<Proyecto[]>(`${this.url}/filtrar`, filtro); }
  opciones(): Observable<ProyectoOpciones> { return this.http.get<ProyectoOpciones>(`${this.url}/opciones`); }
  guardar(proyecto: Partial<Proyecto>): Observable<Proyecto> { return proyecto.id ? this.http.put<Proyecto>(this.url, proyecto) : this.http.post<Proyecto>(this.url, proyecto); }
  cambiarEstado(proyecto: Partial<Proyecto>): Observable<boolean> { return this.http.patch<boolean>(`${this.url}/estado`, proyecto); }
  cerrar(proyecto: Proyecto): Observable<boolean> { return this.http.patch<boolean>(`${this.url}/cerrar`, proyecto); }
  crearCliente(cliente: Partial<Cliente>): Observable<Cliente> { return this.http.post<Cliente>(`${this.url}/clientes`, cliente); }
  modificarCliente(cliente: Cliente): Observable<Cliente> { return this.http.put<Cliente>(`${this.url}/clientes`, cliente); }
  cambiarEstadoCliente(cliente: Partial<Cliente>): Observable<boolean> { return this.http.patch<boolean>(`${this.url}/clientes/estado`, cliente); }
}
