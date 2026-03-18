import { Component, inject, signal } from '@angular/core';
import { RouterModule } from '@angular/router';
import { AuthService } from '../services/auth.service';

@Component({
  selector: 'app-main-layout',
  standalone: true,
  imports: [RouterModule],
  templateUrl: './main-layout.html',
  styleUrl: './main-layout.css',
})
export class MainLayout {
  private authService = inject(AuthService);
  currentUser = this.authService.currentUser;
  sidebarCollapsed = signal(true);
  profileMenuOpen = signal(false);

  toggleProfileMenu() {
    this.profileMenuOpen.update(v => !v);
  }

  toggleSidebar() {
    this.sidebarCollapsed.update(v => !v);
  }

  logout() {
    this.authService.logout();
  }
}
