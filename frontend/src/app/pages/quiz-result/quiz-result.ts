import { Component, signal, OnInit, inject, PLATFORM_ID } from '@angular/core';
import { isPlatformBrowser } from '@angular/common';
import { Router, ActivatedRoute, RouterModule } from '@angular/router';
import { CommonModule } from '@angular/common';

interface QuestionReview {
  number: number;
  text: string;
  type: string;
  userAnswer: string;
  correctAnswer: string;
  isCorrect: boolean;
  explanation?: string;
  options?: string[];
  correctOptionIndex?: number;
  userSelectedIndex?: number | null;
}

interface QuizAttempt {
  id: string;
  deckId: string;
  deckTitle: string;
  score: number;
  totalCorrect: number;
  totalQuestions: number;
  timeTaken: string;
  date: string;
  questions: QuestionReview[];
}

@Component({
  selector: 'app-quiz-result',
  standalone: true,
  imports: [RouterModule, CommonModule],
  templateUrl: './quiz-result.html',
  styleUrl: './quiz-result.css',
})
export class QuizResult implements OnInit {
  private router = inject(Router);
  private route = inject(ActivatedRoute);
  private platformId = inject(PLATFORM_ID);

  deckId = signal('');
  deckTitle = signal('');
  score = signal(0);
  totalCorrect = signal(0);
  totalIncorrect = signal(0);
  totalQuestions = signal(0);
  timeTaken = signal('00:00');

  questions: QuestionReview[] = [];
  expandedQuestion = signal<number | null>(null);

  // History
  pastAttempts = signal<QuizAttempt[]>([]);
  showHistory = signal(false);

  ngOnInit(): void {
    const nav = this.router.getCurrentNavigation();
    const state = nav?.extras?.state ?? history.state;

    if (state && state['totalQuestions']) {
      const correct = state['correctAnswers'] ?? 0;
      const total = state['totalQuestions'] ?? 0;
      const incorrect = total - correct;

      this.deckId.set(state['deckId'] ?? '');
      this.deckTitle.set(state['deckTitle'] ?? 'Quiz');
      this.totalQuestions.set(total);
      this.totalCorrect.set(correct);
      this.totalIncorrect.set(incorrect);
      this.score.set(total > 0 ? Math.round((correct / total) * 100) : 0);
      this.timeTaken.set(state['timeTaken'] ?? '00:00');
      this.questions = (state['questions'] ?? []).map((q: any) => ({
        number: q.number,
        text: q.text,
        type: q.type,
        userAnswer: q.userAnswer ?? q.answer ?? '--',
        correctAnswer: q.correctAnswer ?? '--',
        isCorrect: q.isCorrect,
        explanation: q.explanation,
        options: q.options ?? [],
        correctOptionIndex: q.correctOptionIndex ?? null,
        userSelectedIndex: q.userSelectedIndex ?? null,
      }));

      // Save this attempt to history
      this.saveAttempt();
    } else {
      this.route.params.subscribe(p => this.deckId.set(p['id'] ?? ''));
    }

    // Load history
    this.loadHistory();
  }

  get greeting(): string {
    const s = this.score();
    if (s >= 90) return 'Xuất sắc! 🏆';
    if (s >= 70) return 'Làm tốt lắm! 🎉';
    if (s >= 50) return 'Tiếp tục cố gắng! 💪';
    return 'Hãy ôn luyện thêm! 📚';
  }

  get performanceText(): string {
    return `${this.totalCorrect()} / ${this.totalQuestions()} câu đúng`;
  }

  get starsCount(): number {
    const s = this.score();
    if (s >= 90) return 5;
    if (s >= 75) return 4;
    if (s >= 60) return 3;
    if (s >= 40) return 2;
    return 1;
  }

  toggleQuestion(num: number) {
    this.expandedQuestion.update(v => v === num ? null : num);
  }

  expandAll() {
    this.expandedQuestion.set(this.expandedQuestion() === -1 ? null : -1);
  }

  goToQuiz() {
    this.router.navigate(['/quiz', this.deckId()]);
  }

  goToLearn() {
    this.router.navigate(['/learn', this.deckId()]);
  }

  isExpanded(num: number): boolean {
    return this.expandedQuestion() === num || this.expandedQuestion() === -1;
  }

  getOptionLabel(index: number): string {
    return String.fromCharCode(65 + index); // A, B, C, D...
  }

  // ── History Management ──

  toggleHistory() {
    this.showHistory.update(v => !v);
  }

  private saveAttempt() {
    if (!isPlatformBrowser(this.platformId)) return;
    if (!this.deckId() || this.totalQuestions() === 0) return;

    const attempt: QuizAttempt = {
      id: Date.now().toString(36) + Math.random().toString(36).substring(2, 5),
      deckId: this.deckId(),
      deckTitle: this.deckTitle(),
      score: this.score(),
      totalCorrect: this.totalCorrect(),
      totalQuestions: this.totalQuestions(),
      timeTaken: this.timeTaken(),
      date: new Date().toISOString(),
      questions: this.questions,
    };

    const key = `quiz_history_${this.deckId()}`;
    const existing: QuizAttempt[] = JSON.parse(localStorage.getItem(key) ?? '[]');
    existing.unshift(attempt); // newest first
    // Keep max 20 attempts per deck
    const trimmed = existing.slice(0, 20);
    localStorage.setItem(key, JSON.stringify(trimmed));
  }

  private loadHistory() {
    if (!isPlatformBrowser(this.platformId)) return;
    if (!this.deckId()) return;
    const key = `quiz_history_${this.deckId()}`;
    const data: QuizAttempt[] = JSON.parse(localStorage.getItem(key) ?? '[]');
    this.pastAttempts.set(data);
  }

  formatDate(dateStr: string): string {
    const d = new Date(dateStr);
    return d.toLocaleDateString('vi-VN', { day: '2-digit', month: '2-digit', year: 'numeric', hour: '2-digit', minute: '2-digit' });
  }

  getScoreColor(score: number): string {
    if (score >= 80) return '#16a34a';
    if (score >= 50) return '#d97706';
    return '#ef4444';
  }
}
