import { Component, inject, signal, computed } from '@angular/core';
import { Router, RouterModule, RouterOutlet } from '@angular/router';
import { AuthService } from '../services/auth.service';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-admin-layout',
  standalone: true,
  imports: [RouterModule, RouterOutlet, CommonModule],
  templateUrl: './admin-layout.html',
})
export class AdminLayout {
  private auth = inject(AuthService);
  private router = inject(Router);

  sidebarOpen = signal(true);
  user = computed(() => this.auth.currentUser());

  navItems = [
    { label: 'Dashboard', icon: 'grid_view', path: '/admin/dashboard' },
    { label: 'Quản lý người dùng', icon: 'group', path: '/admin/users' },
    { label: 'Gói đăng ký', icon: 'credit_card', path: '/admin/plans' },
    { label: 'Bộ thẻ (Decks)', icon: 'style', path: '/admin/decks' },
  ];

  logout() {
    this.auth.logout();
  }

  goToApp() {
    this.router.navigate(['/dashboard']);
  }
}
