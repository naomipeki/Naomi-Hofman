import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { MyRequestsComponent } from './my-requests/my-requests.component';
import { LoginComponent } from './login/login.component';
import { AuthSession } from './core/auth-session';

@Component({
  selector: 'app-root',
  imports: [MyRequestsComponent, LoginComponent],
  template: `<main>
    @if (session.isAuthenticated()) {
      <nav dir="rtl" aria-label="חשבון"><button type="button" (click)="session.clear()">התנתקות</button></nav>
      <app-my-requests />
    } @else { <app-login /> }
  </main>`,
  styles: `nav { max-width: 1100px; margin: 16px auto 0; padding: 0 24px; }
    nav button { font: inherit; cursor: pointer; background: white; color: #175a91; border: 1px solid #b4c4d2; border-radius: 6px; padding: 6px 16px; }`,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class App { protected readonly session = inject(AuthSession); }
