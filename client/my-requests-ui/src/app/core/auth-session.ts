import { Injectable, signal } from '@angular/core';

export const AUTH_TOKEN_KEY = 'auth_token';

@Injectable({ providedIn: 'root' })
export class AuthSession {
  private readonly token = signal<string | null>(localStorage.getItem(AUTH_TOKEN_KEY));
  readonly isAuthenticated = () => this.token() !== null;
  getToken(): string | null { return this.token(); }

  save(token: string): void {
    localStorage.setItem(AUTH_TOKEN_KEY, token);
    this.token.set(token);
  }

  clear(): void {
    localStorage.removeItem(AUTH_TOKEN_KEY);
    this.token.set(null);
  }
}
