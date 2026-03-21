import { Component, inject, signal, computed, OnInit, OnDestroy } from '@angular/core';
import { Router, RouterModule, RouterOutlet } from '@angular/router';
import { AuthService } from '../services/auth.service';
import { AdminService } from '../services/admin.service';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-admin-layout',
  standalone: true,
  imports: [RouterModule, RouterOutlet, CommonModule],
  templateUrl: './admin-layout.html',
})
export class AdminLayout implements OnInit, OnDestroy {
  private auth = inject(AuthService);
  private router = inject(Router);
  private adminService = inject(AdminService);
  private pingInterval: any;

  sidebarCollapsed = signal(false);
  user = computed(() => this.auth.currentUser());

  toggleSidebar() {
    this.sidebarCollapsed.update(v => !v);
  }
  isSystemOnline = signal<boolean>(false);

  navItems = [
    { label: 'Dashboard', icon: 'grid_view', path: '/admin/dashboard' },
    { label: 'User Management', icon: 'group', path: '/admin/users' },
    { label: 'Subscription Plans', icon: 'credit_card', path: '/admin/plans' },
    { label: 'Transaction History', icon: 'receipt_long', path: '/admin/transactions' },
    // { label: 'Bộ thẻ (Decks)', icon: 'style', path: '/admin/decks' },
  ];

  logout() {
    this.auth.logout();
  }

  goToApp() {
    this.router.navigate(['/dashboard']);
  }

  ngOnInit() {
    this.checkStatus();
    this.pingInterval = setInterval(() => this.checkStatus(), 60000); // Check every 60 seconds
  }

  ngOnDestroy() {
    if (this.pingInterval) {
      clearInterval(this.pingInterval);
    }
  }

  private checkStatus() {
    this.adminService.checkSystemStatus().subscribe({
      next: (isOnline) => this.isSystemOnline.set(isOnline),
      error: () => this.isSystemOnline.set(false)
    });
  }
}
