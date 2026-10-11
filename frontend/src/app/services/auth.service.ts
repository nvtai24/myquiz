import { inject, Injectable, signal, PLATFORM_ID } from "@angular/core";
import { isPlatformBrowser } from "@angular/common";
import { HttpClient } from "@angular/common/http";
import { Router } from "@angular/router";
import { LoginRequest, RegisterRequest, ConfirmEmailRequest, ForgotPasswordRequest, ResetPasswordRequest, User, AuthTokenResponse, RefreshTokenResponse, RegisterResponse } from "../models/auth.models";
import { ApiResponse } from "../models/api.models";
import { Observable, tap, throwError } from "rxjs";

@Injectable({
  providedIn: 'root'
})
export class AuthService {
  private http = inject(HttpClient);
  private router = inject(Router);
  private platformId = inject(PLATFORM_ID);
  private endpoint = '/api/Auth';

  currentUser = signal<User | null>(null);

  constructor() {
    this.loadUserFromStorage();
  }

  login(credentials: LoginRequest): Observable<ApiResponse<AuthTokenResponse>> {
    return this.http.post<ApiResponse<AuthTokenResponse>>(`${this.endpoint}/login`, credentials).pipe(
      tap(response => {
        if (response.success && response.data) {
          this.setSession(response.data);
        }
      })
    );
  }

  register(data: RegisterRequest): Observable<ApiResponse<RegisterResponse>> {
    return this.http.post<ApiResponse<RegisterResponse>>(`${this.endpoint}/register`, data);
  }

  confirmEmail(data: ConfirmEmailRequest): Observable<ApiResponse<void>> {
    return this.http.post<ApiResponse<void>>(`${this.endpoint}/confirm-email`, data);
  }

  forgotPassword(data: ForgotPasswordRequest): Observable<ApiResponse<void>> {
    return this.http.post<ApiResponse<void>>(`${this.endpoint}/forgot-password`, data);
  }

  resetPassword(data: ResetPasswordRequest): Observable<ApiResponse<void>> {
    return this.http.post<ApiResponse<void>>(`${this.endpoint}/reset-password`, data);
  }

  private setSession(authData: AuthTokenResponse): void {
    if (!isPlatformBrowser(this.platformId)) return;
    localStorage.setItem('user', JSON.stringify(authData.user));
    this.currentUser.set(authData.user);
  }

  logout(): void {
    if (!isPlatformBrowser(this.platformId)) return;

    this.http.post(`${this.endpoint}/logout`, {}).subscribe({
      complete: () => {
        this.clearSession();
      },
      error: () => {
        this.clearSession();
      }
    });
  }

  private clearSession(): void {
    localStorage.removeItem('user');
    this.currentUser.set(null);
    this.router.navigate(['/login']);
  }

  /** Called by the interceptor when a 401 occurs — silently renews the access token.
   *  Backend reads refresh token from HttpOnly cookie, withCredentials: true sends it automatically.
   */
  refreshTokenSilent(): Observable<ApiResponse<RefreshTokenResponse>> {
    if (!isPlatformBrowser(this.platformId)) {
      return throwError(() => new Error('Not a browser environment'));
    }
    return this.http.post<ApiResponse<RefreshTokenResponse>>(`${this.endpoint}/refresh-token`, {});
  }

  /** Called by the interceptor when refresh also fails — clear everything and go to login. */
  forceLogout(): void {
    this.clearSession();
  }

  isLoggedIn(): boolean {
    if (!isPlatformBrowser(this.platformId)) return false;
    return !!this.currentUser();
  }

  private loadUserFromStorage(): void {
    if (!isPlatformBrowser(this.platformId)) return;
    const userJson = localStorage.getItem('user');
    if (userJson) {
      try {
        const user: User = JSON.parse(userJson);
        this.currentUser.set(user);
      } catch (error) {
        console.error('Cannot parse user from storage', error);
        this.logout();
      }
    }
  }

  loginWithGoogle(idToken: string): Observable<ApiResponse<AuthTokenResponse>> {
    return this.http.post<ApiResponse<AuthTokenResponse>>(`${this.endpoint}/google-login`, { idToken }).pipe(
      tap(response => {
        if (response.success && response.data) {
          this.setSession(response.data);
        }
      })
    );
  }

  updateUser(partialUser: Partial<User>): void {
    const current = this.currentUser();
    if (current) {
      const updated = { ...current, ...partialUser };
      this.currentUser.set(updated);
      if (isPlatformBrowser(this.platformId)) {
        localStorage.setItem('user', JSON.stringify(updated));
      }
    }
  }
}
