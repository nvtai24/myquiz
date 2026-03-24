import { Component, OnInit, signal, computed, inject } from '@angular/core';
import { Router, ActivatedRoute, RouterModule } from '@angular/router';
import { CommonModule } from '@angular/common';
import { DeckService } from '../../services/deck.service';
import { QuestionResponse } from '../../models/deck.models';

type CardStatus = 'new' | 'learning' | 'mastered';

@Component({
  selector: 'app-learn',
  standalone: true,
  imports: [RouterModule, CommonModule],
  templateUrl: './learn.html',
  styleUrl: './learn.css',
})
export class Learn implements OnInit {
  private router = inject(Router);
  private route = inject(ActivatedRoute);
  private deckService = inject(DeckService);

  deckId = '';
  deckTitle = signal('');
  cards = signal<QuestionResponse[]>([]);
  loading = signal(true);
  error = signal<string | null>(null);

  isFlipped = signal(false);
  showHint = signal(false);
  showExplanation = signal(false);
  currentIndex = signal(0);
  cardStatuses = signal<CardStatus[]>([]);
  isAnimating = signal(false);
  showCompleteScreen = signal(false);

  readonly MAX_DOTS = 15;

  ngOnInit(): void {
    this.route.params.subscribe(params => {
      this.deckId = params['id'] || '';
      if (this.deckId) this.loadDeck();
    });
  }

  loadDeck() {
    this.loading.set(true);
    this.error.set(null);
    this.deckService.getDeckById(this.deckId).subscribe({
      next: (res) => {
        const deck = res.data!;
        this.deckTitle.set(deck.name);
        this.cards.set(deck.questions || []);
        this.cardStatuses.set(new Array(deck.questions.length).fill('new'));
        this.loading.set(false);
      },
      error: (err) => {
        this.error.set(err?.error?.message ?? 'Failed to load deck.');
        this.loading.set(false);
      }
    });
  }

  get totalCards() { return this.cards().length; }
  get useDots(): boolean { return this.totalCards <= this.MAX_DOTS; }

  currentCard = computed(() => this.cards()[this.currentIndex()]);
  masteredCount = computed(() => this.cardStatuses().filter(s => s === 'mastered').length);
  learningCount = computed(() => this.cardStatuses().filter(s => s === 'learning').length);
  newCount = computed(() => this.cardStatuses().filter(s => s === 'new').length);

  private resetCardState() {
    this.isFlipped.set(false);
    this.showHint.set(false);
    this.showExplanation.set(false);
  }

  flipCard() {
    if (this.isAnimating()) return;
    this.isFlipped.update(v => !v);
    if (!this.isFlipped()) {
      this.showHint.set(false);
      this.showExplanation.set(false);
    }
  }

  markCard(status: CardStatus) {
    this.cardStatuses.update(arr => {
      const updated = [...arr];
      updated[this.currentIndex()] = status;
      return updated;
    });
    setTimeout(() => {
      if (this.currentIndex() < this.totalCards - 1) {
        this.goNext();
      } else {
        const allMastered = this.cardStatuses().every(s => s === 'mastered');
        if (allMastered) this.showCompleteScreen.set(true);
      }
    }, 300);
  }

  goNext() {
    if (this.currentIndex() >= this.totalCards - 1 || this.isAnimating()) return;
    this.isAnimating.set(true);
    setTimeout(() => {
      this.currentIndex.update(i => i + 1);
      this.resetCardState();
      this.isAnimating.set(false);
    }, 250);
  }

  goPrev() {
    if (this.currentIndex() <= 0 || this.isAnimating()) return;
    this.isAnimating.set(true);
    setTimeout(() => {
      this.currentIndex.update(i => i - 1);
      this.resetCardState();
      this.isAnimating.set(false);
    }, 250);
  }

  goToCard(index: number) {
    if (index === this.currentIndex() || this.isAnimating()) return;
    this.isAnimating.set(true);
    setTimeout(() => {
      this.currentIndex.set(index);
      this.resetCardState();
      this.isAnimating.set(false);
    }, 250);
  }

  getCardStatus(index: number): CardStatus {
    return this.cardStatuses()[index];
  }

  resetProgress() {
    this.cardStatuses.set(new Array(this.totalCards).fill('new'));
    this.currentIndex.set(0);
    this.resetCardState();
    this.showCompleteScreen.set(false);
  }

  goToQuiz() {
    this.router.navigate(['/quiz', this.deckId]);
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
