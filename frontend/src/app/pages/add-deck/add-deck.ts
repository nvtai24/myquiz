import { Component, inject, signal, OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router, ActivatedRoute } from '@angular/router';
import { DeckService } from '../../services/deck.service';
import { PaymentService } from '../../services/payment.service';
import { CreateDeckRequest, CreateQuestionRequest, GeneratedDeck, GeneratedQuestion, QuestionType } from '../../models/deck.models';

interface Card {
  id: number;
  type: 'fill-blank' | 'multiple-choice' | 'true-false';
  term: string;
  definition?: string;
  blankAnswer?: string;
  options?: string[];
  correctAnswers?: number[];
  isTrue?: boolean;
  hint?: string;
  explanation?: string;
  showExtra?: boolean;
}

@Component({
  selector: 'app-add-deck',
  standalone: true,
  imports: [FormsModule],
  templateUrl: './add-deck.html',
  styleUrl: './add-deck.css',
})
export class AddDeck implements OnInit {
  title = signal('');
  description = signal('');
  visibility = signal<'public' | 'private'>('public');
  coverImage = signal<string | null>(null);
  coverFile = signal<File | null>(null);
  savingDraft = signal(false);
  creating = signal(false);
    errorMessage = signal<string | null>(null);
  successMessage = signal<string | null>(null);

  isEditMode = signal(false);
  editingId = signal<string | null>(null);
  loadingDeck = signal(false);

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
  aiGenerated = signal(false); // true if cards came from AI

  aiUsageCount = signal<number>(0);
  aiUsageMax = signal<number>(0); // 0 means no active plan or infinite (but we have limits typically). Wait, Free plan may have 0. If 0, UI might say "Buy plan".
  aiLimitLoading = signal<boolean>(false);

  private nextId = 3;
  cards = signal<Card[]>([
    { id: 1, type: 'multiple-choice', term: '', options: ['', '', '', ''], correctAnswers: [0] },
    { id: 2, type: 'multiple-choice', term: '', options: ['', '', '', ''], correctAnswers: [0] },
  ]);

  dragIndex = signal<number | null>(null);
  grabIndex = signal<number | null>(null); // which card's handle is being held

  private deckService = inject(DeckService);
  private paymentService = inject(PaymentService);

  constructor(private router: Router, private route: ActivatedRoute) {}

  ngOnInit() {
    this.route.paramMap.subscribe(params => {
      const id = params.get('id');
      if (id) {
        this.isEditMode.set(true);
        this.editingId.set(id);
        this.loadDeck(id);
      }
    });
    this.loadSubscriptionLimits();
  }

  private loadSubscriptionLimits() {
    this.aiLimitLoading.set(true);
    this.paymentService.getMySubscription().subscribe({
      next: (sub) => {
        this.aiLimitLoading.set(false);
        if (sub && !sub.isExpired) {
          this.aiUsageMax.set(sub.dailyGenerateLimit);
          this.aiUsageCount.set(sub.currentGenerateCount || 0);
        } else {
          // No active plan
          this.aiUsageMax.set(0); 
          this.aiUsageCount.set(0); 
        }
      },
      error: () => {
        this.aiLimitLoading.set(false);
      }
    });
  }

  private loadDeck(id: string) {
    this.loadingDeck.set(true);
    this.deckService.getDeckById(id).subscribe({
      next: (res) => {
        this.loadingDeck.set(false);
        if (res.success && res.data) {
          const deck = res.data;
          this.title.set(deck.name);
          this.description.set(deck.description || '');
          this.visibility.set(deck.visibility === 'Private' ? 'private' : 'public');
          this.coverImage.set(deck.thumbnailUrl || null);

          if (deck.visibility === 'Shared') {
            this.showError('You cannot edit a shared study set');
            this.router.navigate(['/library']);
            return;
          }

          if (deck.questions && deck.questions.length > 0) {
            const mapped: Card[] = deck.questions.map(q => {
              const cardType = this.mapQuestionTypeToCardType(q.type);
              const card: Card = {
                id: this.nextId++,
                type: cardType,
                term: q.content,
                hint: q.hint || undefined,
                explanation: q.explanation || undefined,
                showExtra: !!(q.hint || q.explanation)
              };

              if (cardType === 'multiple-choice') {
                card.options = q.options?.length ? [...q.options] : ['', '', '', ''];
                card.correctAnswers = q.correctAnswers?.map(ans => card.options!.indexOf(ans)).filter(i => i >= 0) || [0];
                if (card.correctAnswers.length === 0) card.correctAnswers = [0];
              } else if (cardType === 'true-false') {
                const correctStr = (q.correctAnswers?.[0] || 'true').toLowerCase();
                card.isTrue = correctStr !== 'false';
              } else if (cardType === 'fill-blank') {
                card.blankAnswer = q.correctAnswers?.[0] || '';
              }
              return card;
            });
            this.cards.set(mapped);
          }
        } else {
          this.showError('Could not load deck details');
          this.router.navigate(['/library']);
        }
      },
      error: () => {
        this.loadingDeck.set(false);
        this.showError('Error loading deck');
        this.router.navigate(['/library']);
      }
    });
  }

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
      isTrue: card.isTrue,
      hint: card.hint,
      explanation: card.explanation,
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

  updateHint(index: number, value: string) {
    this.cards.update(cards => {
      const updated = [...cards];
      updated[index] = { ...updated[index], hint: value };
      return updated;
    });
  }

  updateExplanation(index: number, value: string) {
    this.cards.update(cards => {
      const updated = [...cards];
      updated[index] = { ...updated[index], explanation: value };
      return updated;
    });
  }

  toggleExtra(index: number) {
    this.cards.update(cards => {
      const updated = [...cards];
      updated[index] = { ...updated[index], showExtra: !updated[index].showExtra };
      return updated;
    });
  }

  onDragStart(index: number) {
    if (this.grabIndex() !== index) return; // only allow drag from handle
    this.dragIndex.set(index);
  }

  onGrabStart(index: number) {
    this.grabIndex.set(index);
  }

  onGrabEnd() {
    this.grabIndex.set(null);
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

  get canSaveDraft(): boolean {
    return !!this.title().trim();
  }

  saveDraft() {
    if (this.savingDraft()) return;
    this.errorMessage.set(null);

    if (!this.title().trim()) {
      this.showError('A title is required to save a draft');
      return;
    }

    this.savingDraft.set(true);

    const questions = this.buildQuestions();

    const request: CreateDeckRequest = {
      name: this.title().trim(),
      description: this.description().trim(),
      visibility: this.visibility() === 'public' ? 'Public' : 'Private',
      status: 'Draft',
      source: this.aiGenerated() ? 'AiGenerated' : 'Manual',
      tags: [],
      questions,
    };

    const action = this.isEditMode() && this.editingId() 
      ? this.deckService.updateDeck(this.editingId()!, request, this.coverFile() ?? undefined)
      : this.deckService.createDeck(request, this.coverFile() ?? undefined);

    action.subscribe({
      next: (res) => {
        this.savingDraft.set(false);
        if (res.success) {
          this.showSuccess('Draft saved successfully!');
        } else {
          this.showError(res.message || 'Failed to save draft');
        }
      },
      error: (err) => {
        this.savingDraft.set(false);
        this.showError(this.extractErrorMessage(err));
      },
    });
  }

  create() {
    if (this.creating()) return;
    if (!this.title().trim()) {
      this.showError('Title is required to publish');
      return;
    }

    if (!this.coverImage()) {
      this.showError('A cover image is required to publish this study set');
      return;
    }

    this.creating.set(true);
    this.errorMessage.set(null);

    const questions = this.buildQuestions();

    const request: CreateDeckRequest = {
      name: this.title().trim(),
      description: this.description().trim(),
      visibility: this.visibility() === 'public' ? 'Public' : 'Private',
      status: 'Published',
      source: this.aiGenerated() ? 'AiGenerated' : 'Manual',
      tags: [],
      questions,
    };

    const action = this.isEditMode() && this.editingId()
      ? this.deckService.updateDeck(this.editingId()!, request, this.coverFile() ?? undefined)
      : this.deckService.createDeck(request, this.coverFile() ?? undefined);

    action.subscribe({
      next: (res) => {
        this.creating.set(false);
        if (res.success) {
          this.router.navigate(['/library']);
        } else {
          this.showError(res.message || 'Failed to create deck');
        }
      },
      error: (err) => {
        this.creating.set(false);
        this.showError(this.extractErrorMessage(err));
      },
    });
  }

  private buildQuestions(): CreateQuestionRequest[] {
    return this.cards()
      .filter(card => card.term.trim())
      .map(card => {
        const type = this.mapCardType(card.type);
        let options: string[] = [];
        let correctAnswers: string[] = [];

        if (card.type === 'multiple-choice') {
          options = (card.options || []).filter(o => o.trim());
          correctAnswers = (card.correctAnswers || []).map(i => (card.options || [])[i]).filter(Boolean);
        } else if (card.type === 'true-false') {
          options = ['True', 'False'];
          correctAnswers = [card.isTrue ? 'True' : 'False'];
        } else if (card.type === 'fill-blank') {
          correctAnswers = card.blankAnswer ? [card.blankAnswer] : [];
        }

        return {
          content: card.term,
          type,
          hint: card.hint?.trim() || '',
          explanation: card.explanation?.trim() || '',
          options,
          correctAnswers,
        };
      });
  }

  private mapCardType(type: 'fill-blank' | 'multiple-choice' | 'true-false'): QuestionType {
    switch (type) {
      case 'multiple-choice': return 'MultipleChoice';
      case 'true-false': return 'TrueFalse';
      case 'fill-blank': return 'FillInTheBlank';
    }
  }

  private showError(msg: string) {
    this.errorMessage.set(msg);
    setTimeout(() => this.errorMessage.set(null), 5000);
  }

  private showSuccess(msg: string) {
    this.successMessage.set(msg);
    setTimeout(() => this.successMessage.set(null), 3000);
  }

  private extractErrorMessage(err: any): string {
    const body = err.error;
    // ASP.NET ValidationProblemDetails format
    if (body?.errors && typeof body.errors === 'object') {
      const messages = Object.values(body.errors).flat() as string[];
      return messages.join('. ') || body.title || 'Validation failed';
    }
    return body?.message || 'An error occurred';
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
    if (this.aiGenerating()) return;

    let fileToSend: File | null = null;

    if (this.aiSource() === 'upload') {
      if (!this.aiFile()) {
        this.showError('Please upload a file to generate cards.');
        return;
      }
      fileToSend = this.aiFile();
    } else {
      const text = this.aiPasteText().trim();
      if (!text) {
        this.showError('Please paste some text to generate cards.');
        return;
      }
      // Convert pasted text to a .txt file and send to the same endpoint
      const blob = new Blob([text], { type: 'text/plain' });
      fileToSend = new File([blob], 'pasted-content.txt', { type: 'text/plain' });
    }

    this.aiGenerating.set(true);
    this.errorMessage.set(null);

    this.deckService.generateDeck(fileToSend!).subscribe({
      next: (res) => {
        this.aiGenerating.set(false);
        if (!res.success || !res.data) {
          this.showError(res.message || 'AI generation failed');
          return;
        }
        // Increment usage count locally to reflect the success
        if (this.aiUsageMax() > 0) {
          this.aiUsageCount.update(c => c + 1);
        }
        this.applyGeneratedDeck(res.data);
      },
      error: (err) => {
        this.aiGenerating.set(false);
        this.showError(this.extractErrorMessage(err));
      },
    });
  }

  private applyGeneratedDeck(deck: GeneratedDeck) {
    // Auto-fill title/description only if user hasn't typed anything yet
    if (!this.title().trim() && deck.name) {
      this.title.set(deck.name);
    }
    if (!this.description().trim() && deck.description) {
      this.description.set(deck.description);
    }

    // Map GeneratedQuestion[] → Card[]
    const mapped: Card[] = deck.questions.map((q: GeneratedQuestion) => {
      const cardType = this.mapQuestionTypeToCardType(q.type);
      const card: Card = {
        id: this.nextId++,
        type: cardType,
        term: q.content,
        hint: q.hint || undefined,
        explanation: q.explanation || undefined,
        showExtra: !!(q.hint || q.explanation),
      };

      if (cardType === 'multiple-choice') {
        card.options = q.options.length > 0 ? [...q.options] : ['', '', '', ''];
        // Map correctAnswers strings back to indices
        card.correctAnswers = q.correctAnswers
          .map(ans => card.options!.indexOf(ans))
          .filter(idx => idx >= 0);
        if (card.correctAnswers.length === 0) card.correctAnswers = [0];
      } else if (cardType === 'true-false') {
        const correct = q.correctAnswers[0]?.toLowerCase();
        card.isTrue = correct !== 'false';
      } else if (cardType === 'fill-blank') {
        card.blankAnswer = q.correctAnswers[0] || '';
      }

      return card;
    });

    this.cards.set(mapped.length > 0 ? mapped : [
      { id: this.nextId++, type: 'multiple-choice', term: '', options: ['', '', '', ''], correctAnswers: [0] },
      { id: this.nextId++, type: 'multiple-choice', term: '', options: ['', '', '', ''], correctAnswers: [0] },
    ]);

    // Switch to manual mode so the user can review/edit
    this.mode.set('manual');
    this.aiGenerated.set(true);
    this.showSuccess(`${mapped.length} cards generated! Review and edit them before saving.`);
  }

  private mapQuestionTypeToCardType(type: QuestionType): 'multiple-choice' | 'true-false' | 'fill-blank' {
    switch (type) {
      case 'MultipleChoice': return 'multiple-choice';
      case 'TrueFalse': return 'true-false';
      case 'FillInTheBlank': return 'fill-blank';
      default: return 'multiple-choice';
    }
  }
}
