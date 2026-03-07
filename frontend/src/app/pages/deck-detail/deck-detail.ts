import { Component, signal } from '@angular/core';
import { Router, RouterModule } from '@angular/router';

interface Term {
  term: string;
  definition: string;
}

interface Review {
  name: string;
  timeAgo: string;
  text: string;
  avatar: string;
  rating: number;
}

@Component({
  selector: 'app-deck-detail',
  standalone: true,
  imports: [RouterModule],
  templateUrl: './deck-detail.html',
  styleUrl: './deck-detail.css',
})
export class DeckDetail {
  studyMode = signal<string>('quiz');
  showAllTerms = signal(false);
  filterText = signal('');

  deck = {
    id: 1,
    title: 'Mathematics Basics',
    description: 'A comprehensive set of essential mathematics concepts, from algebra to calculus. Perfect for students preparing for exams.',
    visibility: 'PUBLIC',
    author: 'Alex Johnson',
    role: 'Professor',
    createdAgo: '2 months ago',
    coverImage: '',
    stats: {
      cards: 25,
      attempts: 143,
      rating: 4.7,
      avgScore: 87,
    },
  };

  studyModes = [
    { key: 'learn', label: 'Learn', icon: 'auto_stories', desc: 'Personalized study path', gradient: 'linear-gradient(135deg, #f093fb, #f5576c)' },
    { key: 'quiz', label: 'Quiz', icon: 'quiz', desc: 'Test your knowledge', gradient: 'linear-gradient(135deg, #4255FF, #6366f1)' },
  ];

  terms: Term[] = [
    { term: 'Pythagorean Theorem', definition: 'A fundamental relation in Euclidean geometry among the three sides of a right triangle. It states that the area of the square whose side is the hypotenuse is equal to the sum of the areas of the squares on the other two sides.' },
    { term: 'Prime Number', definition: 'A natural number greater than 1 that is not a product of two smaller natural numbers. It only has two divisors: 1 and itself.' },
    { term: 'Golden Ratio', definition: 'Approximately 1.618, often denoted by the Greek letter phi (φ). Two quantities are in the golden ratio if their ratio is the same as the ratio of their sum to the larger of the two quantities.' },
    { term: 'Quadratic Formula', definition: 'The formula x = (-b ± √(b²-4ac)) / 2a used to find the solutions of a quadratic equation ax² + bx + c = 0.' },
    { term: 'Fibonacci Sequence', definition: 'A sequence where each number is the sum of the two preceding ones, starting from 0 and 1. The sequence goes 0, 1, 1, 2, 3, 5, 8, 13, 21, ...' },
    { term: 'Euler\'s Number', definition: 'The mathematical constant e ≈ 2.71828, which is the base of the natural logarithm. It is the limit of (1 + 1/n)^n as n approaches infinity.' },
  ];

  ratingBreakdown = [
    { stars: 5, count: 85 },
    { stars: 4, count: 35 },
    { stars: 3, count: 15 },
    { stars: 2, count: 5 },
    { stars: 1, count: 3 },
  ];

  reviews: Review[] = [
    { name: 'Sarah M.', timeAgo: '3d ago', text: 'Perfect for brushing up before my calc exam. The quiz mode is great!', avatar: 'S', rating: 5 },
    { name: 'James T.', timeAgo: '1w ago', text: 'Clear definitions and useful diagrams. Highly recommended.', avatar: 'J', rating: 4 },
    { name: 'Michelle K.', timeAgo: '2w ago', text: 'Really helped me understand the fundamentals. Will definitely come back!', avatar: 'M', rating: 5 },
  ];

  constructor(private router: Router) {}

  get totalRatings(): number {
    return this.ratingBreakdown.reduce((sum, r) => sum + r.count, 0);
  }

  get visibleTerms(): Term[] {
    const filtered = this.filterText()
      ? this.terms.filter(t =>
          t.term.toLowerCase().includes(this.filterText().toLowerCase()) ||
          t.definition.toLowerCase().includes(this.filterText().toLowerCase())
        )
      : this.terms;
    return this.showAllTerms() ? filtered : filtered.slice(0, 4);
  }

  get totalFilteredTerms(): number {
    if (!this.filterText()) return this.terms.length;
    return this.terms.filter(t =>
      t.term.toLowerCase().includes(this.filterText().toLowerCase()) ||
      t.definition.toLowerCase().includes(this.filterText().toLowerCase())
    ).length;
  }

  getBarWidth(count: number): string {
    const max = Math.max(...this.ratingBreakdown.map(r => r.count));
    return (count / max * 100) + '%';
  }

  selectMode(key: string) {
    this.studyMode.set(key);
  }

  toggleTerms() {
    this.showAllTerms.update(v => !v);
  }

  onFilterInput(event: Event) {
    const input = event.target as HTMLInputElement;
    this.filterText.set(input.value);
  }

  startStudy() {
    const mode = this.studyMode();
    if (mode === 'quiz') {
      this.router.navigate(['/quiz', this.deck.id]);
    } else if (mode === 'learn') {
      this.router.navigate(['/learn', this.deck.id]);
    }
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
}
