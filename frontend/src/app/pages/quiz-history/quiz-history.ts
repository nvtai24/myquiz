import { Component, inject, signal, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { DeckService } from '../../services/deck.service';
import { QuizAttemptHistoryResponse } from '../../models/deck.models';

@Component({
  selector: 'app-quiz-history',
  standalone: true,
  imports: [CommonModule, RouterModule],
  templateUrl: './quiz-history.html',
  styleUrls: ['./quiz-history.css'],
})
export class QuizHistory implements OnInit {
  private deckService = inject(DeckService);

  attempts = signal<QuizAttemptHistoryResponse[]>([]);
  loading = signal(true);
  error = signal<string | null>(null);

  currentPage = signal(1);
  totalPages = signal(1);
  totalItems = signal(0);
  pageSize = 10;

  ngOnInit() {
    this.loadAttemptedDecks();
  }

  loadAttemptedDecks() {
    this.loading.set(true);
    this.error.set(null);

    this.deckService.getAttemptedDecks(this.currentPage(), this.pageSize).subscribe({
      next: (res) => {
        this.loading.set(false);
        if (res.success && res.data) {
          this.attempts.set(res.data);
          this.totalPages.set(res.pagination?.totalPages || 1);
          this.totalItems.set(res.pagination?.totalRecords || 0);
        } else {
          this.attempts.set([]);
        }
      },
      error: (err) => {
        this.loading.set(false);
        this.error.set('Failed to load quiz history');
        console.error(err);
      },
    });
  }

  goToPage(page: number) {
    if (page < 1 || page > this.totalPages()) return;
    this.currentPage.set(page);
    this.loadAttemptedDecks();
  }

  getPageNumbers(): number[] {
    const total = this.totalPages();
    const current = this.currentPage();
    const pages: number[] = [];

    if (total <= 5) {
      for (let i = 1; i <= total; i++) pages.push(i);
    } else {
      if (current <= 3) {
        pages.push(1, 2, 3, 4, 5);
      } else if (current >= total - 2) {
        pages.push(total - 4, total - 3, total - 2, total - 1, total);
      } else {
        pages.push(current - 2, current - 1, current, current + 1, current + 2);
      }
    }

    return pages;
  }

  formatDate(dateStr: string): string {
    const d = new Date(dateStr);
    return d.toLocaleDateString('vi-VN', {
      day: '2-digit',
      month: '2-digit',
      year: 'numeric',
      hour: '2-digit',
      minute: '2-digit',
    });
  }

  formatTime(ms: number): string {
    const totalSeconds = Math.floor(ms / 1000);
    const mins = Math.floor(totalSeconds / 60);
    const secs = totalSeconds % 60;
    if (mins > 0) {
      return `${mins}m ${secs}s`;
    }
    return `${secs}s`;
  }

  getScoreColor(percent: number): string {
    if (percent >= 80) return 'text-green-600 bg-green-50';
    if (percent >= 60) return 'text-yellow-600 bg-yellow-50';
    return 'text-red-600 bg-red-50';
  }
}
