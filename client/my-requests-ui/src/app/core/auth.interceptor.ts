import { HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { AUTH_TOKEN_GETTER } from './auth-token';
import { AuthSession } from './auth-session';
import { ApiError } from './api-error';
import { HttpErrorResponse } from '@angular/common/http';
import { catchError, throwError } from 'rxjs';

/** Adds "Authorization: Bearer …" to OUR API calls only — the token is never sent to third-party URLs. */
export const authInterceptor: HttpInterceptorFn = (request, next) => {
  if (!request.url.startsWith('/api/')) {
    return next(request);
  }

  const token = inject(AUTH_TOKEN_GETTER)();
  const session = inject(AuthSession);
  const outgoing = token ? request.clone({ setHeaders: { Authorization: `Bearer ${token}` } }) : request;
  return next(outgoing).pipe(catchError((error: unknown) => {
    // Login failures stay on the form. A rejected API token returns the shell to login.
    if (request.url !== '/api/auth/login' &&
        (error instanceof ApiError || error instanceof HttpErrorResponse) && error.status === 401 &&
        token === session.getToken()) session.clear();
    return throwError(() => error);
  }));
};
