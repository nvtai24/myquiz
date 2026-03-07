import { Component, signal } from '@angular/core';
import { RouterModule } from '@angular/router';

interface QuestionReview {
  number: number;
  text: string;
  answer: string;
  correct?: string;
  isCorrect: boolean;
  explanation?: string;
}

@Component({
  selector: 'app-quiz-result',
  standalone: true,
  imports: [RouterModule],
  templateUrl: './quiz-result.html',
  styleUrl: './quiz-result.css',
})
export class QuizResult {
  score = 85;
  totalCorrect = 17;
  totalIncorrect = 3;
  totalQuestions = 20;
  timeTaken = '12:34';
  hintsUsed = 2;

  categories = [
    { name: 'Logical Reasoning', score: 100, color: '#16a34a' },
    { name: 'Mathematical Aptitude', score: 75, color: '#4255FF' },
    { name: 'Historical Context', score: 80, color: '#7c3aed' },
  ];

  questions: QuestionReview[] = [
    { number: 1, text: 'Which planet is known as the Red Planet?', answer: 'Mars', isCorrect: true },
    { number: 2, text: 'What is the square root of 144?', answer: '14', correct: '12', isCorrect: false, explanation: '12 multiplied by itself (12 × 12) equals 144. It\'s a common perfect square in mathematics.' },
    { number: 3, text: 'Who painted the Mona Lisa?', answer: 'Leonardo da Vinci', isCorrect: true },
  ];

  expandedQuestion = signal<number | null>(null);

  get greeting(): string {
    if (this.score >= 90) return 'Outstanding!';
    if (this.score >= 70) return 'Great Job, Alex!';
    if (this.score >= 50) return 'Good effort!';
    return 'Keep practicing!';
  }

  get performanceText(): string {
    return `You've outperformed 78% of other students.`;
  }

  toggleQuestion(num: number) {
    this.expandedQuestion.update(v => v === num ? null : num);
  }

  expandAll() {
    // simple toggle
    this.expandedQuestion.set(this.expandedQuestion() === -1 ? null : -1);
  }
}
