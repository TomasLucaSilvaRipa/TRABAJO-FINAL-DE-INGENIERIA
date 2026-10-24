import { inject, Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { IdiomaPlataforma } from './localization.service';

@Injectable({ providedIn: 'root' })
export class IdiomasService {
  private readonly http = inject(HttpClient);
  private readonly apiUrl = '/api/idiomas';
  consultarGestion(): Observable<IdiomaPlataforma[]> { return this.http.get<IdiomaPlataforma[]>(`${this.apiUrl}/gestion`); }
  registrar(idioma: Omit<IdiomaPlataforma, 'id'>): Observable<IdiomaPlataforma> { return this.http.post<IdiomaPlataforma>(this.apiUrl, idioma); }
  guardar(idioma: IdiomaPlataforma): Observable<void> { return this.http.put<void>(`${this.apiUrl}/${idioma.id}`, idioma); }
  darDeBaja(idioma: IdiomaPlataforma): Observable<void> { return this.http.delete<void>(`${this.apiUrl}/${idioma.id}`); }
}
