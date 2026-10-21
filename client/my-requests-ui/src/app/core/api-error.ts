import { HttpErrorResponse } from '@angular/common/http';

/** What the UI needs to know about a failure — not the raw HTTP details. */
export type ApiErrorKind = 'unauthorized' | 'unavailable' | 'unexpected';

export class ApiError extends Error {
  constructor(
    readonly kind: ApiErrorKind,
    readonly status: number,
  ) {
    super(`API error: ${kind} (${status})`);
    this.name = 'ApiError';
  }

  static fromHttp(error: HttpErrorResponse): ApiError {
    if (error.status === 401 || error.status === 403) {
      return new ApiError('unauthorized', error.status);
    }
    // 0 = network failure / CORS / server down. 502-504 = dependency or gateway problems.
    if (error.status === 0 || error.status === 502 || error.status === 503 || error.status === 504) {
      return new ApiError('unavailable', error.status);
    }
    return new ApiError('unexpected', error.status);
  }
}
