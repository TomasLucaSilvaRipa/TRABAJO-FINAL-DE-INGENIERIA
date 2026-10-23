import { inject } from '@angular/core';
import { CanActivateChildFn, Router } from '@angular/router';
import { AuthService } from '../services/auth.service';

export const subscriptionRestrictionGuard: CanActivateChildFn = (_route, state) => {
  const authService = inject(AuthService);
  const router = inject(Router);

  if (!authService.suscripcionRestringida())
  {
    return true;
  }

  if (authService.tienePermiso('GestionarSuscripcion'))
  {
    return state.url.split('?')[0] === '/dashboard/suscripcion'
      ? true
      : router.createUrlTree(['/dashboard/suscripcion']);
  }

  authService.logOut();
  return router.createUrlTree(['/signin'], { queryParams: { motivo: 'agencia-inactiva' } });
};
