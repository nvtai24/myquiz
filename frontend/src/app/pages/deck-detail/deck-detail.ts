import { Component, OnInit, inject, signal, computed, PLATFORM_ID } from '@angular/core';
import { isPlatformBrowser } from '@angular/common';
import { ActivatedRoute, Router, RouterModule } from '@angular/router';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { DeckService } from '../../services/deck.service';
import { AuthService } from '../../services/auth.service';

interface QuizAttemptSummary {
  id: string;
  score: number;
  totalCorrect: number;
  totalQuestions: number;
  timeTaken: string;
  date: string;
}
import {
  DeckDetailResponse,
  DeckRatingSummaryResponse,
  QuestionResponse
} from '../../models/deck.models';

@Component({
  selector: 'app-deck-detail',
  standalone: true,
  imports: [RouterModule, CommonModule, FormsModule],
  templateUrl: './deck-detail.html',
  styleUrl: './deck-detail.css',
})
export class DeckDetail implements OnInit {
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private deckService = inject(DeckService);
  private authService = inject(AuthService);
  private platformId = inject(PLATFORM_ID);

  // State
  deck = signal<DeckDetailResponse | null>(null);
  ratingSummary = signal<DeckRatingSummaryResponse | null>(null);
  loadingDeck = signal(true);
  loadingRatings = signal(true);
  deckError = signal<string | null>(null);

  // UI
  studyMode = signal<string>('quiz');
  showAllTerms = signal(false);
  filterText = signal('');

  // Quiz history
  quizHistory = signal<QuizAttemptSummary[]>([]);

  // Rating form
  myRating = signal(0);
  myComment = signal('');
  hoverRating = signal(0);
  submittingRating = signal(false);
  ratingSuccess = signal(false);
  ratingError = signal<string | null>(null);

  // Invite modal
  isInviteModalOpen = signal(false);
  inviteEmail = signal('');
  isInviting = signal(false);
  inviteSuccess = signal<string | null>(null);
  inviteError = signal<string | null>(null);

  isLoggedIn = computed(() => this.authService.isLoggedIn());

  openInviteModal() {
    this.isInviteModalOpen.set(true);
    this.inviteEmail.set('');
    this.inviteSuccess.set(null);
    this.inviteError.set(null);
  }

  closeInviteModal() {
    this.isInviteModalOpen.set(false);
  }

  sendInvite() {
    if (!this.inviteEmail().trim() || this.isInviting()) return;
    this.isInviting.set(true);
    this.inviteSuccess.set(null);
    this.inviteError.set(null);

    this.deckService.invite(this.deckId, this.inviteEmail().trim()).subscribe({
      next: (res) => {
        this.isInviting.set(false);
        this.inviteSuccess.set('Đã gửi lời mời thành công!');
        this.inviteEmail.set('');
        setTimeout(() => this.closeInviteModal(), 2000);
      },
      error: (err) => {
        this.isInviting.set(false);
        this.inviteError.set(err?.error?.message ?? 'Đã xảy ra lỗi khi gửi lời mời.');
      }
    });
  }

  studyModes = [
    { key: 'learn', label: 'Learn', icon: 'auto_stories', desc: 'Lộ trình học cá nhân hóa', gradient: 'linear-gradient(135deg, #f093fb, #f5576c)' },
    { key: 'quiz', label: 'Quiz', icon: 'quiz', desc: 'Kiểm tra kiến thức', gradient: 'linear-gradient(135deg, #4255FF, #6366f1)' },
  ];

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('id');
    if (!id) {
      this.deckError.set('Không tìm thấy bộ thẻ.');
      this.loadingDeck.set(false);
      return;
    }

    this.deckService.getDeckById(id).subscribe({
      next: (res) => {
        if (res.data?.status === 'Draft') {
          this.router.navigate(['/edit-deck', id]);
          return;
        }
        this.deck.set(res.data);
        this.loadingDeck.set(false);
      },
      error: (err) => {
        this.deckError.set(err?.error?.message ?? 'Không thể tải bộ thẻ này.');
        this.loadingDeck.set(false);
      }
    });

    this.deckService.getRatings(id).subscribe({
      next: (res) => {
        this.ratingSummary.set(res.data);
        this.loadingRatings.set(false);
      },
      error: () => this.loadingRatings.set(false)
    });

    this.loadQuizHistory(id);
  }

  private loadQuizHistory(deckId: string) {
    if (!isPlatformBrowser(this.platformId)) return;
    const key = `quiz_history_${deckId}`;
    const data: QuizAttemptSummary[] = JSON.parse(localStorage.getItem(key) ?? '[]');
    this.quizHistory.set(data.slice(0, 5)); // Show last 5
  }

  formatHistoryDate(dateStr: string): string {
    const d = new Date(dateStr);
    return d.toLocaleDateString('vi-VN', { day: '2-digit', month: '2-digit', year: 'numeric', hour: '2-digit', minute: '2-digit' });
  }

  getScoreColor(score: number): string {
    if (score >= 80) return '#16a34a';
    if (score >= 50) return '#d97706';
    return '#ef4444';
  }

  get deckId(): string {
    return this.deck()?.id ?? '';
  }

  get visibleTerms(): QuestionResponse[] {
    const all = this.deck()?.questions ?? [];
    const filtered = this.filterText()
      ? all.filter(q =>
          q.content.toLowerCase().includes(this.filterText().toLowerCase()) ||
          q.explanation?.toLowerCase().includes(this.filterText().toLowerCase())
        )
      : all;
    return this.showAllTerms() ? filtered : filtered.slice(0, 4);
  }

  get totalFilteredTerms(): number {
    const all = this.deck()?.questions ?? [];
    if (!this.filterText()) return all.length;
    return all.filter(q =>
      q.content.toLowerCase().includes(this.filterText().toLowerCase()) ||
      q.explanation?.toLowerCase().includes(this.filterText().toLowerCase())
    ).length;
  }

  get ratingBreakdown(): { stars: number; count: number }[] {
    const ratings = this.ratingSummary()?.ratings ?? [];
    return [5, 4, 3, 2, 1].map(stars => ({
      stars,
      count: ratings.filter(r => r.rating === stars).length
    }));
  }

  getBarWidth(count: number): string {
    const max = Math.max(...this.ratingBreakdown.map(r => r.count), 1);
    return (count / max * 100) + '%';
  }

  selectMode(key: string) { this.studyMode.set(key); }

  toggleTerms() { this.showAllTerms.update(v => !v); }

  onFilterInput(event: Event) {
    const input = event.target as HTMLInputElement;
    this.filterText.set(input.value);
  }

  startStudy() {
    const mode = this.studyMode();
    if (mode === 'quiz') this.router.navigate(['/quiz', this.deckId]);
    else if (mode === 'learn') this.router.navigate(['/learn', this.deckId]);
  }

  setMyRating(value: number) { this.myRating.set(value); }
  setHoverRating(value: number) { this.hoverRating.set(value); }
  clearHover() { this.hoverRating.set(0); }

  getStarState(star: number): 'filled' | 'empty' {
    const active = this.hoverRating() || this.myRating();
    return star <= active ? 'filled' : 'empty';
  }

  submitRating() {
    if (!this.myRating()) return;
    this.submittingRating.set(true);
    this.ratingError.set(null);
    this.deckService.submitRating(this.deckId, {
      rating: this.myRating(),
      comment: this.myComment() || undefined
    }).subscribe({
      next: () => {
        this.submittingRating.set(false);
        this.ratingSuccess.set(true);
        // Reload ratings
        this.deckService.getRatings(this.deckId).subscribe(res => {
          this.ratingSummary.set(res.data);
        });
        setTimeout(() => this.ratingSuccess.set(false), 3000);
      },
      error: (err) => {
        this.submittingRating.set(false);
        this.ratingError.set(err?.error?.message ?? 'Không thể gửi đánh giá.');
      }
    });
  }

  getStarArray(rating: number): string[] {
    const stars: string[] = [];
    for (let i = 1; i <= 5; i++) {
      if (i <= Math.floor(rating)) stars.push('full');
      else if (i - rating < 1) stars.push('half');
      else stars.push('empty');
    }
    return stars;
  }

  formatTimeAgo(dateStr: string): string {
    const diff = Date.now() - new Date(dateStr).getTime();
    const days = Math.floor(diff / 86400000);
    if (days === 0) return 'Hôm nay';
    if (days === 1) return '1 ngày trước';
    if (days < 7) return `${days} ngày trước`;
    if (days < 30) return `${Math.floor(days / 7)} tuần trước`;
    return `${Math.floor(days / 30)} tháng trước`;
  }

  getInitial(name: string): string {
    return name ? name.charAt(0).toUpperCase() : '?';
  }
}
