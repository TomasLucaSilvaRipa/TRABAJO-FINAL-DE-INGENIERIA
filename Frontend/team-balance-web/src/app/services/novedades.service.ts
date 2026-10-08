import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';

export interface CategoriaNoticia { id: number; nombre: string; descripcion: string; activo: boolean; }
export interface Noticia { id: number; idCategoriaNoticia: number; categoria: string; titulo: string; contenido: string; imagenUrl?: string | null; fechaPublicacion: string; fechaVencimiento?: string | null; activo: boolean; difusionEnviada: boolean; }
export interface GuardarNoticia { idCategoriaNoticia: number; titulo: string; contenido: string; imagenUrl?: string | null; fechaPublicacion?: string | null; fechaVencimiento?: string | null; }

@Injectable({ providedIn: 'root' })
export class NovedadesService {
  private readonly http = inject(HttpClient); private readonly base = '/api/novedades';
  publicas(): Observable<Noticia[]> { return this.http.get<Noticia[]>(`${this.base}/publicas`); }
  categorias(): Observable<CategoriaNoticia[]> { return this.http.get<CategoriaNoticia[]>(`${this.base}/categorias`); }
  preferencias(): Observable<number[]> { return this.http.get<number[]>(`${this.base}/preferencias`); }
  guardarPreferencias(categoriasIds: number[]): Observable<void> { return this.http.put<void>(`${this.base}/preferencias`, { categoriasIds }); }
  gestion(): Observable<Noticia[]> { return this.http.get<Noticia[]>(`${this.base}/gestion`); }
  guardar(noticia: GuardarNoticia): Observable<Noticia> { return this.http.post<Noticia>(this.base, noticia); }
  baja(id: number): Observable<void> { return this.http.post<void>(`${this.base}/${id}/baja`, {}); }
}
