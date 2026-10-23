import { inject, Injectable, signal } from '@angular/core';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { from, map, Observable, switchMap, tap } from 'rxjs';
import { EncryptionService } from './security/encryption.service';

export interface RegistryResponse {
  mensaje: string;
  tokenGestion: string;
  fechaExpiracionValidacion: string;
}

export interface LogInResponse {
  accessToken: string;
  expiresAt: string;
  usuario: UsuarioSesion;
}

export interface DesafioDosFactoresResponse {
  requiereDosFactores: true;
  tokenDesafio: string;
  fechaExpiracion: string;
}

export interface EstadoDosFactoresResponse { totpHabilitado: boolean; fechaActivacionTotp: string | null; }
export interface PreparacionTotpResponse { uriOtpAuth: string; claveManual: string; fechaExpiracion: string; }
export interface ConfirmacionTotpResponse { codigosRecuperacion: string[]; }
export interface EstadoTelegramResponse { vinculado: boolean; estadoSolicitud: string; fechaExpiracionSolicitud: string | null; fechaVinculacion: string | null; }
export interface PreparacionTelegramResponse { enlaceProfundo: string; comandoInicio: string; fechaExpiracion: string; }
export interface EstadoTelegramRegistroResponse { vinculado: boolean; estadoSolicitud: string; fechaExpiracionSolicitud: string | null; }

export interface UsuarioSesion {
  id: number;
  nombre: string;
  apellido: string;
  email: string;
  idAgencia: number | null;
  rol: RolSesion;
  roles: RolSesion[];
  permisos: PermisoSesion[];
  fotoPerfil: string | null;
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

interface FotoPerfilResponse {
  fotoPerfil: string;
}

@Injectable({
  providedIn: 'root',
})
export class AuthService {
  private readonly http = inject(HttpClient);
  private readonly agenciasApiUrl = '/api/agencias';
  private readonly authApiUrl = '/api/auth';
  private readonly encryptionService = inject(EncryptionService);
  private readonly usuarioSesion = signal<UsuarioSesion | null>(this.leerUsuarioActual());
  readonly suscripcionRestringida = signal(false);

  registrarAgencia(referenciaContratacion: string, agencia: object, recibirNewsletter: boolean, categoriasNewsletterIds: number[]): Observable<RegistryResponse> {
    return this.http.post<RegistryResponse>(
      `${this.agenciasApiUrl}/${referenciaContratacion}/registro`,
      { agencia, recibirNewsletter, categoriasNewsletterIds },
    );
  }

  logIn(data: LogInRequest, mantenerSesion: boolean): Observable<LogInResponse | DesafioDosFactoresResponse> {
    return this.http.get( `${this.authApiUrl}/public-key`, { responseType: 'text' }).pipe(
      switchMap(publicKey => from( this.encryptionService.encrypt( { email: data.email, password: data.passwordHash }, publicKey))),
      switchMap(encrypted => { const request: EncryptedLogInRequest = {
          encryptedData: encrypted.encryptedData,
          encryptedKey: encrypted.encryptedKey,
          iv: encrypted.iv,
          recaptchaToken: data.recaptchaToken
        };
        return this.http.post<LogInResponse | DesafioDosFactoresResponse>( `${this.authApiUrl}/login?mantenerSesion=${mantenerSesion}`, request); }));
  }

  confirmarInicioSesionDosFactores(tokenDesafio: string, codigo: string): Observable<LogInResponse> {
    return this.http.post<LogInResponse>(`${this.authApiUrl}/login/confirmar-2fa`, { tokenDesafio, codigo });
  }

  consultarDosFactores(): Observable<EstadoDosFactoresResponse> { return this.http.get<EstadoDosFactoresResponse>(`${this.authApiUrl}/2fa`); }
  prepararTotp(): Observable<PreparacionTotpResponse> { return this.http.post<PreparacionTotpResponse>(`${this.authApiUrl}/2fa/preparar`, {}); }
  confirmarTotp(codigo: string): Observable<ConfirmacionTotpResponse> { return this.http.post<ConfirmacionTotpResponse>(`${this.authApiUrl}/2fa/confirmar`, { codigo }); }
  desactivarTotp(codigo: string): Observable<void> { return this.http.post<void>(`${this.authApiUrl}/2fa/desactivar`, { codigo }); }
  consultarTelegram(): Observable<EstadoTelegramResponse> { return this.http.get<EstadoTelegramResponse>(`${this.authApiUrl}/telegram`); }
  prepararVinculacionTelegram(): Observable<PreparacionTelegramResponse> { return this.http.post<PreparacionTelegramResponse>(`${this.authApiUrl}/telegram/vinculacion`, {}); }
  confirmarVinculacionTelegram(codigo: string): Observable<void> { return this.http.post<void>(`${this.authApiUrl}/telegram/vinculacion/confirmar`, { codigo }); }
  reenviarCodigoTelegram(): Observable<void> { return this.http.post<void>(`${this.authApiUrl}/telegram/vinculacion/reenviar`, {}); }
  prepararDesvinculacionTelegram(): Observable<void> { return this.http.post<void>(`${this.authApiUrl}/telegram/desvinculacion`, {}); }
  confirmarDesvinculacionTelegram(codigo: string): Observable<void> { return this.http.post<void>(`${this.authApiUrl}/telegram/desvinculacion/confirmar`, { codigo }); }

  validarCuenta(token: string): Observable<EmailValidationResponse> {

    return this.http.post<EmailValidationResponse>(`${this.agenciasApiUrl}/validar-cuenta?token=${encodeURIComponent(token)}`, {});
  }

  reenviarValidacion(email: string): Observable<EmailValidationResponse> {
    return this.http.post<EmailValidationResponse>(`${this.agenciasApiUrl}/reenvio-validacion`, { email });
  }

  enviarValidacionEmailRegistro(tokenGestion: string): Observable<void> { return this.http.post<void>(`${this.agenciasApiUrl}/validacion/email`, { tokenGestion }); }
  prepararValidacionTelegramRegistro(tokenGestion: string): Observable<PreparacionTelegramResponse> { return this.http.post<PreparacionTelegramResponse>(`${this.agenciasApiUrl}/validacion/telegram`, { tokenGestion }); }
  consultarValidacionTelegramRegistro(tokenGestion: string): Observable<EstadoTelegramRegistroResponse> { return this.http.get<EstadoTelegramRegistroResponse>(`${this.agenciasApiUrl}/validacion/telegram?tokenGestion=${encodeURIComponent(tokenGestion)}`); }
  confirmarValidacionTelegramRegistro(tokenGestion: string, codigo: string): Observable<EmailValidationResponse> { return this.http.post<EmailValidationResponse>(`${this.agenciasApiUrl}/validacion/telegram/confirmar`, { tokenGestion, codigo }); }
  reenviarValidacionTelegramRegistro(tokenGestion: string): Observable<void> { return this.http.post<void>(`${this.agenciasApiUrl}/validacion/telegram/reenviar`, { tokenGestion }); }

  validarSesion(): Observable<{ vigente: boolean }> {
    return this.http.get<{ vigente: boolean }>(`${this.authApiUrl}/sesion`);
  }

  consultarAutorizacion(): Observable<{ usuario: UsuarioSesion; roles: RolSesion[]; permisos: PermisoSesion[] }> {
    return this.http.get<{ usuario: UsuarioSesion; roles: RolSesion[]; permisos: PermisoSesion[] }>(`${this.authApiUrl}/autorizacion`);
  }

  validarAccesoAgencia(): Observable<void> {
    const headers = new HttpHeaders().set('X-TeamBalance-Subscription-Probe', 'true');
    return this.http.get<unknown[]>(
      '/api/notificaciones',
      { headers },
    ).pipe(map(() => undefined));
  }

  actualizarAutorizacionSesion(): void {
    this.consultarAutorizacion().subscribe({
      next: respuesta => this.guardarUsuario({ ...respuesta.usuario, roles: respuesta.roles, permisos: respuesta.permisos }),
    });
  }

  cerrarSesion(): Observable<void> {
    return this.http.post<void>(`${this.authApiUrl}/logout`, {});
  }

  guardarSesion(respuesta: LogInResponse, mantenerSesion: boolean): void {
    this.logOut();
    const almacenamiento = mantenerSesion ? localStorage : sessionStorage;
    almacenamiento.setItem('token', respuesta.accessToken);
    almacenamiento.setItem('tokenExpiresAt', respuesta.expiresAt);
    almacenamiento.setItem('usuario', JSON.stringify(respuesta.usuario));
    this.usuarioSesion.set(respuesta.usuario);
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

  actualizarFotoPerfil(fotoPerfil: string): Observable<FotoPerfilResponse> {
    return this.http.put<FotoPerfilResponse>('/api/usuarios/perfil/foto', { fotoPerfil }).pipe(
      tap(respuesta => {
        const usuario = this.usuarioActual();
        if (usuario) { this.guardarUsuario({ ...usuario, fotoPerfil: respuesta.fotoPerfil }); }
      }),
    );
  }

  logOut(): void {
    localStorage.removeItem('token');
    localStorage.removeItem('tokenExpiresAt');
    localStorage.removeItem('usuario');
    sessionStorage.removeItem('token');
    sessionStorage.removeItem('tokenExpiresAt');
    sessionStorage.removeItem('usuario');
    this.usuarioSesion.set(null);
    this.suscripcionRestringida.set(false);
  }

  establecerSuscripcionRestringida(restringida: boolean): void {
    this.suscripcionRestringida.set(restringida);
  }

  guardarUsuario(usuario: UsuarioSesion): void {
    this.obtenerAlmacenamientoSesion().setItem('usuario', JSON.stringify(usuario));
    this.usuarioSesion.set(usuario);
  }

  usuarioActual(): UsuarioSesion | null {
    return this.usuarioSesion();
  }

  private leerUsuarioActual(): UsuarioSesion | null {
    const usuario = this.obtenerAlmacenamientoSesion().getItem('usuario');
    if (!usuario){ return null; }
    try { return JSON.parse(usuario) as UsuarioSesion; }
    catch { return null; }
  }

  obtenerToken(): string | null {
    return this.obtenerAlmacenamientoSesion().getItem('token');
  }

  obtenerVencimientoToken(): string | null {
    return this.obtenerAlmacenamientoSesion().getItem('tokenExpiresAt');
  }

  tienePermiso(codigoPermiso: string): boolean {
    return this.usuarioActual()?.permisos?.some(permiso => permiso.codigo === codigoPermiso) ?? false;
  }

  esSoporte(): boolean {
    return this.usuarioActual()?.roles?.some(rol => rol.tipoUsuario === 'Soporte') ?? false;
  }

  private obtenerAlmacenamientoSesion(): Storage {
    return sessionStorage.getItem('token') ? sessionStorage : localStorage;
  }
}
