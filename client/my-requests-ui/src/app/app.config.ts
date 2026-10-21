import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { ApplicationConfig, provideBrowserGlobalErrorListeners, provideZonelessChangeDetection } from '@angular/core';
import { apiErrorInterceptor } from './core/api-error.interceptor';
import { authInterceptor } from './core/auth.interceptor';

export const appConfig: ApplicationConfig = {
  providers: [
    provideBrowserGlobalErrorListeners(),
    // Zoneless: change detection runs on signal writes / events only — the component is built for it (OnPush + Signals).
    provideZonelessChangeDetection(),
    // Order matters: auth adds the header on the way out; apiError translates failures on the way back.
    provideHttpClient(withInterceptors([authInterceptor, apiErrorInterceptor])),
  ],
};
