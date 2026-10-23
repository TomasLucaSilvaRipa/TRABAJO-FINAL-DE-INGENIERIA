import { inject } from '@angular/core';
import { HttpErrorResponse } from '@angular/common/http';
import { CanActivateFn, Router } from '@angular/router';
import { catchError, map, of } from 'rxjs';
import { AuthService } from '../services/auth.service';

export const authGuard: CanActivateFn = (_route, state) => {
  const router = inject(Router);
  const authService = inject(AuthService);
  const token = authService.obtenerToken();
  const tokenExpiresAt = authService.obtenerVencimientoToken();

  if (!token || !tokenExpiresAt || new Date(tokenExpiresAt) <= new Date())
  {
    authService.logOut();
    return router.createUrlTree(['/signin']);
  }

  return authService.validarAccesoAgencia().pipe(
    map(() => {
      authService.establecerSuscripcionRestringida(false);
      return true;
    }),
    catchError((error: HttpErrorResponse) => {
      if (error.status === 403 && error.error?.code === 'SUBSCRIPTION_EXPIRED')
      {
        if (authService.tienePermiso('GestionarSuscripcion'))
        {
          authService.establecerSuscripcionRestringida(true);
          return of(state.url.split('?')[0] === '/dashboard/suscripcion'
            ? true
            : router.createUrlTree(['/dashboard/suscripcion']));
        }

        authService.logOut();
        return of(router.createUrlTree(['/signin'], { queryParams: { motivo: 'agencia-inactiva' } }));
      }

      authService.logOut();
      return of(router.createUrlTree(['/signin']));
    }),
  );
};
