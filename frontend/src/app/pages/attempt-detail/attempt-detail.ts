import { Component, signal, OnInit, inject, computed } from '@angular/core';
import { Router, ActivatedRoute, RouterModule } from '@angular/router';
import { CommonModule } from '@angular/common';
import { QuizAttemptResponse, UserAnswerResponse } from '../../models/quiz.models';
import { QuizService } from '../../services/quiz.service';

@Component({
  selector: 'app-attempt-detail',
  standalone: true,
  imports: [RouterModule, CommonModule],
  templateUrl: './attempt-detail.html',
  styleUrl: './attempt-detail.css',
})
export class AttemptDetail implements OnInit {
  private router = inject(Router);
  private route = inject(ActivatedRoute);
  private quizService = inject(QuizService);

  attempt = signal<QuizAttemptResponse | null>(null);
  loading = signal(true);
  error = signal<string | null>(null);

  score = computed(() => this.attempt()?.score ?? 0);
  totalCorrect = computed(() => this.attempt()?.correctAnswers ?? 0);
  totalQuestions = computed(() => this.attempt()?.totalQuestions ?? 0);
  totalIncorrect = computed(() => this.totalQuestions() - this.totalCorrect());

  get formattedTime(): string {
    const ms = this.attempt()?.totalTime ?? 0;
    const mins = Math.floor(ms / 60000);
    const secs = Math.floor((ms % 60000) / 1000);
    return `${mins}m ${secs}s`;
  }

  get scoreColor(): string {
    const s = this.score();
    if (s >= 70) return '#16a34a';
    if (s >= 40) return '#d97706';
    return '#ef4444';
  }

  get ringOffset(): number {
    return 326.7 - (326.7 * this.score() / 100);
  }

  ngOnInit(): void {
    const attemptId = this.route.snapshot.params['id'];
    if (attemptId) {
      this.loadAttempt(attemptId);
    }
  }

  private loadAttempt(attemptId: string): void {
    this.loading.set(true);
    this.error.set(null);

    this.quizService.getAttemptById(attemptId).subscribe({
      next: (res) => {
        this.loading.set(false);
        if (res.success && res.data) {
          this.attempt.set(res.data);
        } else {
          this.error.set('Failed to load attempt details');
        }
      },
      error: () => {
        this.loading.set(false);
        this.error.set('Failed to load attempt details');
      }
    });
  }

  formatAttemptDate(): string {
    const dateStr = this.attempt()?.startedAt;
    if (!dateStr) return '';
    const d = new Date(dateStr);
    return d.toLocaleDateString('vi-VN', {
      day: '2-digit',
      month: '2-digit',
      year: 'numeric',
      hour: '2-digit',
      minute: '2-digit',
    });
  }

  getTypeLabel(type: string): string {
    switch (type) {
      case 'MultipleChoice': return 'Multiple Choice';
      case 'TrueFalse': return 'True / False';
      case 'FillInTheBlank': return 'Fill in the Blank';
      default: return type;
    }
  }

  hasOptions(ans: UserAnswerResponse): boolean {
    return ans.options && ans.options.length > 0;
  }

  isOptionUserAnswer(ans: UserAnswerResponse, opt: string): boolean {
    return ans.answer.includes(opt);
  }

  isOptionCorrect(ans: UserAnswerResponse, opt: string): boolean {
    return ans.correctAnswers.includes(opt);
  }

  getOptionClass(ans: UserAnswerResponse, opt: string): string {
    const isCorrect = this.isOptionCorrect(ans, opt);
    const isUser = this.isOptionUserAnswer(ans, opt);
    if (isCorrect && isUser) return 'border-green-500 bg-green-50 text-green-800';
    if (isCorrect) return 'border-green-400 bg-green-50/60 text-green-700';
    if (isUser) return 'border-red-400 bg-red-50 text-red-700';
    return 'border-gray-100 bg-gray-50/50 text-gray-500';
  }

  getOptionLabel(i: number): string {
    return String.fromCharCode(65 + i);
  }

  goToHistory() {
    this.router.navigate(['/history']);
  }

  retryQuiz() {
    const deckId = this.attempt()?.deckId;
    if (deckId) {
      this.router.navigate(['/deck', deckId]);
    }
  }
}
