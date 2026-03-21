import { Component, OnInit, OnDestroy, signal, computed, inject } from '@angular/core';
import { DecimalPipe, CommonModule } from '@angular/common';
import { Router, ActivatedRoute } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { DeckService } from '../../services/deck.service';
import { QuestionResponse } from '../../models/deck.models';

type LocalQuestionType = 'multiple_choice' | 'true_false' | 'fill_blank';

interface QuizQuestion {
  id: number;
  originalId: number;
  text: string;
  type: LocalQuestionType;
  hint?: string;
  options: { label: string; text: string }[];     // for MC
  correctOption: number;                            // MC index
  correctBool: boolean;                             // TF
  correctText: string;                              // fill_blank
}

type QuizState = 'loading' | 'error' | 'setup' | 'in-progress';

@Component({
  selector: 'app-quiz',
  standalone: true,
  imports: [DecimalPipe, FormsModule, CommonModule],
  templateUrl: './quiz.html',
  styleUrl: './quiz.css',
})
export class Quiz implements OnInit, OnDestroy {
  private router = inject(Router);
  private route = inject(ActivatedRoute);
  private deckService = inject(DeckService);

  deckTitle = signal('');
  deckId = '';
  quizState = signal<QuizState>('loading');
  apiError = signal<string | null>(null);
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
  selectedQuestionTypes = signal<Set<LocalQuestionType>>(new Set(['multiple_choice', 'true_false', 'fill_blank']));

  allQuestions: QuizQuestion[] = [];  // built from API
  questions: QuizQuestion[] = [];
  fillBlankInput = signal('');

  ngOnInit(): void {
    this.route.params.subscribe(params => {
      this.deckId = params['id'] || '';
      if (this.deckId) this.loadDeck();
    });
  }

  loadDeck() {
    this.quizState.set('loading');
    this.apiError.set(null);
    this.deckService.getDeckById(this.deckId).subscribe({
      next: (res) => {
        this.deckTitle.set(res.data!.name);
        this.allQuestions = this.convertToQuizQuestions(res.data!.questions || []);
        this.quizState.set('setup');
      },
      error: (err) => {
        this.apiError.set(err?.error?.message ?? 'Không thể tải bộ câu hỏi.');
        this.quizState.set('error');
      }
    });
  }

  private convertToQuizQuestions(apiQs: QuestionResponse[]): QuizQuestion[] {
    return apiQs.map((q, idx): QuizQuestion => {
      const backendType = q.type; // 'MultipleChoice' | 'TrueFalse' | 'FillInTheBlank'
      let localType: LocalQuestionType = 'multiple_choice';
      if (backendType === 'TrueFalse') localType = 'true_false';
      else if (backendType === 'FillInTheBlank') localType = 'fill_blank';

      // Multiple Choice: convert options array to {label, text}[]
      const mcOptions = (q.options || []).map((opt, i) => ({
        label: String.fromCharCode(65 + i), // A, B, C, D
        text: opt
      }));
      const correctIdx = q.options?.indexOf(q.correctAnswers?.[0] ?? '') ?? 0;

      // True/False
      const correctBool = q.correctAnswers?.[0]?.toLowerCase() === 'true';

      // Fill blank
      const correctText = q.correctAnswers?.[0] ?? '';

      return {
        id: idx + 1,
        originalId: q.id,
        text: q.content,
        type: localType,
        hint: q.hint || undefined,
        options: mcOptions,
        correctOption: correctIdx >= 0 ? correctIdx : 0,
        correctBool,
        correctText,
      };
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

  get filteredQuestionPool(): QuizQuestion[] {
    const types = this.selectedQuestionTypes();
    return this.allQuestions.filter(q => types.has(q.type));
  }

  get questionCountOptions(): number[] {
    const total = this.filteredQuestionPool.length;
    const options: number[] = [];
    if (total >= 5) options.push(5);
    if (total >= 10) options.push(10);
    if (total >= 15) options.push(15);
    if (!options.includes(total)) options.push(total);
    return options;
  }

  getQuestionTypeCounts(): { mc: number; tf: number; fb: number } {
    return {
      mc: this.allQuestions.filter(q => q.type === 'multiple_choice').length,
      tf: this.allQuestions.filter(q => q.type === 'true_false').length,
      fb: this.allQuestions.filter(q => q.type === 'fill_blank').length,
    };
  }

  toggleQuestionType(type: LocalQuestionType) {
    this.selectedQuestionTypes.update(set => {
      const newSet = new Set(set);
      if (newSet.has(type)) {
        if (newSet.size > 1) newSet.delete(type);
      } else {
        newSet.add(type);
      }
      return newSet;
    });
    const maxAvail = this.filteredQuestionPool.length;
    if (this.questionCount() > maxAvail) this.questionCount.set(maxAvail);
  }

  startQuiz() {
    let qs = [...this.filteredQuestionPool];
    if (this.shuffleQuestions()) qs = qs.sort(() => Math.random() - 0.5);
    this.questions = qs.slice(0, this.questionCount());
    this.selectedAnswers.set(new Array(this.questions.length).fill(null));
    this.flaggedQuestions.set(new Set());
    this.currentIndex.set(0);
    this.showHint.set(false);
    this.fillBlankInput.set('');

    if (this.showTimer()) {
      this.timerSeconds.set(0);
      this.timerInterval = setInterval(() => this.timerSeconds.update(v => v + 1), 1000);
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
    if (this.currentQuestion()?.type === 'fill_blank') this.submitFillBlank();
    this.currentIndex.set(index);
    this.showHint.set(false);
    this.loadFillBlankAnswer();
  }

  nextQuestion() {
    if (this.currentQuestion()?.type === 'fill_blank') this.submitFillBlank();
    if (this.currentIndex() < this.totalQuestions - 1) {
      this.currentIndex.update(i => i + 1);
      this.showHint.set(false);
      this.loadFillBlankAnswer();
    }
  }

  prevQuestion() {
    if (this.currentQuestion()?.type === 'fill_blank') this.submitFillBlank();
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

  toggleHint() { this.showHint.update(v => !v); }

  toggleFlag() {
    this.flaggedQuestions.update(set => {
      const newSet = new Set(set);
      if (newSet.has(this.currentIndex())) newSet.delete(this.currentIndex());
      else newSet.add(this.currentIndex());
      return newSet;
    });
  }

  setQuestionCount(count: number) { this.questionCount.set(count); }

  finishQuiz() {
    if (this.currentQuestion()?.type === 'fill_blank') this.submitFillBlank();
    if (this.timerInterval) {
      clearInterval(this.timerInterval);
      this.timerInterval = null;
    }

    // Calculate results and pass via navigation state
    let correct = 0;
    this.questions.forEach((q, i) => {
      const ans = this.selectedAnswers()[i];
      if (q.type === 'multiple_choice' && ans === q.correctOption) correct++;
      else if (q.type === 'true_false' && ans === q.correctBool) correct++;
      else if (q.type === 'fill_blank') {
        const ansStr = String(ans ?? '').toLowerCase().trim();
        const corrStr = q.correctText.toLowerCase().trim();
        if (ansStr === corrStr) correct++;
      }
    });

    const state = {
      deckId: this.deckId,
      deckTitle: this.deckTitle(),
      totalQuestions: this.totalQuestions,
      correctAnswers: correct,
      timeTaken: this.formattedTime,
      questions: this.questions.map((q, i) => {
        const ans = this.selectedAnswers()[i];
        let userAnswerText: string;
        let correctAnswerText: string;
        let isCorrect = false;

        if (q.type === 'multiple_choice') {
          userAnswerText = typeof ans === 'number' ? (q.options[ans]?.text ?? '--') : '--';
          correctAnswerText = q.options[q.correctOption]?.text ?? '--';
          isCorrect = ans === q.correctOption;
        } else if (q.type === 'true_false') {
          userAnswerText = ans === true ? 'True' : ans === false ? 'False' : '--';
          correctAnswerText = q.correctBool ? 'True' : 'False';
          isCorrect = ans === q.correctBool;
        } else {
          userAnswerText = String(ans ?? '--');
          correctAnswerText = q.correctText;
          isCorrect = String(ans ?? '').toLowerCase().trim() === q.correctText.toLowerCase().trim();
        }

        return {
          number: i + 1,
          text: q.text,
          type: q.type,
          userAnswer: userAnswerText,
          correctAnswer: correctAnswerText,
          isCorrect,
          explanation: q.hint || '',
          options: q.options.map(opt => opt.text),
          correctOptionIndex: q.correctOption,
          userSelectedIndex: typeof ans === 'number' ? ans : null,
        };
      })
    };

    this.router.navigate(['/quiz-result', this.deckId], { state });
  }

  get unansweredCount(): number {
    return this.selectedAnswers().filter(a => a === null).length;
  }

  get flaggedCount(): number {
    return this.flaggedQuestions().size;
  }

  getTypeIcon(type: LocalQuestionType): string {
    switch (type) {
      case 'multiple_choice': return 'checklist';
      case 'true_false': return 'check_circle';
      case 'fill_blank': return 'edit_note';
    }
  }

  getTypeLabel(type: LocalQuestionType): string {
    switch (type) {
      case 'multiple_choice': return 'Trắc nghiệm';
      case 'true_false': return 'Đúng / Sai';
      case 'fill_blank': return 'Điền vào chỗ trống';
    }
  }

  onFillInput(event: Event) {
    const input = event.target as HTMLInputElement;
    this.fillBlankInput.set(input.value);
  }

  ngOnDestroy() {
    if (this.timerInterval) clearInterval(this.timerInterval);
  }
}
