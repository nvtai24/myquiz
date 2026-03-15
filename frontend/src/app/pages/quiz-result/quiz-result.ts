import { Component, signal, OnInit, inject } from '@angular/core';
import { Router, ActivatedRoute, RouterModule } from '@angular/router';
import { CommonModule } from '@angular/common';

interface QuestionReview {
  number: number;
  text: string;
  answer: string | number | boolean | null;
  correctAnswer?: string;
  correct?: string;       // alias for correctAnswer (for template compat)
  isCorrect: boolean;
  explanation?: string;
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

  deckId = signal('');
  deckTitle = signal('');
  score = signal(0);
  totalCorrect = signal(0);
  totalIncorrect = signal(0);
  totalQuestions = signal(0);
  timeTaken = signal('00:00');
  hintsUsed = signal(0);

  categories: { name: string; score: number; color: string }[] = [];
  questions: QuestionReview[] = [];

  expandedQuestion = signal<number | null>(null);

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
        answer: q.answer,
        correctAnswer: q.correctAnswer,
        isCorrect: q.isCorrect,
      }));
    } else {
      // Fallback: read deckId from route
      this.route.params.subscribe(p => this.deckId.set(p['id'] ?? ''));
    }
  }

  get greeting(): string {
    const s = this.score();
    if (s >= 90) return 'Xuất sắc!';
    if (s >= 70) return 'Làm tốt lắm!';
    if (s >= 50) return 'Tiếp tục cố gắng!';
    return 'Hãy ôn luyện thêm!';
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
}
