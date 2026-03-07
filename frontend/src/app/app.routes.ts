import { Routes } from '@angular/router';
import { Landing } from './pages/landing/landing';
import { Login } from './pages/login/login';
import { Dashboard } from './pages/dashboard/dashboard';
import { authGuard } from './guards/auth.guard';
import { noAuthGuard } from './guards/no-auth.guard';
import { register } from './pages/register/register';
import { VerifyEmail } from './pages/verify-email/verify-email';
import { ForgotPassword } from './pages/forgot-password/forgot-password';
import { ResetPassword } from './pages/reset-password/reset-password';
import { MainLayout } from './layouts/main-layout';
import { Library } from './pages/library/library';
import { AddDeck } from './pages/add-deck/add-deck';
import { DeckDetail } from './pages/deck-detail/deck-detail';
import { Quiz } from './pages/quiz/quiz';
import { Learn } from './pages/learn/learn';
import { QuizResult } from './pages/quiz-result/quiz-result';
import { AccountSettings } from './pages/account-settings/account-settings';
import { UserProfile } from './pages/user-profile/user-profile';
import { SubscriptionSettings } from './pages/subscription-settings/subscription-settings';

export const routes: Routes = [
  // Trang auth: nếu đã đăng nhập → redirect về /dashboard
  { path: 'login',          component: Login,          canActivate: [noAuthGuard] },
  { path: 'register',       component: register,       canActivate: [noAuthGuard] },
  { path: 'forgot-password', component: ForgotPassword, canActivate: [noAuthGuard] },
  { path: 'reset-password', component: ResetPassword,  canActivate: [noAuthGuard] },

  // Verify email không cần guard (có thể mở ngay sau khi click link email)
  { path: 'verify-email',   component: VerifyEmail },
  { path: 'confirm-email',  component: VerifyEmail },

  // Landing page route MUST be before MainLayout so it doesn't get caught by authGuard empty path
  { path: '', component: Landing, canActivate: [noAuthGuard], pathMatch: 'full' },

  // Pages with shared layout (sidebar + navbar)
  {
    path: '',
    component: MainLayout,
    canActivate: [authGuard],
    children: [
      { path: 'dashboard', component: Dashboard },
      { path: 'library', component: Library },
      { path: 'add-deck', component: AddDeck },
      { path: 'deck/:id', component: DeckDetail },
      { path: 'quiz/:id', component: Quiz },
      { path: 'learn/:id', component: Learn },
      { path: 'quiz-result/:id', component: QuizResult },
      { path: 'account-settings', component: AccountSettings },
      { path: 'profile', component: UserProfile },
      { path: 'subscription', component: SubscriptionSettings },
    ]
  },

  { path: '**', redirectTo: '/' }
];
