import { Component, signal, OnInit, inject, computed } from '@angular/core';
import { Router, ActivatedRoute, RouterModule } from '@angular/router';
import { CommonModule } from '@angular/common';
import { QuizAttemptResponse, UserAnswerResponse } from '../../models/quiz.models';

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

  deckId = signal('');
  deckTitle = signal('');
  attempt = signal<QuizAttemptResponse | null>(null);
  expandedIndex = signal<number | null>(null);

  score = computed(() => this.attempt()?.score ?? 0);
  totalCorrect = computed(() => this.attempt()?.correctAnswers ?? 0);
  totalQuestions = computed(() => this.attempt()?.totalQuestions ?? 0);
  totalIncorrect = computed(() => this.totalQuestions() - this.totalCorrect());

  get formattedTime(): string {
    const ms = this.attempt()?.totalTime ?? 0;
    const mins = Math.floor(ms / 60000);
    const secs = Math.floor((ms % 60000) / 1000);
    return `${mins.toString().padStart(2, '0')}:${secs.toString().padStart(2, '0')}`;
  }

  get greeting(): string {
    const s = this.score();
    if (s >= 90) return 'Outstanding!';
    if (s >= 70) return 'Great job!';
    if (s >= 50) return 'Keep it up!';
    return 'Keep practicing!';
  }

  get starsCount(): number {
    const s = this.score();
    if (s >= 90) return 5;
    if (s >= 75) return 4;
    if (s >= 60) return 3;
    if (s >= 40) return 2;
    return 1;
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
    const nav = this.router.getCurrentNavigation();
    const state = nav?.extras?.state ?? history.state;

    this.route.params.subscribe(p => this.deckId.set(p['id'] ?? ''));

    if (state?.['attempt']) {
      this.attempt.set(state['attempt'] as QuizAttemptResponse);
      this.deckId.set(state['attempt'].deckId);
    }
    if (state?.['deckTitle']) {
      this.deckTitle.set(state['deckTitle']);
    }
  }

  toggleQuestion(index: number) {
    this.expandedIndex.update(v => v === index ? null : index);
  }

  isExpanded(index: number): boolean {
    return this.expandedIndex() === index || this.expandedIndex() === -1;
  }

  expandAll() {
    this.expandedIndex.update(v => v === -1 ? null : -1);
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

  hasOptions(ans: UserAnswerResponse): boolean {
    return ans.options.length > 0;
  }

  retryQuiz() {
    this.router.navigate(['/deck', this.deckId()]);
  }

  goBack() {
    this.router.navigate(['/deck', this.deckId()]);
  }
}
