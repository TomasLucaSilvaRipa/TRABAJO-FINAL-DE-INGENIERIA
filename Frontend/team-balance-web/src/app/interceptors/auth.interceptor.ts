import { HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { catchError, throwError } from 'rxjs';
import { AuthService } from '../services/auth.service';

export const authInterceptor: HttpInterceptorFn = (request, next) => {
  const router = inject(Router);
  const authService = inject(AuthService);
  const token = localStorage.getItem('token');
  const tokenExpiresAt = localStorage.getItem('tokenExpiresAt');
  const tokenVencido = !tokenExpiresAt || new Date(tokenExpiresAt) <= new Date();

  if (tokenVencido && token)
  {
    authService.logOut();
  }

  const requestConToken = token && !tokenVencido && request.url.startsWith('/api/')
    ? request.clone({
        setHeaders: {
          Authorization: `Bearer ${token}`,
        },
      })
    : request;

  return next(requestConToken).pipe(
    catchError((error) => {
      if (error.status === 401 && !request.url.includes('/api/auth/login'))
      {
        authService.logOut();
        void router.navigate(['/signin']);
      }

      return throwError(() => error);
    }),
  );
};
