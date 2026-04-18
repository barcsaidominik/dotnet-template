import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { AuthService } from './auth.service';

export const roleGuard = (allowedRoles: string[]): CanActivateFn => () => {
  const auth = inject(AuthService);
  const router = inject(Router);

  if (allowedRoles.includes(auth.role() ?? '')) {
    return true;
  }
  return router.createUrlTree(['/']);
};
