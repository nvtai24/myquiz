import { inject, Injectable, signal, PLATFORM_ID } from "@angular/core";
import { isPlatformBrowser } from "@angular/common";
import { HttpClient } from "@angular/common/http";
import { Router } from "@angular/router";
import { LoginRequest, LoginResponse, User, RegisterRequest, RegisterResponse, ConfirmEmailRequest, ConfirmEmailResponse, ForgotPasswordRequest, ForgotPasswordResponse, ResetPasswordRequest, ResetPasswordResponse } from "../models/auth.models";
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

    login(credentials: LoginRequest): Observable<LoginResponse> {
     return this.http.post<LoginResponse>(`${this.endpoint}/login`, credentials).pipe(
        tap(response => {
            if (response.success) {
             this.setSession(response);
            }
        })
     );   
    } 

    register(data: RegisterRequest): Observable<RegisterResponse> {
        return this.http.post<RegisterResponse>(`${this.endpoint}/register`, data);
    }

    confirmEmail(data: ConfirmEmailRequest): Observable<ConfirmEmailResponse> {
        return this.http.post<ConfirmEmailResponse>(`${this.endpoint}/confirm-email`, data);
    }

    forgotPassword(data: ForgotPasswordRequest): Observable<ForgotPasswordResponse> {
        return this.http.post<ForgotPasswordResponse>(`${this.endpoint}/forgot-password`, data);
    }

    resetPassword(data: ResetPasswordRequest): Observable<ResetPasswordResponse> {
        return this.http.post<ResetPasswordResponse>(`${this.endpoint}/reset-password`, data);
    }

    private setSession(authResult: LoginResponse) {
       if (!isPlatformBrowser(this.platformId)) return;
       localStorage.setItem('user', JSON.stringify(authResult.data.user));
       this.currentUser.set(authResult.data.user);
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

    /** Called by the interceptor when a 401 occurs — silently renews the access token. */
    refreshTokenSilent(): Observable<any> {
      if (!isPlatformBrowser(this.platformId)) {
        return throwError(() => new Error('Not a browser environment'));
      }
      return this.http.post<{ success: boolean; data: { expiresAt: string } }>(
        `${this.endpoint}/refresh-token`,
        {}
      );
    }

    /** Called by the interceptor when refresh also fails — clear everything and go to login. */
    forceLogout(): void {
      this.clearSession();
    }

    isLoggedIn(): boolean {
    if (!isPlatformBrowser(this.platformId)) return false;
    return !!this.currentUser();
   }

  private loadUserFromStorage(){
  if (!isPlatformBrowser(this.platformId)) return;
  const userJson = localStorage.getItem('user');
  if (userJson) {
    try {
      const user: User = JSON.parse(userJson);
      this.currentUser.set(user);
    } catch (error) {
      console.error('Không thể parse thông tin user từ storage', error);
      this.logout();
    }
  }
} 
 loginWithGoogle(idToken: string): Observable<LoginResponse> {
    return this.http.post<LoginResponse>(`${this.endpoint}/google-login`, { idToken }).pipe(
      tap(response => {
        if (response.success) {
          this.setSession(response);
        }
      })
    );
  }
}
