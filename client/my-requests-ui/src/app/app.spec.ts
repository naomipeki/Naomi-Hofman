import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideZonelessChangeDetection } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { App } from './app';
import { AUTH_TOKEN_KEY } from './core/auth-session';
import { authInterceptor } from './core/auth.interceptor';
import { apiErrorInterceptor } from './core/api-error.interceptor';

describe('authenticated application', () => {
  beforeEach(() => {
    localStorage.removeItem(AUTH_TOKEN_KEY);
    TestBed.configureTestingModule({
      imports: [App],
      providers: [provideZonelessChangeDetection(),
        provideHttpClient(withInterceptors([authInterceptor, apiErrorInterceptor])), provideHttpClientTesting()],
    });
  });
  afterEach(() => {
    TestBed.inject(HttpTestingController).verify();
    localStorage.removeItem(AUTH_TOKEN_KEY);
  });

  function enterCredentials(element: HTMLElement) {
    for (const [id, value] of [['username', 'resident'], ['password', 'test-password']]) {
      const input = element.querySelector<HTMLInputElement>(`#${id}`)!;
      input.value = value;
      input.dispatchEvent(new Event('input'));
    }
    element.querySelector('form')!.dispatchEvent(new Event('submit', { bubbles: true, cancelable: true }));
  }

  it('shows login, then requests after successful login, and login again after logout', async () => {
    const fixture = TestBed.createComponent(App);
    const backend = TestBed.inject(HttpTestingController);
    fixture.detectChanges();
    const element: HTMLElement = fixture.nativeElement;
    expect(element.querySelector('app-login')).not.toBeNull();
    expect(element.querySelector('app-my-requests')).toBeNull();
    enterCredentials(element);
    backend.expectOne('/api/auth/login').flush({ token: 'test-token', expiresAt: '2027-01-01' });
    await fixture.whenStable();
    const request = backend.expectOne('/api/my-requests');
    expect(request.request.headers.get('Authorization')).toBe('Bearer test-token');
    request.flush({ requests: [] });
    await fixture.whenStable();
    expect(element.querySelector('app-login')).toBeNull();
    element.querySelector<HTMLButtonElement>('nav button')!.click();
    await fixture.whenStable();
    expect(element.querySelector('app-login')).not.toBeNull();
    expect(localStorage.getItem(AUTH_TOKEN_KEY)).toBeNull();
  });

  it('shows failed credentials without loading protected data', async () => {
    const fixture = TestBed.createComponent(App);
    const backend = TestBed.inject(HttpTestingController);
    fixture.detectChanges();
    enterCredentials(fixture.nativeElement);
    backend.expectOne('/api/auth/login').flush({}, { status: 401, statusText: 'Unauthorized' });
    await fixture.whenStable();
    expect(fixture.nativeElement.querySelector('[role="alert"]').textContent).toContain('שם המשתמש או הסיסמה שגויים');
    expect(fixture.nativeElement.querySelector('#password').value).toBe('');
    backend.expectNone('/api/my-requests');
  });
});
