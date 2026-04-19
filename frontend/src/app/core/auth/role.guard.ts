import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { AuthService } from './auth.service';

export const roleGuard = (allowedRoles: string[]): CanActivateFn => () => {
  const auth = inject(AuthService);
  const router = inject(Router);
  const role = auth.role() ?? '';

  if (allowedRoles.includes(role)) {
    return true;
  }

  if (role === 'SystemAdmin') {
    return router.createUrlTree(['/admin/users']);
  }
  if (role === 'FacilityAdmin') {
    return router.createUrlTree(['/facility/users']);
  }
  return router.createUrlTree(['/products']);
};
