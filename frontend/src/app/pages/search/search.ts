import { Component, inject, signal, OnInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterModule, ActivatedRoute, Router } from '@angular/router';
import { Subject, debounceTime, distinctUntilChanged, takeUntil } from 'rxjs';
import { DeckService } from '../../services/deck.service';
import { DeckSummaryResponse } from '../../models/deck.models';
import { PaginationMeta } from '../../models/api.models';

interface ExploreDeck {
  id: string;
  name: string;
  description: string;
  thumbnailUrl: string | null;
  tags: string[];
  questionCount: number;
  averageRating: number;
  viewCount: number;
  ownerName: string;
  createdAt: string;
}

interface HotTag {
  name: string;
  count: number;
}

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

  exploreDecks: ExploreDeck[] = [
    {
      id: '1',
      name: 'Biology 101: Cell Structure',
      description: 'Complete guide to cell biology, organelles, and cellular processes',
      thumbnailUrl: null,
      tags: ['Biology', 'Science', 'Cells'],
      questionCount: 85,
      averageRating: 4.9,
      viewCount: 12500,
      ownerName: 'Dr. Sarah Chen',
      createdAt: '2026-03-10T08:30:00Z',
    },
    {
      id: '2',
      name: 'JavaScript Fundamentals',
      description: 'Master JavaScript basics: variables, functions, arrays, and objects',
      thumbnailUrl: null,
      tags: ['Programming', 'JavaScript', 'Web Dev'],
      questionCount: 120,
      averageRating: 4.8,
      viewCount: 9800,
      ownerName: 'Code Academy',
      createdAt: '2026-03-08T10:15:00Z',
    },
    {
      id: '3',
      name: 'Spanish Vocabulary - Beginner',
      description: 'Essential Spanish words and phrases for beginners',
      thumbnailUrl: null,
      tags: ['Spanish', 'Language', 'Beginner'],
      questionCount: 200,
      averageRating: 4.7,
      viewCount: 8500,
      ownerName: 'Maria Garcia',
      createdAt: '2026-03-18T09:00:00Z',
    },
    {
      id: '4',
      name: 'World War II Timeline',
      description: 'Key events, battles, and figures of World War II',
      thumbnailUrl: null,
      tags: ['History', 'WWII', 'World History'],
      questionCount: 95,
      averageRating: 4.8,
      viewCount: 7200,
      ownerName: 'History Hub',
      createdAt: '2026-03-05T06:20:00Z',
    },
    {
      id: '5',
      name: 'Organic Chemistry Reactions',
      description: 'All major organic chemistry reactions with mechanisms',
      thumbnailUrl: null,
      tags: ['Chemistry', 'Organic', 'Reactions'],
      questionCount: 150,
      averageRating: 4.9,
      viewCount: 5600,
      ownerName: 'Prof. Johnson',
      createdAt: '2026-03-23T13:00:00Z',
    },
    {
      id: '6',
      name: 'SAT Vocabulary Master',
      description: 'Top 500 SAT vocabulary words with examples',
      thumbnailUrl: null,
      tags: ['SAT', 'Vocabulary', 'Test Prep'],
      questionCount: 500,
      averageRating: 4.8,
      viewCount: 15000,
      ownerName: 'Test Prep Pro',
      createdAt: '2026-03-12T12:45:00Z',
    },
    {
      id: '7',
      name: 'Human Anatomy',
      description: 'Complete human anatomy: bones, muscles, organs',
      thumbnailUrl: null,
      tags: ['Anatomy', 'Medicine', 'Biology'],
      questionCount: 280,
      averageRating: 4.9,
      viewCount: 11200,
      ownerName: 'Med School Help',
      createdAt: '2026-03-19T07:40:00Z',
    },
    {
      id: '8',
      name: 'Machine Learning Basics',
      description: 'Introduction to ML concepts, algorithms, and applications',
      thumbnailUrl: null,
      tags: ['AI', 'Machine Learning', 'Data Science'],
      questionCount: 75,
      averageRating: 4.6,
      viewCount: 1200,
      ownerName: 'AI Academy',
      createdAt: '2026-03-25T11:30:00Z',
    },
    {
      id: '9',
      name: 'French Conjugation',
      description: 'Master French verb conjugations across all tenses',
      thumbnailUrl: null,
      tags: ['French', 'Grammar', 'Verbs'],
      questionCount: 180,
      averageRating: 4.5,
      viewCount: 890,
      ownerName: 'French Fluent',
      createdAt: '2026-03-24T09:15:00Z',
    },
    {
      id: '10',
      name: 'Psychology 101',
      description: 'Introduction to psychology: theories, experiments, concepts',
      thumbnailUrl: null,
      tags: ['Psychology', 'Social Science', 'Behavior'],
      questionCount: 110,
      averageRating: 4.7,
      viewCount: 2100,
      ownerName: 'Mind Matters',
      createdAt: '2026-03-22T15:10:00Z',
    },
    {
      id: '11',
      name: 'AWS Cloud Practitioner',
      description: 'Prepare for AWS Cloud Practitioner certification',
      thumbnailUrl: null,
      tags: ['AWS', 'Cloud', 'Certification'],
      questionCount: 200,
      averageRating: 4.8,
      viewCount: 3400,
      ownerName: 'Cloud Guru',
      createdAt: '2026-03-26T04:00:00Z',
    },
  ];

  ngOnInit() {
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
    const counts = new Map<string, number>();

    for (const deck of this.exploreDecks) {
      for (const tag of deck.tags) {
        counts.set(tag, (counts.get(tag) || 0) + 1);
      }
    }

    return Array.from(counts.entries())
      .map(([name, count]) => ({ name, count }))
      .sort((a, b) => b.count - a.count || a.name.localeCompare(b.name))
      .slice(0, 20);
  }

  get trendingDecks(): ExploreDeck[] {
    return [...this.exploreDecks]
      .sort((a, b) => b.viewCount - a.viewCount)
      .slice(0, 4);
  }

  get recommendedDecks(): ExploreDeck[] {
    return [...this.exploreDecks]
      .sort((a, b) => {
        const scoreA = a.averageRating * 100 + a.viewCount;
        const scoreB = b.averageRating * 100 + b.viewCount;
        return scoreB - scoreA;
      })
      .slice(0, 4);
  }

  get newDecks(): ExploreDeck[] {
    return [...this.exploreDecks]
      .sort((a, b) => new Date(b.createdAt).getTime() - new Date(a.createdAt).getTime())
      .slice(0, 4);
  }
}
