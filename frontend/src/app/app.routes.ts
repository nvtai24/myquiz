import { Routes } from '@angular/router';
import { Login } from './pages/login/login';
import { Dashboard } from './pages/dashboard/dashboard';
import { authGuard } from './guards/auth.guard';
import { noAuthGuard } from './guards/no-auth.guard';
import { register } from './pages/register/register';
import { VerifyEmail } from './pages/verify-email/verify-email';
import { ForgotPassword } from './pages/forgot-password/forgot-password';
import { ResetPassword } from './pages/reset-password/reset-password';

export const routes: Routes = [
  // Trang auth: nếu đã đăng nhập → redirect về /dashboard
  { path: 'login',          component: Login,          canActivate: [noAuthGuard] },
  { path: 'register',       component: register,       canActivate: [noAuthGuard] },
  { path: 'forgot-password', component: ForgotPassword, canActivate: [noAuthGuard] },
  { path: 'reset-password', component: ResetPassword,  canActivate: [noAuthGuard] },

  // Verify email không cần guard (có thể mở ngay sau khi click link email)
  { path: 'verify-email',   component: VerifyEmail },
  { path: 'confirm-email',  component: VerifyEmail },

  // Dashboard: phải đăng nhập
  { path: 'dashboard', component: Dashboard, canActivate: [authGuard] },

  { path: '', redirectTo: '/login', pathMatch: 'full' },
  { path: '**', redirectTo: '/login' }
];
