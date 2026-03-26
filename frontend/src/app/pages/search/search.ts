import { Component, inject, signal, OnInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterModule, ActivatedRoute, Router } from '@angular/router';
import { Subject, debounceTime, distinctUntilChanged, takeUntil } from 'rxjs';
import { DeckService } from '../../services/deck.service';
import { DeckSummaryResponse } from '../../models/deck.models';
import { PaginationMeta } from '../../models/api.models';

interface Category {
  name: string;
  icon: string;
  color: string;
  count: number;
}

interface FeaturedDeck {
  id: string;
  name: string;
  description: string;
  thumbnailUrl: string | null;
  tags: string[];
  questionCount: number;
  rating: number;
  studyCount: number;
  ownerName: string;
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

  // Mock data for explore page
  categories: Category[] = [
    { name: 'Mathematics', icon: 'calculate', color: '#4255FF', count: 1250 },
    { name: 'Science', icon: 'science', color: '#10b981', count: 980 },
    { name: 'Languages', icon: 'translate', color: '#f59e0b', count: 2100 },
    { name: 'History', icon: 'history_edu', color: '#8b5cf6', count: 750 },
    { name: 'Computer Science', icon: 'code', color: '#06b6d4', count: 1500 },
    { name: 'Medicine', icon: 'medical_services', color: '#ef4444', count: 890 },
    { name: 'Business', icon: 'business_center', color: '#6366f1', count: 620 },
    { name: 'Arts', icon: 'palette', color: '#ec4899', count: 430 },
  ];

  trendingDecks: FeaturedDeck[] = [
    {
      id: '1',
      name: 'Biology 101: Cell Structure',
      description: 'Complete guide to cell biology, organelles, and cellular processes',
      thumbnailUrl: null,
      tags: ['Biology', 'Science', 'Cells'],
      questionCount: 85,
      rating: 4.9,
      studyCount: 12500,
      ownerName: 'Dr. Sarah Chen',
    },
    {
      id: '2',
      name: 'JavaScript Fundamentals',
      description: 'Master JavaScript basics: variables, functions, arrays, and objects',
      thumbnailUrl: null,
      tags: ['Programming', 'JavaScript', 'Web Dev'],
      questionCount: 120,
      rating: 4.8,
      studyCount: 9800,
      ownerName: 'Code Academy',
    },
    {
      id: '3',
      name: 'Spanish Vocabulary - Beginner',
      description: 'Essential Spanish words and phrases for beginners',
      thumbnailUrl: null,
      tags: ['Spanish', 'Language', 'Beginner'],
      questionCount: 200,
      rating: 4.7,
      studyCount: 8500,
      ownerName: 'Maria Garcia',
    },
    {
      id: '4',
      name: 'World War II Timeline',
      description: 'Key events, battles, and figures of World War II',
      thumbnailUrl: null,
      tags: ['History', 'WWII', 'World History'],
      questionCount: 95,
      rating: 4.8,
      studyCount: 7200,
      ownerName: 'History Hub',
    },
  ];

  featuredDecks: FeaturedDeck[] = [
    {
      id: '5',
      name: 'Organic Chemistry Reactions',
      description: 'All major organic chemistry reactions with mechanisms',
      thumbnailUrl: null,
      tags: ['Chemistry', 'Organic', 'Reactions'],
      questionCount: 150,
      rating: 4.9,
      studyCount: 5600,
      ownerName: 'Prof. Johnson',
    },
    {
      id: '6',
      name: 'SAT Vocabulary Master',
      description: 'Top 500 SAT vocabulary words with examples',
      thumbnailUrl: null,
      tags: ['SAT', 'Vocabulary', 'Test Prep'],
      questionCount: 500,
      rating: 4.8,
      studyCount: 15000,
      ownerName: 'Test Prep Pro',
    },
    {
      id: '7',
      name: 'Human Anatomy',
      description: 'Complete human anatomy: bones, muscles, organs',
      thumbnailUrl: null,
      tags: ['Anatomy', 'Medicine', 'Biology'],
      questionCount: 280,
      rating: 4.9,
      studyCount: 11200,
      ownerName: 'Med School Help',
    },
  ];

  newDecks: FeaturedDeck[] = [
    {
      id: '8',
      name: 'Machine Learning Basics',
      description: 'Introduction to ML concepts, algorithms, and applications',
      thumbnailUrl: null,
      tags: ['AI', 'Machine Learning', 'Data Science'],
      questionCount: 75,
      rating: 4.6,
      studyCount: 1200,
      ownerName: 'AI Academy',
    },
    {
      id: '9',
      name: 'French Conjugation',
      description: 'Master French verb conjugations across all tenses',
      thumbnailUrl: null,
      tags: ['French', 'Grammar', 'Verbs'],
      questionCount: 180,
      rating: 4.5,
      studyCount: 890,
      ownerName: 'French Fluent',
    },
    {
      id: '10',
      name: 'Psychology 101',
      description: 'Introduction to psychology: theories, experiments, concepts',
      thumbnailUrl: null,
      tags: ['Psychology', 'Social Science', 'Behavior'],
      questionCount: 110,
      rating: 4.7,
      studyCount: 2100,
      ownerName: 'Mind Matters',
    },
    {
      id: '11',
      name: 'AWS Cloud Practitioner',
      description: 'Prepare for AWS Cloud Practitioner certification',
      thumbnailUrl: null,
      tags: ['AWS', 'Cloud', 'Certification'],
      questionCount: 200,
      rating: 4.8,
      studyCount: 3400,
      ownerName: 'Cloud Guru',
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

  searchByCategory(categoryName: string) {
    this.searchTerm.set(categoryName);
    this.searchSubject.next(categoryName);
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
}
