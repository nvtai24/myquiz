import { Component, signal, computed } from '@angular/core';
import { Router, ActivatedRoute, RouterModule } from '@angular/router';

interface Flashcard {
  id: number;
  term: string;
  definition: string;
}

type CardStatus = 'new' | 'learning' | 'mastered';

@Component({
  selector: 'app-learn',
  standalone: true,
  imports: [RouterModule],
  templateUrl: './learn.html',
  styleUrl: './learn.css',
})
export class Learn {
  deckTitle = 'Mathematics Basics';
  deckId = '';

  isFlipped = signal(false);
  currentIndex = signal(0);
  cardStatuses = signal<CardStatus[]>([]);
  isAnimating = signal(false);
  slideDirection = signal<'left' | 'right' | ''>('');
  showCompleteScreen = signal(false);
  showAllTerms = signal(false);

  // Max dots to show before switching to compact nav
  readonly MAX_DOTS = 12;
  // Terms to show before "Show more"
  readonly TERMS_PAGE_SIZE = 10;

  cards: Flashcard[] = [
    { id: 1, term: 'Pythagorean Theorem', definition: 'A fundamental relation in Euclidean geometry: a² + b² = c², where c is the hypotenuse of a right triangle and a, b are the other two sides.' },
    { id: 2, term: 'Prime Number', definition: 'A natural number greater than 1 that has no positive divisors other than 1 and itself. Examples: 2, 3, 5, 7, 11, 13...' },
    { id: 3, term: 'Golden Ratio (φ)', definition: 'Approximately 1.61803. Two quantities are in the golden ratio if their ratio equals the ratio of their sum to the larger quantity. Found throughout nature and art.' },
    { id: 4, term: 'Quadratic Formula', definition: 'x = (-b ± √(b²-4ac)) / 2a — Used to find the solutions of any quadratic equation ax² + bx + c = 0.' },
    { id: 5, term: 'Fibonacci Sequence', definition: 'A sequence where each number is the sum of the two preceding ones: 0, 1, 1, 2, 3, 5, 8, 13, 21, 34, 55...' },
    { id: 6, term: 'Euler\'s Number (e)', definition: 'The mathematical constant e ≈ 2.71828. It is the base of the natural logarithm and appears in compound interest, probability, and calculus.' },
    { id: 7, term: 'Derivative', definition: 'The instantaneous rate of change of a function. For f(x) = xⁿ, the derivative f\'(x) = nxⁿ⁻¹ (power rule).' },
    { id: 8, term: 'Integral', definition: 'The reverse of differentiation. An integral calculates the area under a curve. ∫xⁿ dx = xⁿ⁺¹/(n+1) + C.' },
  ];

  constructor(private router: Router, private route: ActivatedRoute) {
    this.route.params.subscribe(params => {
      this.deckId = params['id'] || '';
    });
    this.cardStatuses.set(new Array(this.cards.length).fill('new'));
  }

  get totalCards() { return this.cards.length; }

  get useDots(): boolean { return this.totalCards <= this.MAX_DOTS; }

  get visibleTerms(): Flashcard[] {
    if (this.showAllTerms() || this.totalCards <= this.TERMS_PAGE_SIZE) {
      return this.cards;
    }
    return this.cards.slice(0, this.TERMS_PAGE_SIZE);
  }

  get hasMoreTerms(): boolean {
    return this.totalCards > this.TERMS_PAGE_SIZE && !this.showAllTerms();
  }

  get remainingTermsCount(): number {
    return this.totalCards - this.TERMS_PAGE_SIZE;
  }

  toggleShowAll() {
    this.showAllTerms.update(v => !v);
  }

  currentCard = computed(() => this.cards[this.currentIndex()]);

  progress = computed(() => {
    const statuses = this.cardStatuses();
    const mastered = statuses.filter(s => s === 'mastered').length;
    return Math.round((mastered / this.totalCards) * 100);
  });

  masteredCount = computed(() => this.cardStatuses().filter(s => s === 'mastered').length);
  learningCount = computed(() => this.cardStatuses().filter(s => s === 'learning').length);
  newCount = computed(() => this.cardStatuses().filter(s => s === 'new').length);

  flipCard() {
    if (this.isAnimating()) return;
    this.isFlipped.update(v => !v);
  }

  markCard(status: CardStatus) {
    this.cardStatuses.update(arr => {
      const updated = [...arr];
      updated[this.currentIndex()] = status;
      return updated;
    });

    // Auto advance after marking
    setTimeout(() => {
      if (this.currentIndex() < this.totalCards - 1) {
        this.goNext();
      } else {
        // Check if all mastered
        const allMastered = this.cardStatuses().every(s => s === 'mastered');
        if (allMastered) {
          this.showCompleteScreen.set(true);
        }
      }
    }, 300);
  }

  goNext() {
    if (this.currentIndex() >= this.totalCards - 1 || this.isAnimating()) return;
    this.isAnimating.set(true);
    this.slideDirection.set('left');
    setTimeout(() => {
      this.currentIndex.update(i => i + 1);
      this.isFlipped.set(false);
      this.slideDirection.set('');
      this.isAnimating.set(false);
    }, 250);
  }

  goPrev() {
    if (this.currentIndex() <= 0 || this.isAnimating()) return;
    this.isAnimating.set(true);
    this.slideDirection.set('right');
    setTimeout(() => {
      this.currentIndex.update(i => i - 1);
      this.isFlipped.set(false);
      this.slideDirection.set('');
      this.isAnimating.set(false);
    }, 250);
  }

  goToCard(index: number) {
    if (index === this.currentIndex() || this.isAnimating()) return;
    this.isAnimating.set(true);
    this.slideDirection.set(index > this.currentIndex() ? 'left' : 'right');
    setTimeout(() => {
      this.currentIndex.set(index);
      this.isFlipped.set(false);
      this.slideDirection.set('');
      this.isAnimating.set(false);
    }, 250);
  }

  getCardStatus(index: number): CardStatus {
    return this.cardStatuses()[index];
  }

  resetProgress() {
    this.cardStatuses.set(new Array(this.cards.length).fill('new'));
    this.currentIndex.set(0);
    this.isFlipped.set(false);
    this.showCompleteScreen.set(false);
  }

  goBackToDeck() {
    this.router.navigate(['/deck', this.deckId || '1']);
  }

  handleKeydown(event: KeyboardEvent) {
    switch (event.key) {
      case ' ':
      case 'Enter':
        event.preventDefault();
        this.flipCard();
        break;
      case 'ArrowRight':
        this.goNext();
        break;
      case 'ArrowLeft':
        this.goPrev();
        break;
    }
  }
}
