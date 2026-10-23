import { HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { catchError, throwError } from 'rxjs';
import { AuthService } from '../services/auth.service';

export const authInterceptor: HttpInterceptorFn = (request, next) => {
  const router = inject(Router);
  const authService = inject(AuthService);
  const token = authService.obtenerToken();
  const tokenExpiresAt = authService.obtenerVencimientoToken();
  const tokenVencido = !tokenExpiresAt || new Date(tokenExpiresAt) <= new Date();

  if (tokenVencido && token)
  {
    authService.logOut();
  }

  const esVerificacionAcceso = request.headers.has('X-TeamBalance-Subscription-Probe');
  const requestSinMarcaInterna = esVerificacionAcceso
    ? request.clone({ headers: request.headers.delete('X-TeamBalance-Subscription-Probe') })
    : request;
  const requestConToken = token && !tokenVencido && requestSinMarcaInterna.url.startsWith('/api/')
    ? requestSinMarcaInterna.clone({
        setHeaders: {
          Authorization: `Bearer ${token}`,
        },
      })
    : requestSinMarcaInterna;

  return next(requestConToken).pipe(
    catchError((error) => {
      if (error.status === 401 && !request.url.includes('/api/auth/login'))
      {
        authService.logOut();
        void router.navigate(['/signin']);
      }
      else if (error.status === 403 && error.error?.code === 'SUBSCRIPTION_EXPIRED')
      {
        authService.establecerSuscripcionRestringida(true);
        const esGestionSuscripcion = router.url.split('?')[0] === '/dashboard/suscripcion';
        if (!esGestionSuscripcion && !esVerificacionAcceso)
        {
          const destino = authService.tienePermiso('GestionarSuscripcion') ? '/dashboard/suscripcion' : '/suscripcion-vencida';
          void router.navigate([destino]);
        }
      }

      return throwError(() => error);
    }),
  );
};
