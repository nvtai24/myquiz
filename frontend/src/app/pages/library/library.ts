import { Component, inject, signal, OnInit, computed, effect, untracked } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterModule } from '@angular/router';
import { DeckService } from '../../services/deck.service';
import { DeckSummaryResponse } from '../../models/deck.models';
import { PaginationMeta } from '../../models/api.models';
import Swal from 'sweetalert2';

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

  activeTab = signal<'owned' | 'shared' | 'saved' | 'drafts'>('owned');
  searchQuery = signal('');
  sortBy = signal('recent');
  isSortOpen = signal(false);

  // Filters
  selectedSource = signal<'all' | 'AiGenerated' | 'Manual'>('all');
  selectedVisibility = signal<'all' | 'Public' | 'Private' | 'Shared'>('all');
  selectedTags = signal<string[]>([]);
  tagInput = signal('');

  // Pagination (server-side)
  currentPage = signal(1);
  itemsPerPage = 6;
  pagination = signal<PaginationMeta | null>(null);

  // Data
  decks = signal<DeckSummaryResponse[]>([]);
  loading = signal(false);
  errorMessage = signal('');

  // Tab counts
  ownedDecksCount = signal(0);
  sharedDecksCount = signal(0);
  savedDecksCount = signal(0);
  draftsCount = signal(0);

  // All unique tags from decks
  allTags = computed(() => {
    const tags = new Set<string>();
    this.decks().forEach(d => d.tags?.forEach(t => tags.add(t)));
    return Array.from(tags).sort();
  });

  constructor() {
    // Reload data when tab changes
    effect(() => {
      const tab = this.activeTab();
      this.currentPage.set(1);
      untracked(() => this.loadDecks());
    }, { allowSignalWrites: true });
  }

  getDeckColor(index: number): string {
    return DECK_COLORS[index % DECK_COLORS.length];
  }

  getDeckIcon(index: number): string {
    return DECK_ICONS[index % DECK_ICONS.length];
  }

  ngOnInit() {
    this.loadAllCounts();
  }

  loadAllCounts() {
    // Load counts for all tabs
    this.deckService.getMyDecks(1, 1).subscribe(res => {
      if (res.pagination) this.ownedDecksCount.set(res.pagination.totalRecords);
    });
    this.deckService.getSharedDecks(1, 1).subscribe(res => {
      if (res.pagination) this.sharedDecksCount.set(res.pagination.totalRecords);
    });
    this.deckService.getSavedDecks(1, 1).subscribe(res => {
      if (res.pagination) this.savedDecksCount.set(res.pagination.totalRecords);
    });
    this.deckService.getDraftDecks(1, 1).subscribe(res => {
      if (res.pagination) this.draftsCount.set(res.pagination.totalRecords);
    });
  }

  loadDecks() {
    this.loading.set(true);
    this.errorMessage.set('');

    const page = this.currentPage();
    const size = this.itemsPerPage;
    const tab = this.activeTab();

    const apiCall = tab === 'owned'
      ? this.deckService.getMyDecks(page, size)
      : tab === 'shared'
        ? this.deckService.getSharedDecks(page, size)
        : tab === 'saved'
          ? this.deckService.getSavedDecks(page, size)
          : this.deckService.getDraftDecks(page, size);

    apiCall.subscribe({
      next: (res) => {
        this.loading.set(false);
        if (res.success) {
          this.decks.set(res.data || []);
          this.pagination.set(res.pagination || null);
          // Update count for current tab
          if (res.pagination) {
            if (tab === 'owned') this.ownedDecksCount.set(res.pagination.totalRecords);
            else if (tab === 'shared') this.sharedDecksCount.set(res.pagination.totalRecords);
            else if (tab === 'saved') this.savedDecksCount.set(res.pagination.totalRecords);
            else this.draftsCount.set(res.pagination.totalRecords);
          }
        } else {
          this.errorMessage.set(res.message || 'Cannot load decks');
        }
      },
      error: (err) => {
        this.loading.set(false);
        this.errorMessage.set(err.error?.message || 'An error occurred while loading decks');
      },
    });
  }

  get filteredSets(): DeckSummaryResponse[] {
    let deckList = this.decks() || [];

    // Client-side search filter
    if (this.searchQuery()) {
      const q = this.searchQuery().toLowerCase();
      deckList = deckList.filter(s =>
        s?.name?.toLowerCase().includes(q) ||
        s?.description?.toLowerCase().includes(q) ||
        s?.tags?.some(t => t.toLowerCase().includes(q))
      );
    }

    // Client-side visibility filter
    if (this.selectedVisibility() !== 'all') {
      deckList = deckList.filter(d => d.visibility === this.selectedVisibility());
    }

    // Client-side tags filter
    const tags = this.selectedTags();
    if (tags.length > 0) {
      deckList = deckList.filter(d => tags.every(t => d.tags?.includes(t)));
    }

    // Client-side sort
    deckList = [...deckList].sort((a, b) => {
      if (this.sortBy() === 'recent') {
        return new Date(b.createdAt).getTime() - new Date(a.createdAt).getTime();
      }
      if (this.sortBy() === 'terms') return b.questionCount - a.questionCount;
      if (this.sortBy() === 'rating') return (b.averageRating ?? 0) - (a.averageRating ?? 0);
      if (this.sortBy() === 'alpha') return a.name.localeCompare(b.name);
      return 0;
    });

    return deckList;
  }

  get totalPages(): number {
    return this.pagination()?.totalPages || 1;
  }

  get paginatedSets(): DeckSummaryResponse[] {
    return this.filteredSets;
  }

  get hasActiveFilters(): boolean {
    return this.searchQuery() !== '' ||
      this.selectedVisibility() !== 'all' ||
      this.selectedTags().length > 0 ||
      this.sortBy() !== 'recent';
  }

  setTab(tab: 'owned' | 'shared' | 'saved' | 'drafts') {
    if (this.activeTab() !== tab) {
      this.activeTab.set(tab);
    }
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
    if (page >= 1 && page <= this.totalPages && page !== this.currentPage()) {
      this.currentPage.set(page);
      this.loadDecks();
    }
  }

  deleteDeck(deck: DeckSummaryResponse) {
    Swal.fire({
      title: 'Delete Draft',
      text: `Are you sure you want to delete "${deck.name}"? This action cannot be undone.`,
      icon: 'warning',
      showCancelButton: true,
      confirmButtonColor: '#ef4444',
      cancelButtonColor: '#9ca3af',
      confirmButtonText: 'Yes, delete it',
      cancelButtonText: 'Cancel'
    }).then((result) => {
      if (result.isConfirmed) {
        this.deckService.deleteDeck(deck.id).subscribe({
          next: (res) => {
            if (res.success) {
              Swal.fire('Deleted!', 'Your draft has been deleted.', 'success');
              this.loadDecks();
              this.loadAllCounts();
            } else {
              Swal.fire('Error', res.message || 'Failed to delete deck', 'error');
            }
          },
          error: (err) => {
            Swal.fire('Error', err.error?.message || 'An error occurred while deleting', 'error');
          }
        });
      }
    });
  }
}
