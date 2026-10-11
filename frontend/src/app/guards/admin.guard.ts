import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { AuthService } from '../services/auth.service';

export const adminGuard: CanActivateFn = () => {
  const auth = inject(AuthService);
  const router = inject(Router);
  const user = auth.currentUser();
  if (user?.roles?.includes('Admin')) return true;
  if (auth.isLoggedIn()) return router.createUrlTree(['/dashboard']);
  return router.createUrlTree(['/login']);
};
