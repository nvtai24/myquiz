import { Component, inject, signal, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterModule } from '@angular/router';
import { DeckService } from '../../services/deck.service';
import { DeckSummary } from '../../models/deck.models';

// UI colors/icons for deck cards
const DECK_COLORS = ['#7c3aed', '#4255FF', '#059669', '#64748b', '#dc2626', '#d97706', '#0891b2'];
const DECK_ICONS = ['style', 'code', 'api', 'webhook', 'school', 'menu_book', 'quiz'];

@Component({
  selector: 'app-library',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterModule],
  templateUrl: './library.html',
  styleUrl: './library.css',
})
export class Library implements OnInit {
  private deckService = inject(DeckService);

  activeTab = signal<'all' | 'created' | 'studying' | 'folders'>('all');
  searchQuery = signal('');
  sortBy = signal('recent');
  isSortOpen = signal(false);

  // Pagination
  currentPage = signal(1);
  itemsPerPage = signal(6);

  // Data
  allDecks = signal<DeckSummary[]>([]);
  loading = signal(false);
  errorMessage = signal('');

  // UI helpers
  getDeckColor(index: number): string {
    return DECK_COLORS[index % DECK_COLORS.length];
  }

  getDeckIcon(index: number): string {
    return DECK_ICONS[index % DECK_ICONS.length];
  }

  ngOnInit() {
    this.loadDecks();
  }

  loadDecks() {
    this.loading.set(true);
    this.errorMessage.set('');
    this.deckService.getUserDecks().subscribe({
      next: (res) => {
        this.loading.set(false);
        if (res.success) {
          this.allDecks.set(res.data);
        } else {
          this.errorMessage.set(res.message || 'Failed to load decks');
        }
      },
      error: (err) => {
        this.loading.set(false);
        this.errorMessage.set(err.error?.message || 'An error occurred while loading decks');
      },
    });
  }

  get filteredSets(): DeckSummary[] {
    let decks = this.allDecks().filter(s => {
      if (this.searchQuery()) {
        const q = this.searchQuery().toLowerCase();
        if (!s.name.toLowerCase().includes(q) && !s.description.toLowerCase().includes(q)) {
          return false;
        }
      }
      return true;
    });

    decks.sort((a, b) => {
      if (this.sortBy() === 'recent') {
        return new Date(b.createdAt).getTime() - new Date(a.createdAt).getTime();
      }
      if (this.sortBy() === 'terms') {
        return b.questionCount - a.questionCount;
      }
      return 0;
    });

    return decks;
  }

  get totalPages(): number {
    return Math.ceil(this.filteredSets.length / this.itemsPerPage());
  }

  get paginatedSets(): DeckSummary[] {
    const start = (this.currentPage() - 1) * this.itemsPerPage();
    const end = start + this.itemsPerPage();
    return this.filteredSets.slice(start, end);
  }

  setTab(tab: 'all' | 'created' | 'studying' | 'folders') {
    this.activeTab.set(tab);
    this.currentPage.set(1);
  }

  toggleSort() {
    this.isSortOpen.update(v => !v);
  }

  setSort(sortType: string) {
    this.sortBy.set(sortType);
    this.isSortOpen.set(false);
    this.currentPage.set(1);
  }

  clearFilters() {
    this.searchQuery.set('');
    this.sortBy.set('recent');
    this.currentPage.set(1);
  }

  onSearchChange(query: string) {
    this.searchQuery.set(query);
    this.currentPage.set(1);
  }

  changePage(page: number) {
    if (page >= 1 && page <= this.totalPages) {
      this.currentPage.set(page);
    }
  }
}
