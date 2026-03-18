import { Component, inject, signal, OnInit, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterModule } from '@angular/router';
import { DeckService } from '../../services/deck.service';
import { DeckSummary, DeckSource, DeckVisibility } from '../../models/deck.models';

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

  activeTab = signal<'all' | 'created' | 'studying'>('all');
  searchQuery = signal('');
  sortBy = signal('recent');
  isSortOpen = signal(false);

  // Filters
  selectedSource = signal<'all' | 'AiGenerated' | 'Manual'>('all');
  selectedVisibility = signal<'all' | 'Public' | 'Private' | 'Shared'>('all');
  selectedTags = signal<string[]>([]);
  tagInput = signal('');

  // Pagination
  currentPage = signal(1);
  itemsPerPage = signal(6);

  // Data
  allDecks = signal<DeckSummary[]>([]);
  loading = signal(false);
  errorMessage = signal('');

  // All unique tags from decks
  allTags = computed(() => {
    const tags = new Set<string>();
    this.allDecks().forEach(d => d.tags?.forEach(t => tags.add(t)));
    return Array.from(tags).sort();
  });

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
          this.allDecks.set(res.data || []);
        } else {
          this.errorMessage.set(res.message || 'Không thể tải bộ thẻ');
        }
      },
      error: (err) => {
        this.loading.set(false);
        this.errorMessage.set(err.error?.message || 'Đã xảy ra lỗi khi tải bộ thẻ');
      },
    });
  }

  get filteredSets(): DeckSummary[] {
    let decks = this.allDecks() || [];

    // Search by name/description/tags
    if (this.searchQuery()) {
      const q = this.searchQuery().toLowerCase();
      decks = decks.filter(s =>
        s?.name?.toLowerCase().includes(q) ||
        s?.description?.toLowerCase().includes(q) ||
        s?.tags?.some(t => t.toLowerCase().includes(q))
      );
    }

    // Filter by visibility (used as source proxy since DeckSummary has visibility)
    if (this.selectedVisibility() !== 'all') {
      decks = decks.filter(d => d.visibility === this.selectedVisibility());
    }

    // Filter by tags
    const tags = this.selectedTags();
    if (tags.length > 0) {
      decks = decks.filter(d => tags.every(t => d.tags?.includes(t)));
    }

    // Sort
    decks = [...decks].sort((a, b) => {
      if (this.sortBy() === 'recent') {
        return new Date(b.createdAt).getTime() - new Date(a.createdAt).getTime();
      }
      if (this.sortBy() === 'terms') return b.questionCount - a.questionCount;
      if (this.sortBy() === 'rating') return (b.averageRating ?? 0) - (a.averageRating ?? 0);
      if (this.sortBy() === 'alpha') return a.name.localeCompare(b.name);
      return 0;
    });

    return decks;
  }

  get totalPages(): number {
    return Math.ceil(this.filteredSets.length / this.itemsPerPage());
  }

  get paginatedSets(): DeckSummary[] {
    const start = (this.currentPage() - 1) * this.itemsPerPage();
    return this.filteredSets.slice(start, start + this.itemsPerPage());
  }

  get hasActiveFilters(): boolean {
    return this.searchQuery() !== '' ||
      this.selectedVisibility() !== 'all' ||
      this.selectedTags().length > 0 ||
      this.sortBy() !== 'recent';
  }

  setTab(tab: 'all' | 'created' | 'studying') {
    this.activeTab.set(tab);
    this.currentPage.set(1);
  }

  toggleSort() { this.isSortOpen.update(v => !v); }

  setSort(sortType: string) {
    this.sortBy.set(sortType);
    this.isSortOpen.set(false);
    this.currentPage.set(1);
  }

  setVisibility(v: 'all' | 'Public' | 'Private' | 'Shared') {
    this.selectedVisibility.set(v);
    this.currentPage.set(1);
  }

  toggleTag(tag: string) {
    const current = this.selectedTags();
    if (current.includes(tag)) {
      this.selectedTags.set(current.filter(t => t !== tag));
    } else {
      this.selectedTags.set([...current, tag]);
    }
    this.currentPage.set(1);
  }

  clearFilters() {
    this.searchQuery.set('');
    this.sortBy.set('recent');
    this.selectedVisibility.set('all');
    this.selectedTags.set([]);
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
