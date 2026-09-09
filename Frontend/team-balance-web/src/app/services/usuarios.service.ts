import { inject, Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { Rol } from './roles.service';

export interface PerfilEmpleado { costoHora: number; horasDisponiblesSemanales: number; seniority: string; estadoLaboral: string; }
export interface UsuarioGestion { id: number; nombre: string; apellido: string; email: string; passwordHash?: string; estado: string; activo: boolean; roles: Rol[]; empleado?: PerfilEmpleado | null; }

@Injectable({ providedIn: 'root' })
export class UsuariosService {
  private readonly http = inject(HttpClient);
  private readonly apiUrl = '/api/usuarios/agencia';

  consultarUsuarios(): Observable<UsuarioGestion[]> { return this.http.get<UsuarioGestion[]>(this.apiUrl); }
  soporteInicialDisponible(): Observable<{ disponible: boolean }> { return this.http.get<{ disponible: boolean }>('/api/usuarios/soporte-inicial-disponible'); }
  consultarRolSoporteInicial(): Observable<Rol> { return this.http.get<Rol>('/api/usuarios/rol-soporte-inicial'); }
  registrarUsuario(usuario: Partial<UsuarioGestion>): Observable<UsuarioGestion> { return this.http.post<UsuarioGestion>(this.apiUrl, usuario); }
  modificarUsuario(usuario: UsuarioGestion): Observable<void> { return this.http.put<void>(`${this.apiUrl}/${usuario.id}`, usuario); }
  cambiarEstado(idUsuario: number, activo: boolean): Observable<void> { return this.http.patch<void>(`${this.apiUrl}/${idUsuario}/estado?activo=${activo}`, {}); }
}
