import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { tap } from 'rxjs';
import { AuthSession } from './auth-session';

export interface LoginResponse { token: string; expiresAt: string; }

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly http = inject(HttpClient);
  private readonly session = inject(AuthSession);

  login(username: string, password: string) {
    return this.http.post<LoginResponse>('/api/auth/login', { username: username.trim(), password })
      .pipe(tap(response => this.session.save(response.token)));
  }
}
