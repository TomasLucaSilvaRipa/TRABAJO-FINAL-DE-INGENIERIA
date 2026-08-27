import { inject, Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

export interface RegistryRequest {
  referenciaContratacion: string;
  nombre: string;
  apellido: string;
  emailLaboral: string;
  telefonoResponsable: string;
  password: string;
}

export interface RegistryResponse {
  mensaje: string;
  emailValidacionEnviado: boolean;
}

export interface LogInResponse {
  accessToken: string;
  expiresAt: string;
}

export interface LogInRequest {
  email: string;
  password: string;
}

@Injectable({
  providedIn: 'root',
})
export class AuthService {
  private readonly http = inject(HttpClient);
  private readonly apiUrl = '/api/auth';

  registrarAgencia(data: RegistryRequest): Observable<RegistryResponse> {
    return this.http.post<RegistryResponse>(`${this.apiUrl}/registry`, data);
  }

  logIn(data: LogInRequest): Observable<LogInResponse> {
    return this.http.post<LogInResponse>(`${this.apiUrl}/login`, data);
  }
}
