import { HttpClient, provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { provideZonelessChangeDetection } from '@angular/core';
import { AuthSession, AUTH_TOKEN_KEY } from './auth-session';
import { AuthService } from './auth.service';
import { authInterceptor } from './auth.interceptor';
import { apiErrorInterceptor } from './api-error.interceptor';

describe('login session', () => {
  let backend: HttpTestingController;
  beforeEach(() => {
    localStorage.removeItem(AUTH_TOKEN_KEY);
    TestBed.configureTestingModule({ providers: [
      provideZonelessChangeDetection(),
      provideHttpClient(withInterceptors([authInterceptor, apiErrorInterceptor])),
      provideHttpClientTesting(),
    ] });
    backend = TestBed.inject(HttpTestingController);
  });
  afterEach(() => { backend.verify(); localStorage.removeItem(AUTH_TOKEN_KEY); });

  it('stores the login token and sends it with the next API request', () => {
    TestBed.inject(AuthService).login(' resident ', 'password').subscribe();
    const login = backend.expectOne('/api/auth/login');
    expect(login.request.body).toEqual({ username: 'resident', password: 'password' });
    login.flush({ token: 'signed-token', expiresAt: '2027-01-01T00:00:00Z' });
    expect(localStorage.getItem(AUTH_TOKEN_KEY)).toBe('signed-token');
    TestBed.inject(HttpClient).get('/api/my-requests').subscribe();
    const requests = backend.expectOne('/api/my-requests');
    expect(requests.request.headers.get('Authorization')).toBe('Bearer signed-token');
    requests.flush({ requests: [] });
  });

  it('restores the stored session after a page reload', () => {
    localStorage.setItem(AUTH_TOKEN_KEY, 'persisted-token');
    expect(TestBed.inject(AuthSession).getToken()).toBe('persisted-token');
    expect(TestBed.inject(AuthSession).isAuthenticated()).toBeTrue();
  });

  it('clears rejected tokens on 401 and returns to login', () => {
    const session = TestBed.inject(AuthSession);
    session.save('expired-token');
    TestBed.inject(HttpClient).get('/api/my-requests').subscribe({ error: () => {} });
    backend.expectOne('/api/my-requests').flush({}, { status: 401, statusText: 'Unauthorized' });
    expect(session.isAuthenticated()).toBeFalse();
    expect(localStorage.getItem(AUTH_TOKEN_KEY)).toBeNull();
  });

  it('keeps the session on a 403 authorization failure', () => {
    const session = TestBed.inject(AuthSession);
    session.save('valid-token');
    TestBed.inject(HttpClient).get('/api/my-requests').subscribe({ error: () => {} });
    backend.expectOne('/api/my-requests').flush({}, { status: 403, statusText: 'Forbidden' });
    expect(session.isAuthenticated()).toBeTrue();
  });

  it('does not store anything after failed login', () => {
    TestBed.inject(AuthService).login('resident', 'wrong').subscribe({ error: () => {} });
    backend.expectOne('/api/auth/login').flush({}, { status: 401, statusText: 'Unauthorized' });
    expect(localStorage.getItem(AUTH_TOKEN_KEY)).toBeNull();
  });

  it('removes persisted credentials on logout', () => {
    const session = TestBed.inject(AuthSession);
    session.save('token');
    session.clear();
    expect(localStorage.getItem(AUTH_TOKEN_KEY)).toBeNull();
    expect(session.isAuthenticated()).toBeFalse();
  });
});
