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
  usuario: UsuarioSesion;
}

export interface UsuarioSesion {
  id: number;
  nombre: string;
  apellido: string;
  email: string;
  idAgencia: number | null;
  rol: RolSesion;
  roles: RolSesion[];
  permisos: PermisoSesion[];
}

export interface RolSesion { id: number; nombre: string; tipoUsuario: string; }
export interface PermisoSesion { id: number; codigo: string; nombre: string; url: string; }

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

  consultarAutorizacion(): Observable<{ usuario: UsuarioSesion; roles: RolSesion[]; permisos: PermisoSesion[] }> {
    return this.http.get<{ usuario: UsuarioSesion; roles: RolSesion[]; permisos: PermisoSesion[] }>(`${this.authApiUrl}/autorizacion`);
  }

  actualizarAutorizacionSesion(): void {
    this.consultarAutorizacion().subscribe({
      next: respuesta => this.guardarUsuario({ ...respuesta.usuario, roles: respuesta.roles, permisos: respuesta.permisos }),
    });
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
    localStorage.removeItem('usuario');
  }

  guardarUsuario(usuario: UsuarioSesion): void {
    localStorage.setItem('usuario', JSON.stringify(usuario));
  }

  usuarioActual(): UsuarioSesion | null {
    const usuario = localStorage.getItem('usuario');
    if (!usuario){ return null; }
    try { return JSON.parse(usuario) as UsuarioSesion; }
    catch { return null; }
  }

  tienePermiso(codigoPermiso: string): boolean {
    return this.usuarioActual()?.permisos?.some(permiso => permiso.codigo === codigoPermiso) ?? false;
  }

  esSoporte(): boolean {
    return this.usuarioActual()?.roles?.some(rol => rol.tipoUsuario === 'Soporte') ?? false;
  }
}
