import { Component, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';

interface Card {
  id: number;
  type: 'fill-blank' | 'multiple-choice' | 'true-false';
  term: string;
  definition?: string;
  blankAnswer?: string;
  options?: string[];
  correctAnswers?: number[];
  isTrue?: boolean;
}

@Component({
  selector: 'app-add-deck',
  standalone: true,
  imports: [FormsModule],
  templateUrl: './add-deck.html',
  styleUrl: './add-deck.css',
})
export class AddDeck {
  title = signal('');
  description = signal('');
  visibility = signal<'public' | 'private'>('public');
  coverImage = signal<string | null>(null);
  coverFile = signal<File | null>(null);
  saved = signal(false);

  /* ── Creation mode ── */
  mode = signal<'manual' | 'ai'>('manual');

  /* ── AI Generation ── */
  aiSource = signal<'upload' | 'paste'>('upload');
  aiPasteText = signal('');
  aiFile = signal<File | null>(null);
  aiFileName = signal('');
  aiCardCount = signal(10);
  aiDifficulty = signal<'beginner' | 'intermediate' | 'expert'>('intermediate');
  aiFocusInput = signal('');
  aiFocusTopics = signal<string[]>([]);
  aiGenerating = signal(false);

  private nextId = 3;
  cards = signal<Card[]>([
    { id: 1, type: 'multiple-choice', term: '', options: ['', '', '', ''], correctAnswers: [0] },
    { id: 2, type: 'multiple-choice', term: '', options: ['', '', '', ''], correctAnswers: [0] },
  ]);

  dragIndex = signal<number | null>(null);

  constructor(private router: Router) {}

  toggleVisibility() {
    this.visibility.update(v => v === 'public' ? 'private' : 'public');
  }

  onCoverImageSelect(event: Event) {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0];
    if (file && file.type.startsWith('image/')) {
      this.coverFile.set(file);
      const reader = new FileReader();
      reader.onload = () => {
        this.coverImage.set(reader.result as string);
      };
      reader.readAsDataURL(file);
    }
  }

  removeCoverImage() {
    this.coverImage.set(null);
    this.coverFile.set(null);
  }

  addCard() {
    this.cards.update(cards => [...cards, { id: this.nextId++, type: 'multiple-choice', term: '', options: ['', '', '', ''], correctAnswers: [0] }]);
  }

  duplicateCard(index: number) {
    const card = this.cards()[index];
    const newCard: Card = { 
      id: this.nextId++, 
      type: card.type, 
      term: card.term, 
      definition: card.definition,
      options: card.options ? [...card.options] : undefined,
      correctAnswers: card.correctAnswers ? [...card.correctAnswers] : undefined,
      isTrue: card.isTrue
    };
    this.cards.update(cards => {
      const updated = [...cards];
      updated.splice(index + 1, 0, newCard);
      return updated;
    });
  }

  deleteCard(index: number) {
    if (this.cards().length <= 2) return;
    this.cards.update(cards => cards.filter((_, i) => i !== index));
  }

  updateCardType(index: number, newType: 'fill-blank' | 'multiple-choice' | 'true-false') {
    this.cards.update(cards => {
      const updated = [...cards];
      const card = { ...updated[index], type: newType };
      if (newType === 'fill-blank' && card.blankAnswer === undefined) card.blankAnswer = '';
      if (newType === 'multiple-choice' && card.options === undefined) {
        card.options = ['', '', '', ''];
        card.correctAnswers = [0];
      }
      if (newType === 'true-false' && card.isTrue === undefined) card.isTrue = true;
      updated[index] = card;
      return updated;
    });
  }

  updateTerm(index: number, value: string) {
    this.cards.update(cards => {
      const updated = [...cards];
      updated[index] = { ...updated[index], term: value };
      return updated;
    });
  }

  updateDefinition(index: number, value: string) {
    this.cards.update(cards => {
      const updated = [...cards];
      updated[index] = { ...updated[index], definition: value };
      return updated;
    });
  }

  updateBlankAnswer(index: number, value: string) {
    this.cards.update(cards => {
      const updated = [...cards];
      updated[index] = { ...updated[index], blankAnswer: value };
      return updated;
    });
  }

  updateOption(cardIndex: number, optionIndex: number, value: string) {
    this.cards.update(cards => {
      const updated = [...cards];
      const newOptions = [...(updated[cardIndex].options || [])];
      newOptions[optionIndex] = value;
      updated[cardIndex] = { ...updated[cardIndex], options: newOptions };
      return updated;
    });
  }

  toggleCorrectAnswer(cardIndex: number, optionIndex: number) {
    this.cards.update(cards => {
      const updated = [...cards];
      const currentArr = updated[cardIndex].correctAnswers || [];
      let newAnswers = [...currentArr];
      if (newAnswers.includes(optionIndex)) {
        newAnswers = newAnswers.filter(a => a !== optionIndex);
        if (newAnswers.length === 0) newAnswers = [optionIndex]; // prevent having 0 answers just to simplify logic
      } else {
        newAnswers.push(optionIndex);
      }
      updated[cardIndex] = { ...updated[cardIndex], correctAnswers: newAnswers };
      return updated;
    });
  }

  updateIsTrue(cardIndex: number, val: boolean) {
    this.cards.update(cards => {
      const updated = [...cards];
      updated[cardIndex] = { ...updated[cardIndex], isTrue: val };
      return updated;
    });
  }

  onDragStart(index: number) {
    this.dragIndex.set(index);
  }

  onDragOver(event: DragEvent, index: number) {
    event.preventDefault();
    const fromIndex = this.dragIndex();
    if (fromIndex === null || fromIndex === index) return;
    this.cards.update(cards => {
      const updated = [...cards];
      const [moved] = updated.splice(fromIndex, 1);
      updated.splice(index, 0, moved);
      return updated;
    });
    this.dragIndex.set(index);
  }

  onDragEnd() {
    this.dragIndex.set(null);
  }

  saveDraft() {
    this.saved.set(true);
  }

  create() {
    // API call to create deck
    this.router.navigate(['/library']);
  }

  cancel() {
    this.router.navigate(['/library']);
  }

  /* ── AI Methods ── */
  onAiFileSelect(event: Event) {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0];
    if (file) {
      this.aiFile.set(file);
      this.aiFileName.set(file.name);
    }
  }

  removeAiFile() {
    this.aiFile.set(null);
    this.aiFileName.set('');
  }

  onAiFileDrop(event: DragEvent) {
    event.preventDefault();
    const file = event.dataTransfer?.files[0];
    if (file) {
      this.aiFile.set(file);
      this.aiFileName.set(file.name);
    }
  }

  addFocusTopic() {
    const topic = this.aiFocusInput().trim();
    if (topic && !this.aiFocusTopics().includes(topic)) {
      this.aiFocusTopics.update(t => [...t, topic]);
      this.aiFocusInput.set('');
    }
  }

  removeFocusTopic(index: number) {
    this.aiFocusTopics.update(t => t.filter((_, i) => i !== index));
  }

  onFocusKeydown(event: KeyboardEvent) {
    if (event.key === 'Enter') {
      event.preventDefault();
      this.addFocusTopic();
    }
  }

  generateWithAi() {
    this.aiGenerating.set(true);
    setTimeout(() => {
      const generated: Card[] = Array.from({ length: this.aiCardCount() }, (_, i) => ({
        id: this.nextId++,
        type: 'fill-blank' as const,
        term: `Generated Term ${i + 1}`,
        blankAnswer: `Generated Answer ${i + 1}`,
      }));
      this.cards.set(generated);
      this.mode.set('manual');
      this.aiGenerating.set(false);
    }, 1500);
  }
}
