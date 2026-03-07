import { Component, signal, computed } from '@angular/core';
import { DecimalPipe } from '@angular/common';
import { Router, ActivatedRoute } from '@angular/router';
import { FormsModule } from '@angular/forms';

type QuestionType = 'multiple_choice' | 'true_false' | 'fill_blank';

interface BaseQuestion {
  id: number;
  text: string;
  type: QuestionType;
  hint?: string;
}

interface MultipleChoiceQuestion extends BaseQuestion {
  type: 'multiple_choice';
  options: { label: string; text: string }[];
  correctIndex: number;
}

interface TrueFalseQuestion extends BaseQuestion {
  type: 'true_false';
  correctAnswer: boolean;
}

interface FillBlankQuestion extends BaseQuestion {
  type: 'fill_blank';
  correctAnswer: string;
  placeholder?: string;
}

type Question = MultipleChoiceQuestion | TrueFalseQuestion | FillBlankQuestion;

type QuizState = 'setup' | 'in-progress';

@Component({
  selector: 'app-quiz',
  standalone: true,
  imports: [DecimalPipe, FormsModule],
  templateUrl: './quiz.html',
  styleUrl: './quiz.css',
})
export class Quiz {
  deckTitle = 'Mathematics Basics';
  deckId = '';
  quizState = signal<QuizState>('setup');
  currentIndex = signal(0);
  selectedAnswers = signal<(number | string | boolean | null)[]>([]);
  showHint = signal(false);
  flaggedQuestions = signal<Set<number>>(new Set());
  timerSeconds = signal(0);
  timerInterval: any = null;

  // Setup options
  questionCount = signal(10);
  shuffleQuestions = signal(true);
  showTimer = signal(true);
  showHintsOption = signal(true);

  // Question type filter
  selectedQuestionTypes = signal<Set<QuestionType>>(new Set(['multiple_choice', 'true_false', 'fill_blank']));

  allQuestions: Question[] = [
    // Multiple Choice
    { id: 1, type: 'multiple_choice', text: 'What is the Pythagorean theorem formula?', options: [{ label: 'A', text: 'a² + b² = c²' }, { label: 'B', text: 'a + b = c' }, { label: 'C', text: 'a³ + b³ = c³' }, { label: 'D', text: '2a + 2b = 2c' }], correctIndex: 0, hint: 'Think about the relationship between sides of a right triangle.' },
    { id: 2, type: 'multiple_choice', text: 'Which number is a prime number?', options: [{ label: 'A', text: '4' }, { label: 'B', text: '9' }, { label: 'C', text: '7' }, { label: 'D', text: '15' }], correctIndex: 2, hint: 'A prime number is only divisible by 1 and itself.' },
    { id: 3, type: 'multiple_choice', text: 'What is the approximate value of the Golden Ratio (φ)?', options: [{ label: 'A', text: '3.14159' }, { label: 'B', text: '2.71828' }, { label: 'C', text: '1.61803' }, { label: 'D', text: '1.41421' }], correctIndex: 2, hint: 'It is related to the Fibonacci sequence.' },
    { id: 4, type: 'multiple_choice', text: 'What is the derivative of x²?', options: [{ label: 'A', text: '2x' }, { label: 'B', text: 'x²' }, { label: 'C', text: '2' }, { label: 'D', text: 'x' }], correctIndex: 0, hint: 'Apply the power rule: d/dx(xⁿ) = nxⁿ⁻¹' },
    { id: 5, type: 'multiple_choice', text: 'What is ∫2x dx?', options: [{ label: 'A', text: 'x²+C' }, { label: 'B', text: '2x²' }, { label: 'C', text: 'x' }, { label: 'D', text: '2' }], correctIndex: 0, hint: 'Reverse the power rule for derivatives.' },

    // True/False
    { id: 6, type: 'true_false', text: 'The square root of 2 is a rational number.', correctAnswer: false, hint: '√2 cannot be expressed as a fraction of two integers.' },
    { id: 7, type: 'true_false', text: 'The derivative of a constant is zero.', correctAnswer: true, hint: 'Constants do not change, so their rate of change is 0.' },
    { id: 8, type: 'true_false', text: 'Pi (π) is exactly equal to 22/7.', correctAnswer: false, hint: '22/7 is just an approximation of π.' },
    { id: 9, type: 'true_false', text: 'Every even number greater than 2 can be expressed as the sum of two primes.', correctAnswer: true, hint: 'This is known as Goldbach\'s conjecture (unproven but verified for very large numbers).' },
    { id: 10, type: 'true_false', text: 'The limit of sin(x)/x as x→0 is 0.', correctAnswer: false, hint: 'This is a fundamental limit in calculus.' },

    // Fill in the Blank
    { id: 11, type: 'fill_blank', text: 'The derivative of eˣ is _____.', correctAnswer: 'eˣ', placeholder: 'Type your answer...', hint: 'eˣ is special — its derivative is itself.' },
    { id: 12, type: 'fill_blank', text: 'The value of sin(90°) is _____.', correctAnswer: '1', placeholder: 'Type a number...', hint: 'Think about the unit circle at 90 degrees.' },
    { id: 13, type: 'fill_blank', text: '∫cos(x) dx = _____ + C', correctAnswer: 'sin(x)', placeholder: 'Type your answer...', hint: 'Which function has cos(x) as its derivative?' },
    { id: 14, type: 'fill_blank', text: 'The 8th Fibonacci number is _____.', correctAnswer: '21', placeholder: 'Type a number...', hint: 'Sequence: 0, 1, 1, 2, 3, 5, 8, 13, ...' },
    { id: 15, type: 'fill_blank', text: 'd/dx(ln x) = _____.', correctAnswer: '1/x', placeholder: 'Type your answer...', hint: 'The natural log is the inverse of the exponential function.' },
  ];

  questions: Question[] = [];
  fillBlankInput = signal('');

  constructor(private router: Router, private route: ActivatedRoute) {
    this.route.params.subscribe(params => {
      this.deckId = params['id'] || '';
    });
  }

  get totalQuestions() { return this.questions.length; }

  currentQuestion = computed(() => this.questions[this.currentIndex()]);

  progress = computed(() => {
    if (this.totalQuestions === 0) return 0;
    const answered = this.selectedAnswers().filter(a => a !== null).length;
    return Math.round((answered / this.totalQuestions) * 100);
  });

  answeredCount = computed(() => this.selectedAnswers().filter(a => a !== null).length);

  get formattedTime(): string {
    const s = this.timerSeconds();
    const mins = Math.floor(s / 60);
    const secs = s % 60;
    return `${mins.toString().padStart(2, '0')}:${secs.toString().padStart(2, '0')}`;
  }

  get questionCountOptions(): number[] {
    const filtered = this.filteredQuestionPool;
    const total = filtered.length;
    const options: number[] = [];
    if (total >= 5) options.push(5);
    if (total >= 10) options.push(10);
    if (total >= 15) options.push(15);
    if (!options.includes(total)) options.push(total);
    return options;
  }

  get filteredQuestionPool(): Question[] {
    const types = this.selectedQuestionTypes();
    return this.allQuestions.filter(q => types.has(q.type));
  }

  getQuestionTypeCounts(): { mc: number; tf: number; fb: number } {
    return {
      mc: this.allQuestions.filter(q => q.type === 'multiple_choice').length,
      tf: this.allQuestions.filter(q => q.type === 'true_false').length,
      fb: this.allQuestions.filter(q => q.type === 'fill_blank').length,
    };
  }

  toggleQuestionType(type: QuestionType) {
    this.selectedQuestionTypes.update(set => {
      const newSet = new Set(set);
      if (newSet.has(type)) {
        if (newSet.size > 1) newSet.delete(type);
      } else {
        newSet.add(type);
      }
      return newSet;
    });
    // Adjust question count if needed
    const maxAvail = this.filteredQuestionPool.length;
    if (this.questionCount() > maxAvail) {
      this.questionCount.set(maxAvail);
    }
  }

  startQuiz() {
    let qs = [...this.filteredQuestionPool];
    if (this.shuffleQuestions()) {
      qs = qs.sort(() => Math.random() - 0.5);
    }
    this.questions = qs.slice(0, this.questionCount());
    this.selectedAnswers.set(new Array(this.questions.length).fill(null));
    this.flaggedQuestions.set(new Set());
    this.currentIndex.set(0);
    this.showHint.set(false);
    this.fillBlankInput.set('');

    if (this.showTimer()) {
      this.timerSeconds.set(0);
      this.timerInterval = setInterval(() => {
        this.timerSeconds.update(v => v + 1);
      }, 1000);
    }

    this.quizState.set('in-progress');
  }

  selectAnswer(value: number | boolean) {
    this.selectedAnswers.update(arr => {
      const updated = [...arr];
      updated[this.currentIndex()] = value;
      return updated;
    });
  }

  submitFillBlank() {
    const val = this.fillBlankInput().trim();
    if (val) {
      this.selectedAnswers.update(arr => {
        const updated = [...arr];
        updated[this.currentIndex()] = val;
        return updated;
      });
    }
  }

  getQuestionStatus(index: number): string {
    if (index === this.currentIndex()) return 'current';
    if (this.flaggedQuestions().has(index)) return 'flagged';
    const answer = this.selectedAnswers()[index];
    if (answer !== null) return 'answered';
    return 'unanswered';
  }

  goToQuestion(index: number) {
    // Save fill-blank before navigating away
    if (this.currentQuestion()?.type === 'fill_blank') {
      this.submitFillBlank();
    }
    this.currentIndex.set(index);
    this.showHint.set(false);
    // Load existing fill-blank answer
    this.loadFillBlankAnswer();
  }

  nextQuestion() {
    if (this.currentQuestion()?.type === 'fill_blank') {
      this.submitFillBlank();
    }
    if (this.currentIndex() < this.totalQuestions - 1) {
      this.currentIndex.update(i => i + 1);
      this.showHint.set(false);
      this.loadFillBlankAnswer();
    }
  }

  prevQuestion() {
    if (this.currentQuestion()?.type === 'fill_blank') {
      this.submitFillBlank();
    }
    if (this.currentIndex() > 0) {
      this.currentIndex.update(i => i - 1);
      this.showHint.set(false);
      this.loadFillBlankAnswer();
    }
  }

  private loadFillBlankAnswer() {
    const q = this.questions[this.currentIndex()];
    if (q?.type === 'fill_blank') {
      const existing = this.selectedAnswers()[this.currentIndex()];
      this.fillBlankInput.set(existing ? String(existing) : '');
    }
  }

  toggleHint() {
    this.showHint.update(v => !v);
  }

  toggleFlag() {
    this.flaggedQuestions.update(set => {
      const newSet = new Set(set);
      if (newSet.has(this.currentIndex())) {
        newSet.delete(this.currentIndex());
      } else {
        newSet.add(this.currentIndex());
      }
      return newSet;
    });
  }

  setQuestionCount(count: number) {
    this.questionCount.set(count);
  }

  finishQuiz() {
    if (this.currentQuestion()?.type === 'fill_blank') {
      this.submitFillBlank();
    }
    if (this.timerInterval) {
      clearInterval(this.timerInterval);
      this.timerInterval = null;
    }
    this.router.navigate(['/quiz-result', this.deckId || '1']);
  }

  get unansweredCount(): number {
    return this.selectedAnswers().filter(a => a === null).length;
  }

  get flaggedCount(): number {
    return this.flaggedQuestions().size;
  }

  getTypeIcon(type: QuestionType): string {
    switch (type) {
      case 'multiple_choice': return 'checklist';
      case 'true_false': return 'check_circle';
      case 'fill_blank': return 'edit_note';
    }
  }

  getTypeLabel(type: QuestionType): string {
    switch (type) {
      case 'multiple_choice': return 'Multiple Choice';
      case 'true_false': return 'True / False';
      case 'fill_blank': return 'Fill in the Blank';
    }
  }

  onFillInput(event: Event) {
    const input = event.target as HTMLInputElement;
    this.fillBlankInput.set(input.value);
  }

  ngOnDestroy() {
    if (this.timerInterval) {
      clearInterval(this.timerInterval);
    }
  }
}
