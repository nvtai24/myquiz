import { HttpInterceptorFn, HttpErrorResponse } from "@angular/common/http";
import { inject } from "@angular/core";
import { catchError, switchMap, throwError } from "rxjs";
import { AuthService } from "../services/auth.service";

export const authInterceptor: HttpInterceptorFn = (req, next) => {
    const authReq = req.clone({ withCredentials: true });

    // Skip refresh logic for auth endpoints to avoid loops (case-insensitive)
    const urlLower = req.url.toLowerCase();
    const isAuthCall = urlLower.includes('/auth/refresh-token')
        || urlLower.includes('/auth/login')
        || urlLower.includes('/auth/logout')
        || urlLower.includes('/auth/register');

    if (isAuthCall) {
        return next(authReq);
    }

    const authService = inject(AuthService);

    return next(authReq).pipe(
        catchError((error) => {
            if (error instanceof HttpErrorResponse && error.status === 401) {
                return authService.refreshTokenSilent().pipe(
                    switchMap(() => next(authReq)),
                    catchError(() => {
                        authService.forceLogout();
                        return throwError(() => error);
                    })
                );
            }
            return throwError(() => error);
        })
    );
}