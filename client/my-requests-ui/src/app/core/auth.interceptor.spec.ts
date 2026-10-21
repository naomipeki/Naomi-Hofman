import { HttpClient, provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideZonelessChangeDetection } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { AUTH_TOKEN_GETTER } from './auth-token';
import { authInterceptor } from './auth.interceptor';

describe('authInterceptor', () => {
  function setup(token: string | null) {
    TestBed.configureTestingModule({
      providers: [
        provideZonelessChangeDetection(),
        provideHttpClient(withInterceptors([authInterceptor])),
        provideHttpClientTesting(),
        { provide: AUTH_TOKEN_GETTER, useValue: () => token },
      ],
    });
    return { http: TestBed.inject(HttpClient), backend: TestBed.inject(HttpTestingController) };
  }

  it('never sends the token to a foreign URL', () => {
    const { http, backend } = setup('secret');
    http.get('https://example.org/other').subscribe();
    expect(backend.expectOne('https://example.org/other').request.headers.has('Authorization')).toBeFalse();
    backend.verify();
  });

  it('sends no header when the host provides no token', () => {
    const { http, backend } = setup(null);
    http.get('/api/my-requests').subscribe();
    expect(backend.expectOne('/api/my-requests').request.headers.has('Authorization')).toBeFalse();
    backend.verify();
  });
});
