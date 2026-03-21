import { Component, OnInit, inject, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { AdminService } from '../../../services/admin.service';
import { AdminDeckSummaryResponse } from '../../../models/admin.models';

@Component({
  selector: 'app-admin-decks',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './admin-decks.html',
})
export class AdminDecks implements OnInit {
  private adminService = inject(AdminService);

  decks = signal<AdminDeckSummaryResponse[]>([]);
  loading = signal(true);
  error = signal<string | null>(null);
  searchQuery = signal('');
  filterVisibility = signal<'All' | 'Public' | 'Private' | 'Shared'>('All');
  currentPage = signal(1);
  readonly PAGE_SIZE = 10;

  ngOnInit() { this.loadDecks(); }

  loadDecks() {
    this.loading.set(true);
    this.adminService.getAllDecks().subscribe({
      next: (res) => {
        this.decks.set(res.data ?? []);
        this.loading.set(false);
      },
      error: (err) => {
        this.error.set(err?.error?.message ?? 'Could not load decks list');
        this.loading.set(false);
      }
    });
  }

  filteredDecks = computed(() => {
    let decks = this.decks();
    const q = this.searchQuery().toLowerCase();
    if (q) decks = decks.filter(d => d.name.toLowerCase().includes(q));
    if (this.filterVisibility() !== 'All') decks = decks.filter(d => d.visibility === this.filterVisibility());
    return decks;
  });

  pagedDecks = computed(() => {
    const start = (this.currentPage() - 1) * this.PAGE_SIZE;
    return this.filteredDecks().slice(start, start + this.PAGE_SIZE);
  });

  totalPages = computed(() => Math.ceil(this.filteredDecks().length / this.PAGE_SIZE));

  prevPage() { if (this.currentPage() > 1) this.currentPage.update(p => p - 1); }
  nextPage() { if (this.currentPage() < this.totalPages()) this.currentPage.update(p => p + 1); }

  getVisibilityStyle(v: string) {
    if (v === 'Public') return 'bg-green-50 text-green-700 border-green-200';
    if (v === 'Private') return 'bg-gray-50 text-gray-600 border-gray-200';
    return 'bg-blue-50 text-blue-600 border-blue-200';
  }

  ICONS = ['style', 'code', 'science', 'school', 'quiz', 'biotech', 'history_edu', 'calculate'];
  COLORS = ['#4255FF', '#7c3aed', '#059669', '#dc2626', '#d97706', '#0891b2', '#be185d', '#374151'];
  getIcon(i: number) { return this.ICONS[i % this.ICONS.length]; }
  getColor(i: number) { return this.COLORS[i % this.COLORS.length]; }
}
