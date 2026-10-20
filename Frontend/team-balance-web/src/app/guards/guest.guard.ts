import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { catchError, map, of } from 'rxjs';
import { AuthService } from '../services/auth.service';

export const guestGuard: CanActivateFn = () => {
  const router = inject(Router);
  const authService = inject(AuthService);
  const token = authService.obtenerToken();
  const tokenExpiresAt = authService.obtenerVencimientoToken();

  if (!token || !tokenExpiresAt || new Date(tokenExpiresAt) <= new Date()) {
    if (token || tokenExpiresAt) { authService.logOut(); }
    return true;
  }

  return authService.validarSesion().pipe(
    map(() => router.createUrlTree(['/dashboard'])),
    catchError(() => {
      authService.logOut();
      return of(true);
    }),
  );
};
