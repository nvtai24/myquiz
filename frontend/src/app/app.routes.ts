import { Routes } from '@angular/router';
import { authGuard } from './guards/auth.guard';
import { noAuthGuard } from './guards/no-auth.guard';
import { adminGuard } from './guards/admin.guard';
import { MainLayout } from './layouts/main-layout';
import { AdminLayout } from './layouts/admin-layout';

export const routes: Routes = [
  // Auth pages (redirect to dashboard if already logged in)
  { path: 'login', loadComponent: () => import('./pages/login/login').then(m => m.Login), canActivate: [noAuthGuard] },
  { path: 'register', loadComponent: () => import('./pages/register/register').then(m => m.register), canActivate: [noAuthGuard] },
  { path: 'forgot-password', loadComponent: () => import('./pages/forgot-password/forgot-password').then(m => m.ForgotPassword), canActivate: [noAuthGuard] },
  { path: 'reset-password', loadComponent: () => import('./pages/reset-password/reset-password').then(m => m.ResetPassword) },

  // Verify email
  { path: 'verify-email', loadComponent: () => import('./pages/verify-email/verify-email').then(m => m.VerifyEmail) },
  { path: 'confirm-email', loadComponent: () => import('./pages/verify-email/verify-email').then(m => m.VerifyEmail) },

  // Landing
  { path: '', loadComponent: () => import('./pages/landing/landing').then(m => m.Landing), canActivate: [noAuthGuard], pathMatch: 'full' },

  // ===== ADMIN routes (role=Admin required) =====
  {
    path: 'admin',
    component: AdminLayout,
    canActivate: [adminGuard],
    children: [
      { path: '', redirectTo: 'dashboard', pathMatch: 'full' },
      { path: 'dashboard', loadComponent: () => import('./pages/admin/dashboard/admin-dashboard').then(m => m.AdminDashboard) },
      { path: 'users', loadComponent: () => import('./pages/admin/users/admin-users').then(m => m.AdminUsers) },
      { path: 'plans', loadComponent: () => import('./pages/admin/plans/admin-plans').then(m => m.AdminPlans) },
      { path: 'decks', loadComponent: () => import('./pages/admin/decks/admin-decks').then(m => m.AdminDecks) },
      { path: 'transactions', loadComponent: () => import('./pages/admin/transactions/admin-transactions').then(m => m.AdminTransactions) },
    ]
  },

  // ===== Main app layout (authenticated users) =====
  {
    path: '',
    component: MainLayout,
    canActivate: [authGuard],
    children: [
      { path: 'dashboard', loadComponent: () => import('./pages/dashboard/dashboard').then(m => m.Dashboard) },
      { path: 'library', loadComponent: () => import('./pages/library/library').then(m => m.Library) },
      { path: 'search', loadComponent: () => import('./pages/search/search').then(m => m.Search) },
      { path: 'add-deck', loadComponent: () => import('./pages/add-deck/add-deck').then(m => m.AddDeck) },
      { path: 'edit-deck/:id', loadComponent: () => import('./pages/edit-deck/edit-deck').then(m => m.EditDeck) },
      { path: 'deck/:id', loadComponent: () => import('./pages/deck-detail/deck-detail').then(m => m.DeckDetail) },
      { path: 'quiz/:id', loadComponent: () => import('./pages/quiz/quiz').then(m => m.Quiz) },
      { path: 'learn/:id', loadComponent: () => import('./pages/learn/learn').then(m => m.Learn) },
      { path: 'quiz-result/:id', loadComponent: () => import('./pages/quiz-result/quiz-result').then(m => m.QuizResult) },
      { path: 'history', loadComponent: () => import('./pages/quiz-history/quiz-history').then(m => m.QuizHistory) },
      { path: 'accept-invitation', loadComponent: () => import('./pages/accept-invite/accept-invite').then(m => m.AcceptInvite) },
      { path: 'invite/accept', loadComponent: () => import('./pages/accept-invite/accept-invite').then(m => m.AcceptInvite) },
      { path: 'profile', loadComponent: () => import('./pages/user-profile/user-profile').then(m => m.UserProfile) },
      { path: 'subscription', loadComponent: () => import('./pages/subscription-settings/subscription-settings').then(m => m.SubscriptionSettings) },
    ]
  },

  { path: '**', redirectTo: '/' }
];
