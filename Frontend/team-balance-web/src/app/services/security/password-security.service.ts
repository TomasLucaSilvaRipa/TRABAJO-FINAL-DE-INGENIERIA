import { inject, Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

export interface PasswordRequirements {
  longitud: boolean;
  mayuscula: boolean;
  minuscula: boolean;
  numero: boolean;
  caracterEspecial: boolean;
}

export interface PasswordEvaluation {
  valida: boolean;
  puntaje: number;
  nivel: string;
  requisitos: PasswordRequirements;
}

@Injectable({
  providedIn: 'root',
})
export class PasswordSecurityService {
  private readonly http = inject(HttpClient);
  private readonly apiUrl = '/api/password-security/evaluar';

  evaluar(password: string): Observable<PasswordEvaluation> {
    return this.http.post<PasswordEvaluation>(this.apiUrl, { password });
  }
}
