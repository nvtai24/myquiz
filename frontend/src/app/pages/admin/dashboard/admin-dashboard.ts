import { Component, OnInit, inject, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { AdminService } from '../../../services/admin.service';
import { DeckService } from '../../../services/deck.service';

@Component({
  selector: 'app-admin-dashboard',
  standalone: true,
  imports: [CommonModule, RouterModule],
  templateUrl: './admin-dashboard.html',
})
export class AdminDashboard implements OnInit {
  private adminService = inject(AdminService);
  private deckService = inject(DeckService);

  totalDecks = signal(0);
  publicDecks = signal(0);
  totalPlans = signal(0);
  loadingDecks = signal(true);
  loadingPlans = signal(true);

  recentDecks = signal<any[]>([]);

  COLORS = ['#7c3aed', '#4255FF', '#059669', '#dc2626', '#d97706', '#0891b2'];
  ICONS = ['style', 'code', 'science', 'school', 'quiz', 'biotech'];

  getColor(i: number) { return this.COLORS[i % this.COLORS.length]; }
  getIcon(i: number) { return this.ICONS[i % this.ICONS.length]; }

  statCards = computed(() => [
    { label: 'Tổng bộ thẻ', value: this.totalDecks(), icon: 'style', color: '#4255FF', bg: '#eef0ff', change: '+12%' },
    { label: 'Deck công khai', value: this.publicDecks(), icon: 'public', color: '#059669', bg: '#ecfdf5', change: '+5%' },
    { label: 'Gói dịch vụ', value: this.totalPlans(), icon: 'credit_card', color: '#7c3aed', bg: '#f5f3ff', change: null },
  ]);

  ngOnInit(): void {
    this.deckService.getUserDecks().subscribe({
      next: (res) => {
        if (res.success) {
          this.totalDecks.set(res.data.length);
          this.publicDecks.set(res.data.filter((d: any) => d.visibility === 'Public').length);
          this.recentDecks.set(res.data.slice(0, 5));
        }
        this.loadingDecks.set(false);
      },
      error: () => this.loadingDecks.set(false)
    });

    this.adminService.getSubscriptionPlans().subscribe({
      next: (res) => {
        if (res.success) this.totalPlans.set(res.data.length);
        this.loadingPlans.set(false);
      },
      error: () => this.loadingPlans.set(false)
    });
  }
}
