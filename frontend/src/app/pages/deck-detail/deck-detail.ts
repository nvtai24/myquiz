import { Component, OnInit, inject, signal, computed, PLATFORM_ID } from '@angular/core';
import { isPlatformBrowser } from '@angular/common';
import { ActivatedRoute, Router, RouterModule } from '@angular/router';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { DeckService } from '../../services/deck.service';
import { AuthService } from '../../services/auth.service';
import { PaymentService } from '../../services/payment.service';

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
  DeckMemberResponse,
  DeckRatingSummaryResponse,
  QuestionResponse,
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
  private paymentService = inject(PaymentService);
  private platformId = inject(PLATFORM_ID);

  // State
  deck = signal<DeckDetailResponse | null>(null);
  ratingSummary = signal<DeckRatingSummaryResponse | null>(null);
  loadingDeck = signal(true);
  loadingRatings = signal(true);
  deckError = signal<string | null>(null);

  // Export PDF
  hasExportToPdf = signal(false);
  exportingPdf = signal(false);

  // Save deck
  isSaved = signal(false);
  savingDeck = signal(false);

  // UI
  studyMode = signal<string>('quiz');
  showAllTerms = signal(false);
  filterText = signal('');

  // Flashcard
  displayQuestions = signal<QuestionResponse[]>([]);
  isShuffled = signal(false);
  currentCardIndex = signal(0);
  isFlipped = signal(false);
  showHint = signal(false);
  showExplanation = signal(false);

  // Bottom List Toggles
  expandedHints = signal<Set<number>>(new Set());
  expandedExplanations = signal<Set<number>>(new Set());

  toggleListHint(id: number) {
    this.expandedHints.update((prev) => {
      const next = new Set(prev);
      if (next.has(id)) next.delete(id);
      else next.add(id);
      return next;
    });
  }

  toggleListExplanation(id: number) {
    this.expandedExplanations.update((prev) => {
      const next = new Set(prev);
      if (next.has(id)) next.delete(id);
      else next.add(id);
      return next;
    });
  }

  currentQuestion = computed(() => {
    const questions = this.displayQuestions();
    if (!questions?.length) return null;
    return questions[this.currentCardIndex()] ?? null;
  });

  flipCard() {
    this.isFlipped.update((v) => !v);
  }

  nextCard() {
    const total = this.displayQuestions().length;
    if (this.currentCardIndex() < total - 1) {
      this.currentCardIndex.update((i) => i + 1);
      this.resetCard();
    }
  }

  prevCard() {
    if (this.currentCardIndex() > 0) {
      this.currentCardIndex.update((i) => i - 1);
      this.resetCard();
    }
  }

  resetCard() {
    this.isFlipped.set(false);
    this.showHint.set(false);
    this.showExplanation.set(false);
  }

  shuffleCards() {
    const questions = this.deck()?.questions ?? [];
    if (!this.isShuffled()) {
      const shuffled = [...questions];
      for (let i = shuffled.length - 1; i > 0; i--) {
        const j = Math.floor(Math.random() * (i + 1));
        [shuffled[i], shuffled[j]] = [shuffled[j], shuffled[i]];
      }
      this.displayQuestions.set(shuffled);
      this.isShuffled.set(true);
    } else {
      this.displayQuestions.set([...questions]);
      this.isShuffled.set(false);
    }
    this.currentCardIndex.set(0);
    this.resetCard();
  }

  restartCards() {
    this.currentCardIndex.set(0);
    this.resetCard();
  }

  // Quiz history
  quizHistory = signal<QuizAttemptSummary[]>([]);

  // Rating check
  hasRated = signal(false);
  existingRatingValue = signal<number | null>(null);
  existingRatingComment = signal<string | null>(null);

  // Rating form
  myRating = signal(5);
  myComment = signal('');
  hoverRating = signal(0);
  submittingRating = signal(false);
  ratingSuccess = signal(false);
  ratingError = signal<string | null>(null);

  // Quiz config modal
  isQuizConfigOpen = signal(false);
  quizConfig = signal({
    questionCount: 10,
    types: { MultipleChoice: true, TrueFalse: true, FillInTheBlank: true },
    shuffle: true,
    showTimer: false,
    enableHints: true,
  });

  availableTypes = computed(() => {
    const questions = this.deck()?.questions ?? [];
    return {
      MultipleChoice: questions.some((q) => q.type === 'MultipleChoice'),
      TrueFalse: questions.some((q) => q.type === 'TrueFalse'),
      FillInTheBlank: questions.some((q) => q.type === 'FillInTheBlank'),
    };
  });

  typeCounts = computed(() => {
    const questions = this.deck()?.questions ?? [];
    return {
      MultipleChoice: questions.filter((q) => q.type === 'MultipleChoice').length,
      TrueFalse: questions.filter((q) => q.type === 'TrueFalse').length,
      FillInTheBlank: questions.filter((q) => q.type === 'FillInTheBlank').length,
    };
  });

  openQuizConfig() {
    const total = this.deck()?.questionCount ?? 10;
    const available = this.availableTypes();
    this.quizConfig.update((c) => ({
      ...c,
      questionCount: Math.min(c.questionCount, total),
      types: {
        MultipleChoice: available.MultipleChoice,
        TrueFalse: available.TrueFalse,
        FillInTheBlank: available.FillInTheBlank,
      },
    }));
    this.isQuizConfigOpen.set(true);
  }

  closeQuizConfig() {
    this.isQuizConfigOpen.set(false);
  }

  setQuizConfigQuestionCount(val: number) {
    const max = this.deck()?.questionCount ?? 10;
    this.quizConfig.update((c) => ({ ...c, questionCount: Math.max(1, Math.min(val, max)) }));
  }

  toggleQuizConfigType(type: 'MultipleChoice' | 'TrueFalse' | 'FillInTheBlank') {
    if (!this.availableTypes()[type]) return;
    this.quizConfig.update((c) => ({
      ...c,
      types: { ...c.types, [type]: !c.types[type] },
    }));
  }

  toggleQuizConfigOption(key: 'shuffle' | 'showTimer' | 'enableHints') {
    this.quizConfig.update((c) => ({ ...c, [key]: !c[key] }));
  }

  get quizConfigValid(): boolean {
    const t = this.quizConfig().types;
    const a = this.availableTypes();
    return (
      (t.MultipleChoice && a.MultipleChoice) ||
      (t.TrueFalse && a.TrueFalse) ||
      (t.FillInTheBlank && a.FillInTheBlank)
    );
  }

  isTypeEnabled(key: string): boolean {
    const types = this.quizConfig().types;
    return types[key as keyof typeof types];
  }

  isTypeAvailable(key: string): boolean {
    return this.availableTypes()[key as keyof ReturnType<typeof this.availableTypes>];
  }

  getTypeCount(key: string): number {
    return this.typeCounts()[key as keyof ReturnType<typeof this.typeCounts>];
  }

  isOptionEnabled(key: 'shuffle' | 'showTimer' | 'enableHints'): boolean {
    return this.quizConfig()[key];
  }

  // Rating modal
  isRatingModalOpen = signal(false);

  openRatingModal() {
    this.isRatingModalOpen.set(true);
    this.myRating.set(5);
    this.myComment.set('');
    this.ratingSuccess.set(false);
    this.ratingError.set(null);
  }

  closeRatingModal() {
    this.isRatingModalOpen.set(false);
  }

  // Invite modal
  isInviteModalOpen = signal(false);
  inviteEmail = signal('');
  isInviting = signal(false);
  inviteSuccess = signal<string | null>(null);
  inviteError = signal<string | null>(null);
  deckMembers = signal<DeckMemberResponse[]>([]);
  loadingMembers = signal(false);

  isLoggedIn = computed(() => this.authService.isLoggedIn());
  isOwner = computed(() => {
    const user = this.authService.currentUser();
    return !!user && user.email === this.deck()?.ownerEmail;
  });

  openInviteModal() {
    this.isInviteModalOpen.set(true);
    this.inviteEmail.set('');
    this.inviteSuccess.set(null);
    this.inviteError.set(null);
    this.loadingMembers.set(true);
    this.deckService.getMembers(this.deckId).subscribe({
      next: (res) => {
        this.deckMembers.set(res.data ?? []);
        this.loadingMembers.set(false);
      },
      error: () => this.loadingMembers.set(false),
    });
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
      next: () => {
        this.isInviting.set(false);
        this.inviteSuccess.set('Invitation sent successfully!');
        this.inviteEmail.set('');
        this.deckService.getMembers(this.deckId).subscribe({
          next: (res) => this.deckMembers.set(res.data ?? []),
          error: () => {},
        });
        setTimeout(() => this.closeInviteModal(), 2000);
      },
      error: (err) => {
        this.isInviting.set(false);
        this.inviteError.set(err?.error?.message ?? 'Failed to send invitation.');
      },
    });
  }

  studyModes = [
    {
      key: 'learn',
      label: 'Learn',
      icon: 'auto_stories',
      desc: 'Lộ trình học cá nhân hóa',
      gradient: 'linear-gradient(135deg, #f093fb, #f5576c)',
    },
    {
      key: 'quiz',
      label: 'Quiz',
      icon: 'quiz',
      desc: 'Kiểm tra kiến thức',
      gradient: 'linear-gradient(135deg, #4255FF, #6366f1)',
    },
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
        this.deck.set(res.data ?? null);
        this.displayQuestions.set(res.data?.questions ?? []);
        this.isSaved.set(res.data?.isSaved ?? false);
        this.loadingDeck.set(false);
      },
      error: (err) => {
        this.deckError.set(err?.error?.message ?? 'Không thể tải bộ thẻ này.');
        this.loadingDeck.set(false);
      },
    });

    this.deckService.getRatings(id).subscribe({
      next: (res) => {
        const summary = res.data ?? null;
        this.ratingSummary.set(summary);
        this.syncDeckRatingStats(summary);
        this.loadingRatings.set(false);
      },
      error: () => this.loadingRatings.set(false),
    });

    if (this.authService.isLoggedIn()) {
      this.deckService.checkRating(id).subscribe({
        next: (res) => {
          const data = res.data;
          if (data?.hasRated) {
            this.hasRated.set(true);
            this.existingRatingValue.set(data.rating);
            this.existingRatingComment.set(data.comment);
          }
        },
        error: () => {},
      });

      this.paymentService.getMySubscription().subscribe({
        next: (sub) => {
          this.hasExportToPdf.set(sub?.hasExportToPdf ?? false);
        },
        error: () => {},
      });
    }

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
    return d.toLocaleDateString('vi-VN', {
      day: '2-digit',
      month: '2-digit',
      year: 'numeric',
      hour: '2-digit',
      minute: '2-digit',
    });
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
      ? all.filter(
          (q) =>
            q.content.toLowerCase().includes(this.filterText().toLowerCase()) ||
            q.explanation?.toLowerCase().includes(this.filterText().toLowerCase()),
        )
      : all;
    return this.showAllTerms() ? filtered : filtered.slice(0, 4);
  }

  get totalFilteredTerms(): number {
    const all = this.deck()?.questions ?? [];
    if (!this.filterText()) return all.length;
    return all.filter(
      (q) =>
        q.content.toLowerCase().includes(this.filterText().toLowerCase()) ||
        q.explanation?.toLowerCase().includes(this.filterText().toLowerCase()),
    ).length;
  }

  get ratingBreakdown(): { stars: number; count: number }[] {
    const ratings = this.ratingSummary()?.ratings ?? [];
    return [5, 4, 3, 2, 1].map((stars) => ({
      stars,
      count: ratings.filter((r) => r.rating === stars).length,
    }));
  }

  getBarWidth(count: number): string {
    const max = Math.max(...this.ratingBreakdown.map((r) => r.count), 1);
    return (count / max) * 100 + '%';
  }

  selectMode(key: string) {
    this.studyMode.set(key);
  }

  toggleTerms() {
    this.showAllTerms.update((v) => !v);
  }

  onFilterInput(event: Event) {
    const input = event.target as HTMLInputElement;
    this.filterText.set(input.value);
  }

  goToLearn() {
    this.router.navigate(['/learn', this.deckId]);
  }

  startStudy() {
    const mode = this.studyMode();
    if (mode === 'quiz') {
      const cfg = this.quizConfig();
      const types = [
        cfg.types.MultipleChoice ? 'mc' : null,
        cfg.types.TrueFalse ? 'tf' : null,
        cfg.types.FillInTheBlank ? 'fb' : null,
      ]
        .filter(Boolean)
        .join(',');
      this.router.navigate(['/quiz', this.deckId], {
        queryParams: {
          count: cfg.questionCount,
          types,
          shuffle: cfg.shuffle,
          timer: cfg.showTimer,
          hints: cfg.enableHints,
        },
      });
    } else if (mode === 'learn') {
      this.router.navigate(['/learn', this.deckId]);
    }
  }

  setMyRating(value: number) {
    this.myRating.set(value);
  }
  setHoverRating(value: number) {
    this.hoverRating.set(value);
  }
  clearHover() {
    this.hoverRating.set(0);
  }

  getStarState(star: number): 'filled' | 'empty' {
    const active = this.hoverRating() || this.myRating();
    return star <= active ? 'filled' : 'empty';
  }

  private syncDeckRatingStats(summary: DeckRatingSummaryResponse | null) {
    if (!summary) return;
    this.deck.update((current) =>
      current
        ? {
            ...current,
            averageRating: summary.averageRating,
            totalRatings: summary.totalRatings,
          }
        : current,
    );
  }

  private refreshRatings(deckId: string) {
    this.deckService.getRatings(deckId).subscribe({
      next: (res) => {
        const summary = res.data ?? null;
        this.ratingSummary.set(summary);
        this.syncDeckRatingStats(summary);
      },
      error: () => {},
    });
  }

  submitRating() {
    if (!this.myRating()) return;
    const submittedRating = this.myRating();
    const submittedComment = this.myComment().trim();
    this.submittingRating.set(true);
    this.ratingError.set(null);
    this.deckService
      .submitRating(this.deckId, {
        rating: submittedRating,
        comment: submittedComment || undefined,
      })
      .subscribe({
        next: () => {
          this.submittingRating.set(false);
          this.ratingSuccess.set(true);
          this.hasRated.set(true);
          this.existingRatingValue.set(submittedRating);
          this.existingRatingComment.set(submittedComment || null);
          this.refreshRatings(this.deckId);
          setTimeout(() => {
            this.ratingSuccess.set(false);
            this.closeRatingModal();
          }, 1500);
        },
        error: (err) => {
          this.submittingRating.set(false);
          this.ratingError.set(err?.error?.message ?? 'Không thể gửi đánh giá.');
        },
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

  exportPdf() {
    if (this.exportingPdf()) return;
    this.exportingPdf.set(true);
    this.deckService.exportPdf(this.deckId).subscribe({
      next: (response: any) => {
        const blob = response.body;
        const contentDisposition = response.headers.get('content-disposition');
        let filename = `${this.deck()?.name ?? 'deck'}.pdf`;

        if (contentDisposition) {
          const fileNameStarMatch = /filename\*=UTF-8''([^;]+)/i.exec(contentDisposition);
          if (fileNameStarMatch && fileNameStarMatch[1]) {
            filename = decodeURIComponent(fileNameStarMatch[1]);
          } else {
            const fileNameMatch = /filename="?([^;"]+)"?/i.exec(contentDisposition);
            if (fileNameMatch && fileNameMatch[1]) {
              filename = fileNameMatch[1].trim();
            }
          }
        }

        const url = window.URL.createObjectURL(blob);
        const a = document.createElement('a');
        a.href = url;
        a.download = filename;
        a.click();
        window.URL.revokeObjectURL(url);
        this.exportingPdf.set(false);
      },
      error: () => {
        this.exportingPdf.set(false);
      },
    });
  }

  toggleSave() {
    if (this.savingDeck() || !this.deckId) return;
    this.savingDeck.set(true);

    if (this.isSaved()) {
      this.deckService.unsaveDeck(this.deckId).subscribe({
        next: () => {
          this.isSaved.set(false);
          this.savingDeck.set(false);
        },
        error: () => {
          this.savingDeck.set(false);
        },
      });
    } else {
      this.deckService.saveDeck(this.deckId).subscribe({
        next: () => {
          this.isSaved.set(true);
          this.savingDeck.set(false);
        },
        error: () => {
          this.savingDeck.set(false);
        },
      });
    }
  }
}
