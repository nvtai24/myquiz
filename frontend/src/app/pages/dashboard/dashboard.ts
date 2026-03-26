import { Component, computed, inject, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { AuthService } from '../../services/auth.service';
import { DeckService } from '../../services/deck.service';
import { DashboardStatsResponse, RecentDeckResponse } from '../../models/deck.models';

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [RouterModule, CommonModule],
  templateUrl: './dashboard.html',
  styleUrl: './dashboard.css',
})
export class Dashboard implements OnInit {
  private authService = inject(AuthService);
  private deckService = inject(DeckService);

  // Loading states
  loadingStats = signal(true);
  loadingRecentDecks = signal(true);

  // Data
  stats = signal<DashboardStatsResponse | null>(null);
  recentDecks = signal<RecentDeckResponse[]>([]);

  userName = computed(() => {
    const user = this.authService.currentUser();
    return user?.firstName || 'there';
  });

  greeting = computed(() => {
    const hour = new Date().getHours();
    if (hour < 12) return 'Good morning';
    if (hour < 18) return 'Good afternoon';
    return 'Good evening';
  });

  ngOnInit() {
    this.loadStats();
    this.loadRecentDecks();
  }

  private loadStats() {
    this.deckService.getDashboardStats().subscribe({
      next: (res) => {
        this.stats.set(res.data ?? null);
        this.loadingStats.set(false);
      },
      error: () => {
        this.loadingStats.set(false);
      },
    });
  }

  private loadRecentDecks() {
    this.deckService.getRecentDecks(4).subscribe({
      next: (res) => {
        this.recentDecks.set(res.data ?? []);
        this.loadingRecentDecks.set(false);
      },
      error: () => {
        this.loadingRecentDecks.set(false);
      },
    });
  }

  getActivityIcon(type: string): string {
    switch (type) {
      case 'Studied': return 'school';
      case 'Saved': return 'bookmark';
      default: return 'visibility';
    }
  }

  getActivityLabel(type: string): string {
    switch (type) {
      case 'Studied': return 'Studied';
      case 'Saved': return 'Saved';
      default: return 'Viewed';
    }
  }

  formatTimeAgo(dateStr: string): string {
    const diff = Date.now() - new Date(dateStr).getTime();
    const minutes = Math.floor(diff / 60000);
    const hours = Math.floor(diff / 3600000);
    const days = Math.floor(diff / 86400000);

    if (minutes < 1) return 'Just now';
    if (minutes < 60) return `${minutes}m ago`;
    if (hours < 24) return `${hours}h ago`;
    if (days === 1) return 'Yesterday';
    if (days < 7) return `${days}d ago`;
    return new Date(dateStr).toLocaleDateString();
  }
}
