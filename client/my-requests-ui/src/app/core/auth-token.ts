import { inject, InjectionToken } from '@angular/core';
import { AuthSession } from './auth-session';

/**
 * Where the bearer token comes from.
 * Defaults to the session persisted by login. An embedding host can override the getter.
 */
export const AUTH_TOKEN_GETTER = new InjectionToken<() => string | null>('AUTH_TOKEN_GETTER', {
  providedIn: 'root',
  factory: () => {
    const session = inject(AuthSession);
    return () => session.getToken();
  },
});
