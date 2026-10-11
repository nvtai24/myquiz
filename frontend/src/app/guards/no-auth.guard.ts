import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { AuthService } from '../services/auth.service';

/**
 * Guard cho các trang "chưa đăng nhập" (login, register, forgot-password, reset-password).
 * Nếu đã đăng nhập rồi → tự động chuyển về /dashboard.
 */
export const noAuthGuard: CanActivateFn = () => {
  const authService = inject(AuthService);
  const router = inject(Router);

  if (authService.isLoggedIn()) {
    return router.createUrlTree(['/dashboard']);
  }

  return true;
};
