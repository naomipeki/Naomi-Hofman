import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideZonelessChangeDetection } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { apiErrorInterceptor } from '../core/api-error.interceptor';
import { authInterceptor } from '../core/auth.interceptor';
import { AUTH_TOKEN_GETTER } from '../core/auth-token';
import { MyRequest } from './my-request.model';
import { MyRequestsComponent } from './my-requests.component';

const actionRequest: MyRequest = {
  requestId: 'F-88213',
  serviceName: 'הנחה בארנונה',
  authorityName: 'עיריית חדרה',
  status: 'נדרשים מסמכים',
  actionRequired: true,
  isOverdue: false,
  submittedAt: '02/09/2026',
  lastUpdate: '20/09/2026',
};

const overdueRequest: MyRequest = {
  ...actionRequest,
  requestId: 'F-90001',
  status: 'בטיפול',
  actionRequired: false,
  isOverdue: true,
};

describe('MyRequestsComponent', () => {
  let fixture: ComponentFixture<MyRequestsComponent>;
  let http: HttpTestingController;

  function element(): HTMLElement {
    return fixture.nativeElement as HTMLElement;
  }

  async function render(): Promise<void> {
    fixture.detectChanges();
    await fixture.whenStable();
  }

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [MyRequestsComponent],
      providers: [
        // Zoneless in tests: proves the component updates through signals alone (OnPush-safe).
        provideZonelessChangeDetection(),
        provideHttpClient(withInterceptors([authInterceptor, apiErrorInterceptor])),
        provideHttpClientTesting(),
        { provide: AUTH_TOKEN_GETTER, useValue: () => 'test-token' },
      ],
    });
    fixture = TestBed.createComponent(MyRequestsComponent);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  it('shows a loading state, then the requests with CTA and overdue flag', async () => {
    await render();
    expect(element().textContent).toContain('טוען את הבקשות שלך');

    const call = http.expectOne('/api/my-requests');
    expect(call.request.method).toBe('GET');
    expect(call.request.headers.get('Authorization')).toBe('Bearer test-token');
    call.flush({ requests: [actionRequest, overdueRequest] });
    await render();

    const cards = element().querySelectorAll('article');
    expect(cards.length).toBe(2);
    expect(cards[0].textContent).toContain('נדרשת פעולה');
    const cta = cards[0].querySelector<HTMLAnchorElement>('a.request-card__cta');
    expect(cta?.getAttribute('href')).toBe('/requests/F-88213');
    expect(cards[1].textContent).toContain('חריגה מזמן הטיפול');
    expect(cards[1].querySelector('a.request-card__cta')).toBeNull();
  });

  it('shows the exact empty-state text when there are no requests', async () => {
    await render();
    http.expectOne('/api/my-requests').flush({ requests: [] });
    await render();

    expect(element().querySelector('.my-requests__empty')?.textContent?.trim()).toBe('עדיין לא הגשת בקשות');
  });

  it('shows a friendly error with retry when the service is unavailable, and retry reloads', async () => {
    await render();
    http.expectOne('/api/my-requests').flush(null, { status: 503, statusText: 'Service Unavailable' });
    await render();

    const alert = element().querySelector('[role="alert"]');
    expect(alert?.textContent).toContain('לא הצלחנו לטעון');
    const retry = alert?.querySelector<HTMLButtonElement>('button');
    expect(retry).toBeTruthy();

    retry!.click();
    await render();
    http.expectOne('/api/my-requests').flush({ requests: [actionRequest] });
    await render();

    expect(element().querySelector('[role="alert"]')).toBeNull();
    expect(element().querySelectorAll('article').length).toBe(1);
  });

  it('asks to log in again (no retry button) on 401', async () => {
    await render();
    http.expectOne('/api/my-requests').flush(null, { status: 401, statusText: 'Unauthorized' });
    await render();

    const alert = element().querySelector('[role="alert"]');
    expect(alert?.textContent).toContain('יש להתחבר מחדש');
    expect(alert?.querySelector('button')).toBeNull();
  });
});
