import { inject, Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

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
  passwordHash: string;
}

export interface EmailValidationResponse {
  mensaje: string;
}

@Injectable({
  providedIn: 'root',
})
export class AuthService {
  private readonly http = inject(HttpClient);
  private readonly agenciasApiUrl = '/api/agencias';
  private readonly authApiUrl = '/api/auth';

  registrarAgencia(referenciaContratacion: string, agencia: object): Observable<RegistryResponse> {
    return this.http.post<RegistryResponse>(
      `${this.agenciasApiUrl}/${referenciaContratacion}/registro`,
      agencia,
    );
  }

  logIn(data: LogInRequest, mantenerSesion: boolean): Observable<LogInResponse> {
    return this.http.post<LogInResponse>(`${this.authApiUrl}/login?mantenerSesion=${mantenerSesion}`, data);
  }

  validarCuenta(token: string): Observable<EmailValidationResponse> {
    return this.http.post<EmailValidationResponse>(`${this.agenciasApiUrl}/validar-cuenta?token=${encodeURIComponent(token)}`, {});
  }

  reenviarValidacion(email: string): Observable<EmailValidationResponse> {
    return this.http.post<EmailValidationResponse>(`${this.agenciasApiUrl}/reenvio-validacion`, { email });
  }

  validarSesion(): Observable<{ vigente: boolean }> {
    return this.http.get<{ vigente: boolean }>(`${this.authApiUrl}/sesion`);
  }

  cerrarSesion(): Observable<void> {
    return this.http.post<void>(`${this.authApiUrl}/logout`, {});
  }

  logOut(): void {
    localStorage.removeItem('token');
    localStorage.removeItem('tokenExpiresAt');
  }
}
