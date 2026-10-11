import { Component, inject, signal, OnInit } from '@angular/core';
import { RouterModule } from '@angular/router';
import { HttpClient } from '@angular/common/http';
import { AuthService } from '../services/auth.service';
import { ApiResponse } from '../models/api.models';
import { ProfileResponse } from '../models/profile.models';

@Component({
  selector: 'app-main-layout',
  standalone: true,
  imports: [RouterModule],
  templateUrl: './main-layout.html',
  styleUrl: './main-layout.css',
})
export class MainLayout implements OnInit {
  private authService = inject(AuthService);
  private http = inject(HttpClient);
  currentUser = this.authService.currentUser;
  sidebarCollapsed = signal(false);
  profileMenuOpen = signal(false);

  ngOnInit() {
    this.http.get<ApiResponse<ProfileResponse>>('/api/profile').subscribe({
      next: (res) => {
        const data = res.data;
        if (data) {
          this.authService.updateUser({
            firstName: data.firstName,
            lastName: data.lastName,
            avatarUrl: data.avatarUrl,
          });
        }
      }
    });
  }

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
