import { Component, inject, signal, OnInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterModule, ActivatedRoute, Router } from '@angular/router';
import { Subject, debounceTime, distinctUntilChanged, takeUntil } from 'rxjs';
import { DeckService } from '../../services/deck.service';
import { DeckSummaryResponse } from '../../models/deck.models';
import { PaginationMeta } from '../../models/api.models';

@Component({
  selector: 'app-search',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterModule],
  templateUrl: './search.html',
  styleUrl: './search.css',
})
export class Search implements OnInit, OnDestroy {
  private deckService = inject(DeckService);
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private destroy$ = new Subject<void>();

  searchTerm = signal('');
  results = signal<DeckSummaryResponse[]>([]);
  loading = signal(false);
  pagination = signal<PaginationMeta | null>(null);
  currentPage = signal(1);
  pageSize = 12;
  hasSearched = signal(false);

  private searchSubject = new Subject<string>();

  ngOnInit() {
    // Read query param on init
    this.route.queryParams
      .pipe(takeUntil(this.destroy$))
      .subscribe(params => {
        const q = params['q'] || '';
        const page = parseInt(params['page'] || '1', 10);
        if (q) {
          this.searchTerm.set(q);
          this.currentPage.set(page);
          this.performSearch(q, page);
        }
      });

    // Debounced search
    this.searchSubject
      .pipe(
        debounceTime(400),
        distinctUntilChanged(),
        takeUntil(this.destroy$)
      )
      .subscribe(term => {
        this.currentPage.set(1);
        this.updateUrl(term, 1);
        this.performSearch(term, 1);
      });
  }

  ngOnDestroy() {
    this.destroy$.next();
    this.destroy$.complete();
  }

  onSearchInput(value: string) {
    this.searchTerm.set(value);
    this.searchSubject.next(value);
  }

  clearSearch() {
    this.searchTerm.set('');
    this.results.set([]);
    this.pagination.set(null);
    this.hasSearched.set(false);
    this.updateUrl('', 1);
  }

  changePage(page: number) {
    this.currentPage.set(page);
    this.updateUrl(this.searchTerm(), page);
    this.performSearch(this.searchTerm(), page);
    // Scroll to top
    window.scrollTo({ top: 0, behavior: 'smooth' });
  }

  private updateUrl(term: string, page: number) {
    this.router.navigate([], {
      relativeTo: this.route,
      queryParams: { q: term || null, page: page > 1 ? page : null },
      queryParamsHandling: 'merge',
      replaceUrl: true,
    });
  }

  private performSearch(term: string, page: number) {
    if (!term.trim()) {
      this.results.set([]);
      this.pagination.set(null);
      this.hasSearched.set(false);
      return;
    }

    this.loading.set(true);
    this.hasSearched.set(true);

    this.deckService.searchDecks(term, page, this.pageSize).subscribe({
      next: (res) => {
        this.loading.set(false);
        if (res.success) {
          this.results.set(res.data || []);
          this.pagination.set(res.pagination || null);
        } else {
          this.results.set([]);
        }
      },
      error: () => {
        this.loading.set(false);
        this.results.set([]);
      },
    });
  }

  get totalPages(): number {
    return this.pagination()?.totalPages || 0;
  }

  get pageNumbers(): number[] {
    const total = this.totalPages;
    const current = this.currentPage();
    const pages: number[] = [];

    if (total <= 7) {
      for (let i = 1; i <= total; i++) pages.push(i);
    } else {
      pages.push(1);
      if (current > 3) pages.push(-1); // ellipsis
      for (let i = Math.max(2, current - 1); i <= Math.min(total - 1, current + 1); i++) {
        pages.push(i);
      }
      if (current < total - 2) pages.push(-1); // ellipsis
      pages.push(total);
    }

    return pages;
  }
}
