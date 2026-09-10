import { inject, Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

export interface Permiso { id: number; nombre: string; descripcion: string | null; codigo: string; url: string; activo: boolean; }
export type TipoUsuarioRol = 'Dueno' | 'PM' | 'Empleado' | 'Soporte';
export interface Rol { id: number; idAgencia: number | null; nombre: string; descripcion: string | null; tipoUsuario: TipoUsuarioRol; esRolBase: boolean; activo: boolean; permisos: Permiso[]; }

@Injectable({ providedIn: 'root' })
export class RolesService {
  private readonly http = inject(HttpClient);
  private readonly apiUrl = '/api/roles';

  consultarRoles(): Observable<Rol[]> { return this.http.get<Rol[]>(this.apiUrl); }
  consultarPermisos(): Observable<Permiso[]> { return this.http.get<Permiso[]>(`${this.apiUrl}/permisos`); }
  consultarRolesAsignablesAgencia(): Observable<Rol[]> { return this.http.get<Rol[]>(`${this.apiUrl}/asignables-agencia`); }
  registrarRol(rol: Partial<Rol>): Observable<Rol> { return this.http.post<Rol>(this.apiUrl, rol); }
  modificarRol(rol: Rol): Observable<void> { return this.http.put<void>(`${this.apiUrl}/${rol.id}`, rol); }
  cambiarEstado(idRol: number, activo: boolean): Observable<void> { return this.http.patch<void>(`${this.apiUrl}/${idRol}/estado?activo=${activo}`, {}); }
}
