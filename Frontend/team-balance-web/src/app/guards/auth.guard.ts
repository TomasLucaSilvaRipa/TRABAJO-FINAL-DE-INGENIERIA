import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { catchError, map, of } from 'rxjs';
import { AuthService } from '../services/auth.service';

export const authGuard: CanActivateFn = () => {
  const router = inject(Router);
  const authService = inject(AuthService);
  const token = localStorage.getItem('token');
  const tokenExpiresAt = localStorage.getItem('tokenExpiresAt');

  if (!token || !tokenExpiresAt || new Date(tokenExpiresAt) <= new Date())
  {
    authService.logOut();
    return router.createUrlTree(['/signin']);
  }

  return authService.validarSesion().pipe(
    map(() => true),
    catchError(() => {
      authService.logOut();
      return of(router.createUrlTree(['/signin']));
    }),
  );
};
