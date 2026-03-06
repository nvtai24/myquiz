import { Component, computed, inject, signal } from '@angular/core';
import { AuthService } from '../../services/auth.service';

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [],
  templateUrl: './dashboard.html',
  styleUrl: './dashboard.css',
})
export class Dashboard {
  private authService = inject(AuthService);
  sidebarCollapsed = signal(false);

  toggleSidebar() {
    this.sidebarCollapsed.update(v => !v);
  }

  userName = computed(() => {
    const user = this.authService.currentUser();
    return user?.firstName || 'there';
  });

  continueStudying = [
    { title: 'Biology 101', terms: 42, topic: 'Molecular Structure', progress: 65, color: '#3b82f6', barColor: '#3b82f6', icon: 'biotech' },
    { title: 'Advanced Calculus', terms: 128, topic: 'Integration Rules', progress: 30, color: '#1e293b', barColor: '#4255FF', icon: 'calculate' },
    { title: 'World History', terms: 85, topic: 'The Renaissance', progress: 85, color: '#a16207', barColor: '#f59e0b', icon: 'public' },
    { title: 'Spanish Verbs', terms: 200, topic: 'Irregular Forms', progress: 12, color: '#b45309', barColor: '#ea580c', icon: 'translate' },
  ];

  recommended = [
    { title: 'Organic Chemistry II', rating: 4.9, reviews: '1.2k', terms: 84, color: '#7c3aed', icon: 'science' },
    { title: 'Comp Sci: Algorithms', rating: 4.8, reviews: '859', terms: 110, color: '#0ea5e9', icon: 'code' },
    { title: 'Intro to Psychology', rating: 4.7, reviews: '2.4k', terms: 92, color: '#ec4899', icon: 'psychology' },
  ];

  logout() {
    this.authService.logout();
  }
}
