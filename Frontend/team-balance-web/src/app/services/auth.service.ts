import { inject, Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { from, Observable,switchMap  } from 'rxjs';
import { EncryptionService } from './security/encryption.service';

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
  recaptchaToken: string;
}

interface EncryptedLogInRequest {
  encryptedData: string;
  encryptedKey: string;
  iv: string;
  recaptchaToken: string;
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
  private readonly encryptionService = inject(EncryptionService);

  registrarAgencia(referenciaContratacion: string, agencia: object): Observable<RegistryResponse> {
    return this.http.post<RegistryResponse>(
      `${this.agenciasApiUrl}/${referenciaContratacion}/registro`,
      agencia,
    );
  }

  logIn(data: LogInRequest, mantenerSesion: boolean): Observable<LogInResponse> {
    return this.http.get( `${this.authApiUrl}/public-key`, { responseType: 'text' }).pipe(
      switchMap(publicKey => from( this.encryptionService.encrypt( { email: data.email, password: data.passwordHash }, publicKey))),
      switchMap(encrypted => { const request: EncryptedLogInRequest = {
          encryptedData: encrypted.encryptedData,
          encryptedKey: encrypted.encryptedKey,
          iv: encrypted.iv,
          recaptchaToken: data.recaptchaToken
        };
        return this.http.post<LogInResponse>( `${this.authApiUrl}/login?mantenerSesion=${mantenerSesion}`, request); }));
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

  solicitarRecuperoPassword(email: string): Observable<EmailValidationResponse> {
    return this.http.post<EmailValidationResponse>(`${this.authApiUrl}/recuperar-password`, { email });
  }

  restablecerPassword(token: string, passwordHash: string): Observable<EmailValidationResponse> {
    return this.http.post<EmailValidationResponse>(`${this.authApiUrl}/restablecer-password?token=${encodeURIComponent(token)}`, { passwordHash });
  }

  cambiarPassword(passwordActual: string, passwordHash: string): Observable<EmailValidationResponse> {
    return this.http.post<EmailValidationResponse>(`${this.authApiUrl}/cambiar-password`, { passwordActual, passwordHash });
  }

  logOut(): void {
    localStorage.removeItem('token');
    localStorage.removeItem('tokenExpiresAt');
  }
}
