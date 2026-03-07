import { Routes } from '@angular/router';
import { authGuard } from './guards/auth.guard';
import { noAuthGuard } from './guards/no-auth.guard';
import { MainLayout } from './layouts/main-layout';

export const routes: Routes = [
  // Trang auth: nếu đã đăng nhập → redirect về /dashboard
  { 
    path: 'login',          
    loadComponent: () => import('./pages/login/login').then(m => m.Login),
    canActivate: [noAuthGuard] 
  },
  { 
    path: 'register',       
    loadComponent: () => import('./pages/register/register').then(m => m.register),
    canActivate: [noAuthGuard] 
  },
  { 
    path: 'forgot-password', 
    loadComponent: () => import('./pages/forgot-password/forgot-password').then(m => m.ForgotPassword),
    canActivate: [noAuthGuard] 
  },
  { 
    path: 'reset-password', 
    loadComponent: () => import('./pages/reset-password/reset-password').then(m => m.ResetPassword),
    canActivate: [noAuthGuard] 
  },

  // Verify email
  { 
    path: 'verify-email',   
    loadComponent: () => import('./pages/verify-email/verify-email').then(m => m.VerifyEmail) 
  },
  { 
    path: 'confirm-email',  
    loadComponent: () => import('./pages/verify-email/verify-email').then(m => m.VerifyEmail) 
  },

  // Landing page
  { 
    path: '', 
    loadComponent: () => import('./pages/landing/landing').then(m => m.Landing),
    canActivate: [noAuthGuard], 
    pathMatch: 'full' 
  },

  // Pages with shared layout (sidebar + navbar)
  {
    path: '',
    component: MainLayout,
    canActivate: [authGuard],
    children: [
      { 
        path: 'dashboard', 
        loadComponent: () => import('./pages/dashboard/dashboard').then(m => m.Dashboard) 
      },
      { 
        path: 'library', 
        loadComponent: () => import('./pages/library/library').then(m => m.Library) 
      },
      { 
        path: 'add-deck', 
        loadComponent: () => import('./pages/add-deck/add-deck').then(m => m.AddDeck) 
      },
      { 
        path: 'deck/:id', 
        loadComponent: () => import('./pages/deck-detail/deck-detail').then(m => m.DeckDetail) 
      },
      { 
        path: 'quiz/:id', 
        loadComponent: () => import('./pages/quiz/quiz').then(m => m.Quiz) 
      },
      { 
        path: 'learn/:id', 
        loadComponent: () => import('./pages/learn/learn').then(m => m.Learn) 
      },
      { 
        path: 'quiz-result/:id', 
        loadComponent: () => import('./pages/quiz-result/quiz-result').then(m => m.QuizResult) 
      },
      { 
        path: 'account-settings', 
        loadComponent: () => import('./pages/account-settings/account-settings').then(m => m.AccountSettings) 
      },
      { 
        path: 'profile', 
        loadComponent: () => import('./pages/user-profile/user-profile').then(m => m.UserProfile) 
      },
      { 
        path: 'subscription', 
        loadComponent: () => import('./pages/subscription-settings/subscription-settings').then(m => m.SubscriptionSettings) 
      },
    ]
  },

  { path: '**', redirectTo: '/' }
];
