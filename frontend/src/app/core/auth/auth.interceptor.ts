import type { HttpInterceptorFn, HttpErrorResponse } from '@angular/common/http';
import { inject } from '@angular/core';
import { catchError, switchMap, throwError } from 'rxjs';
import { AuthService } from './auth.service';
import { Router } from '@angular/router';

export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const auth = inject(AuthService);
  const router = inject(Router);
  const requestUrl = req.url.toLowerCase();

  const credentialedReq = requestUrl.includes('/auth/')
    ? req.clone({ withCredentials: true })
    : req;

  const token = auth.accessToken();
  const authReq = token
    ? credentialedReq.clone({ setHeaders: { Authorization: `Bearer ${token}` } })
    : credentialedReq;

  return next(authReq).pipe(
    catchError((error: HttpErrorResponse) => {
      if (error.status === 401 && !requestUrl.includes('/auth/')) {
        return auth.refresh().pipe(
          switchMap(() => {
            const refreshedToken = auth.accessToken();
            const retryReq = req.clone({
              setHeaders: refreshedToken ? { Authorization: `Bearer ${refreshedToken}` } : {},
            });
            return next(retryReq);
          }),
          catchError(() => {
            auth.clearSession();
            router.navigate(['/auth/login']);
            return throwError(() => error);
          })
        );
      }
      return throwError(() => error);
    })
  );
};
