import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { catchError, throwError } from 'rxjs';
import { ApiError } from './api-error';

/**
 * Central error translation: every HTTP failure leaves here as an ApiError with a UI-level kind.
 * Components never inspect status codes. (Could also be the single place for telemetry.)
 */
export const apiErrorInterceptor: HttpInterceptorFn = (request, next) =>
  next(request).pipe(
    catchError((error: unknown) =>
      throwError(() => (error instanceof HttpErrorResponse ? ApiError.fromHttp(error) : error)),
    ),
  );
