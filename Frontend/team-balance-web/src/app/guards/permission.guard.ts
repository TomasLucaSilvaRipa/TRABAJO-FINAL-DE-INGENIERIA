import { inject } from '@angular/core';
import { ActivatedRouteSnapshot, CanActivateFn, Router } from '@angular/router';
import { catchError, map, of } from 'rxjs';
import { AuthService } from '../services/auth.service';

export const permissionGuard: CanActivateFn = (route: ActivatedRouteSnapshot) => {
  const authService = inject(AuthService);
  const router = inject(Router);
  const permission = route.data['permission'] as string | undefined;
  const excludedRoleTypes = (route.data['excludedRoleTypes'] as string[] | undefined) ?? [];
  const hasExcludedRole = () => excludedRoleTypes.some(roleType => authService.tieneTipoUsuario(roleType));

  if (hasExcludedRole())
  {
    return router.createUrlTree(['/dashboard']);
  }

  if (!permission || authService.tienePermiso(permission))
  {
    return true;
  }

  return authService.consultarAutorizacion().pipe(
    map(respuesta => {
      authService.guardarUsuario({ ...respuesta.usuario, roles: respuesta.roles, permisos: respuesta.permisos });
      if (hasExcludedRole()) { return router.createUrlTree(['/dashboard']); }
      return authService.tienePermiso(permission) ? true : router.createUrlTree(['/signin']);
    }),
    catchError(() => of(router.createUrlTree(['/signin']))),
  );
};
