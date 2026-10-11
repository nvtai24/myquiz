import {
  AfterViewInit,
  Component,
  ElementRef,
  OnDestroy,
  OnInit,
  ViewChild,
  inject,
  signal,
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterModule, ActivatedRoute, Router } from '@angular/router';
import { Subject, debounceTime, distinctUntilChanged, forkJoin, takeUntil } from 'rxjs';
import { DeckService } from '../../services/deck.service';
import { DeckSummaryResponse, ExploreDeckResponse } from '../../models/deck.models';
import { PaginationMeta } from '../../models/api.models';

interface ExploreDeck {
  id: string;
  name: string;
  description: string;
  thumbnailUrl: string | null;
  tags: string[];
  questionCount: number;
  averageRating: number;
  totalRatings: number;
  ownerName: string;
  ownerEmail: string;
  createdAt: string;
  updatedAt: string | null;
}

interface HotTag {
  name: string;
}

@Component({
  selector: 'app-search',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterModule],
  templateUrl: './search.html',
  styleUrl: './search.css',
})
export class Search implements OnInit, OnDestroy, AfterViewInit {
  private deckService = inject(DeckService);
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private destroy$ = new Subject<void>();
  private scrollContainer: HTMLElement | Window = window;
  private removeScrollListener?: () => void;

  @ViewChild('searchCompactTrigger') private searchCompactTrigger?: ElementRef<HTMLDivElement>;

  searchTerm = signal('');
  results = signal<DeckSummaryResponse[]>([]);
  loading = signal(false);
  pagination = signal<PaginationMeta | null>(null);
  currentPage = signal(1);
  pageSize = 12;
  hasSearched = signal(false);
  compactSearch = signal(false);
  exploreLoading = signal(false);

  private searchSubject = new Subject<string>();
  private hotTagsData = signal<HotTag[]>([]);
  private recommendedDecksData = signal<ExploreDeck[]>([]);
  private trendingDecksData = signal<ExploreDeck[]>([]);

  ngOnInit() {
    this.loadExploreData();

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
    this.removeScrollListener?.();
    this.destroy$.next();
    this.destroy$.complete();
  }

  ngAfterViewInit() {
    if (!this.searchCompactTrigger?.nativeElement) {
      return;
    }

    this.scrollContainer = this.findScrollContainer(this.searchCompactTrigger.nativeElement);
    const onScroll = () => this.updateCompactSearch();

    this.scrollContainer.addEventListener('scroll', onScroll, { passive: true });
    this.removeScrollListener = () => this.scrollContainer.removeEventListener('scroll', onScroll);

    this.updateCompactSearch();
  }

  private updateCompactSearch() {
    const trigger = this.searchCompactTrigger?.nativeElement;
    if (!trigger) {
      this.compactSearch.set(false);
      return;
    }

    const threshold = 0;

    if (this.scrollContainer === window) {
      this.compactSearch.set(trigger.getBoundingClientRect().top <= threshold);
      return;
    }

    const containerRect = (this.scrollContainer as HTMLElement).getBoundingClientRect();
    const triggerRect = trigger.getBoundingClientRect();
    this.compactSearch.set(triggerRect.top - containerRect.top <= threshold);
  }

  private findScrollContainer(element: HTMLElement): HTMLElement | Window {
    let current = element.parentElement;

    while (current) {
      const style = window.getComputedStyle(current);
      const overflowY = style.overflowY;
      if ((overflowY === 'auto' || overflowY === 'scroll') && current.scrollHeight > current.clientHeight) {
        return current;
      }
      current = current.parentElement;
    }

    return window;
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

  searchByTag(tagName: string) {
    this.searchTerm.set(tagName);
    this.searchSubject.next(tagName);
  }

  changePage(page: number) {
    this.currentPage.set(page);
    this.updateUrl(this.searchTerm(), page);
    this.performSearch(this.searchTerm(), page);
    window.scrollTo({ top: 0, behavior: 'smooth' });
  }

  formatStudyCount(count: number): string {
    if (count >= 1000) {
      return (count / 1000).toFixed(1) + 'k';
    }
    return count.toString();
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

  private loadExploreData() {
    this.exploreLoading.set(true);

    forkJoin({
      hotTags: this.deckService.getExploreHotTags(15),
      recommended: this.deckService.getExploreRecommended(8),
      trending: this.deckService.getExploreTrending(8),
    }).subscribe({
      next: ({ hotTags, recommended, trending }) => {
        this.hotTagsData.set((hotTags.data ?? []).map(name => ({ name })));
        this.recommendedDecksData.set((recommended.data ?? []).map(deck => this.mapExploreDeck(deck)));
        this.trendingDecksData.set((trending.data ?? []).map(deck => this.mapExploreDeck(deck)));
        this.exploreLoading.set(false);
      },
      error: () => {
        this.hotTagsData.set([]);
        this.recommendedDecksData.set([]);
        this.trendingDecksData.set([]);
        this.exploreLoading.set(false);
      },
    });
  }

  private mapExploreDeck(deck: ExploreDeckResponse): ExploreDeck {
    return {
      id: deck.id,
      name: deck.name,
      description: deck.description,
      thumbnailUrl: deck.thumbnailUrl ?? null,
      tags: deck.tags ?? [],
      questionCount: deck.questionCount ?? 0,
      averageRating: deck.averageRating ?? 0,
      totalRatings: deck.totalRatings ?? 0,
      ownerName: deck.ownerName,
      ownerEmail: deck.ownerEmail,
      createdAt: deck.createdAt,
      updatedAt: deck.updatedAt ?? null,
    };
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
      if (current > 3) pages.push(-1);
      for (let i = Math.max(2, current - 1); i <= Math.min(total - 1, current + 1); i++) {
        pages.push(i);
      }
      if (current < total - 2) pages.push(-1);
      pages.push(total);
    }

    return pages;
  }

  get hotTags(): HotTag[] {
    return this.hotTagsData();
  }

  get trendingDecks(): ExploreDeck[] {
    return this.trendingDecksData();
  }

  get recommendedDecks(): ExploreDeck[] {
    return this.recommendedDecksData();
  }
}
