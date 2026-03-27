import { Component, inject, signal, OnInit, HostListener } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { DeckService } from '../../services/deck.service';
import { PaymentService } from '../../services/payment.service';
import { AuthService } from '../../services/auth.service';
import {
  CreateDeckRequest,
  CreateQuestionRequest,
  DeckVisibility,
  GeneratedDeckResponse,
  GeneratedQuestionResponse,
  QuestionType,
} from '../../models/deck.models';
import { forkJoin, of, switchMap, map, finalize, Subject } from 'rxjs';
import { CommonModule } from '@angular/common';
import { CanComponentDeactivate } from '../../guards/unsaved-changes.guard';

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
  imports: [FormsModule, CommonModule],
  templateUrl: './add-deck.html',
})
export class AddDeck implements OnInit, CanComponentDeactivate {
  title = signal('');
  description = signal('');
  visibility = signal<'public' | 'private' | 'shared'>('public');
  coverImage = signal<string | null>(null);
  coverFile = signal<File | null>(null);
  savingDraft = signal(false);
  creating = signal(false);
  errorMessage = signal<string | null>(null);
  successMessage = signal<string | null>(null);

  tags = signal<string[]>([]);
  tagInput = signal('');

  documentUrl = signal<string | null>(null);

  checkLimit = signal(false);
  checkLimitDeck = signal(false);

  /* ── AI Generation (Optional) ── */
  showAiPanel = signal(false);
  aiSource = signal<'upload' | 'paste'>('upload');
  aiPasteText = signal('');
  aiFile = signal<File | null>(null);
  aiFileName = signal('');
  aiCardCount = signal(10);
  aiDifficulty = signal<'beginner' | 'intermediate' | 'expert'>('intermediate');
  aiFocusInput = signal('');
  aiFocusTopics = signal<string[]>([]);
  aiGenerating = signal(false);
  aiGenerated = signal(false);
  lastUsedAiFile = signal<File | null>(null);

  /* ── AI Confirm Dialog ── */
  showAiConfirm = signal(false);
  pendingAiCards = signal<Card[]>([]);

  aiUsageCount = signal<number>(0);
  aiUsageMax = signal<number>(0); 
  aiLimitLoading = signal<boolean>(false);

  private nextId = 3;
  cards = signal<Card[]>([
    { id: 1, type: 'multiple-choice', term: '', options: ['', '', '', ''], correctAnswers: [0] },
    { id: 2, type: 'multiple-choice', term: '', options: ['', '', '', ''], correctAnswers: [0] },
  ]);

  dragIndex = signal<number | null>(null);
  grabIndex = signal<number | null>(null);

  /* ── Leave Confirmation ── */
  showLeaveConfirm = signal(false);
  private leaveConfirmSubject = new Subject<boolean>();
  private allowNavigation = false;

  private deckService = inject(DeckService);
  private paymentService = inject(PaymentService);
  private authService = inject(AuthService);

  constructor(private router: Router) {}

  ngOnInit() {
    this.loadSubscriptionLimits();
  }

  @HostListener('window:beforeunload', ['$event'])
  onBeforeUnload(event: BeforeUnloadEvent) {
    if (this.hasUnsavedData()) {
      event.preventDefault();
      return '';
    }
    return;
  }

  hasUnsavedData(): boolean {
    const hasTitle = !!this.title().trim();
    const hasDescription = !!this.description().trim();
    const hasTags = this.tags().length > 0;
    const hasCover = !!this.coverImage();
    const hasCards = this.cards().some(card => card.term.trim());
    return hasTitle || hasDescription || hasTags || hasCover || hasCards;
  }

  canDeactivate() {
    if (!this.hasUnsavedData() || this.allowNavigation) {
      return true;
    }
    this.showLeaveConfirm.set(true);
    return this.leaveConfirmSubject.asObservable();
  }

  confirmLeave() {
    this.showLeaveConfirm.set(false);
    this.leaveConfirmSubject.next(true);
  }

  cancelLeave() {
    this.showLeaveConfirm.set(false);
    this.leaveConfirmSubject.next(false);
  }

  private loadSubscriptionLimits() {
    this.aiLimitLoading.set(true);
    forkJoin({
      sub: this.paymentService.getMySubscription(),
      limit: this.paymentService.getMySubscriptionLimit(),
    }).subscribe({
      next: ({ sub, limit }) => {
        this.aiLimitLoading.set(false);
        if (limit && !sub?.isExpired) {
          this.aiUsageMax.set(limit.dailyGenerateLimit);
          this.aiUsageCount.set(limit.dailyGenerateUsed);
        } else {
          this.aiUsageMax.set(0);
          this.aiUsageCount.set(0);
        }
      },
      error: () => {
        this.aiLimitLoading.set(false);
      },
    });
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
    this.cards.update((cards) => [
      ...cards,
      {
        id: this.nextId++,
        type: 'multiple-choice',
        term: '',
        options: ['', '', '', ''],
        correctAnswers: [0],
      },
    ]);
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
    this.cards.update((cards) => {
      const updated = [...cards];
      updated.splice(index + 1, 0, newCard);
      return updated;
    });
  }

  deleteCard(index: number) {
    if (this.cards().length <= 2) return;
    this.cards.update((cards) => cards.filter((_, i) => i !== index));
  }

  updateCardType(index: number, newType: 'fill-blank' | 'multiple-choice' | 'true-false') {
    this.cards.update((cards) => {
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
    this.cards.update((cards) => {
      const updated = [...cards];
      updated[index] = { ...updated[index], term: value };
      return updated;
    });
  }

  updateOption(cardIndex: number, optionIndex: number, value: string) {
    this.cards.update((cards) => {
      const updated = [...cards];
      const newOptions = [...(updated[cardIndex].options || [])];
      newOptions[optionIndex] = value;
      updated[cardIndex] = { ...updated[cardIndex], options: newOptions };
      return updated;
    });
  }

  toggleCorrectAnswer(cardIndex: number, optionIndex: number) {
    this.cards.update((cards) => {
      const updated = [...cards];
      const currentArr = updated[cardIndex].correctAnswers || [];
      let newAnswers = [...currentArr];
      if (newAnswers.includes(optionIndex)) {
        newAnswers = newAnswers.filter((a) => a !== optionIndex);
        if (newAnswers.length === 0) newAnswers = [optionIndex];
      } else {
        newAnswers.push(optionIndex);
      }
      updated[cardIndex] = { ...updated[cardIndex], correctAnswers: newAnswers };
      return updated;
    });
  }

  updateIsTrue(cardIndex: number, val: boolean) {
    this.cards.update((cards) => {
      const updated = [...cards];
      updated[cardIndex] = { ...updated[cardIndex], isTrue: val };
      return updated;
    });
  }

  updateBlankAnswer(index: number, value: string) {
    this.cards.update((cards) => {
      const updated = [...cards];
      updated[index] = { ...updated[index], blankAnswer: value };
      return updated;
    });
  }

  updateHint(index: number, value: string) {
    this.cards.update((cards) => {
      const updated = [...cards];
      updated[index] = { ...updated[index], hint: value };
      return updated;
    });
  }

  updateExplanation(index: number, value: string) {
    this.cards.update((cards) => {
      const updated = [...cards];
      updated[index] = { ...updated[index], explanation: value };
      return updated;
    });
  }

  toggleExtra(index: number) {
    this.cards.update((cards) => {
      const updated = [...cards];
      updated[index] = { ...updated[index], showExtra: !updated[index].showExtra };
      return updated;
    });
  }

  onDragStart(index: number) {
    if (this.grabIndex() !== index) return;
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
    this.cards.update((cards) => {
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
    this.submitDeck('Draft');
  }

  publish() {
    if (!this.title().trim()) {
      this.showError('Title is required to publish');
      return;
    }
    this.submitDeck('Published');
  }

  private submitDeck(status: 'Draft' | 'Published') {
    const isDraft = status === 'Draft';
    if (isDraft) {
      if (this.savingDraft()) return;
      this.savingDraft.set(true);
    } else {
      if (this.creating()) return;
      this.creating.set(true);
    }

    this.errorMessage.set(null);

    const visMap: Record<string, DeckVisibility> = {
      public: 'Public',
      private: 'Private',
      shared: 'Shared',
    };
    const visibility: DeckVisibility = visMap[this.visibility()] ?? 'Public';

    const createRequest: CreateDeckRequest = {
      name: this.title().trim(),
      description: this.description().trim(),
      visibility,
      status,
      tags: this.tags(),
      questions: this.buildQuestions(),
      thumbnailUrl: this.coverImage() || undefined,
      documentUrl: this.documentUrl() || undefined,
    };

    const newCoverFile = this.coverFile();
    const newAiFile = (this.aiGenerated() && this.lastUsedAiFile()) ? this.lastUsedAiFile() : null;

    const thumbnail$ = newCoverFile
      ? this.deckService.uploadFile(newCoverFile).pipe(
          map((res) => {
            if (res.success && res.data?.url) return res.data.url;
            throw new Error(res.message || 'Failed to upload thumbnail');
          }),
        )
      : of(this.coverImage());

    const document$ = newAiFile
      ? this.deckService.uploadFile(newAiFile).pipe(
          map((res) => {
            if (res.success && res.data?.url) return res.data.url;
            throw new Error(res.message || 'Failed to upload AI document');
          }),
        )
      : of(this.documentUrl());

    forkJoin({ thumbnail: thumbnail$, document: document$ })
      .pipe(
        switchMap(({ thumbnail, document }) => {
          createRequest.thumbnailUrl = thumbnail ?? undefined;
          createRequest.documentUrl = document ?? undefined;
          return this.deckService.createDeck(createRequest);
        }),
        finalize(() => {
          if (isDraft) this.savingDraft.set(false);
          else this.creating.set(false);
        }),
      )
      .subscribe({
        next: (res) => {
          if (res.success) {
            this.allowNavigation = true;
            this.router.navigate(['/library']);
          } else {
            this.showError(res.message || `Failed to ${isDraft ? 'save draft' : 'create deck'}`);
          }
        },
        error: (err) => {
          this.showError(this.extractErrorMessage(err));
        },
      });
  }

  private buildQuestions(): CreateQuestionRequest[] {
    return this.cards()
      .filter((card) => card.term.trim())
      .map((card) => {
        const type = this.mapCardType(card.type);
        let options: string[] = [];
        let correctAnswers: string[] = [];

        if (card.type === 'multiple-choice') {
          options = (card.options || []).filter((o) => o.trim());
          correctAnswers = (card.correctAnswers || [])
            .map((i) => (card.options || [])[i])
            .filter(Boolean);
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
    if (body?.errors && typeof body.errors === 'object') {
      const messages = Object.values(body.errors).flat() as string[];
      return messages.join('. ') || body.title || 'Validation failed';
    }
    return body?.message || 'An error occurred';
  }

  addTag() {
    const tag = this.tagInput().trim();
    if (tag && !this.tags().includes(tag)) {
      this.tags.update((t) => [...t, tag]);
    }
    this.tagInput.set('');
  }

  removeTag(index: number) {
    this.tags.update((t) => t.filter((_, i) => i !== index));
  }

  onTagKeydown(event: KeyboardEvent) {
    if (event.key === 'Enter' || event.key === ',') {
      event.preventDefault();
      this.addTag();
    } else if (event.key === 'Backspace' && !this.tagInput() && this.tags().length > 0) {
      this.removeTag(this.tags().length - 1);
    }
  }

  cancel() {
    this.router.navigate(['/library']);
  }
  updateSubscription() {
    this.router.navigate(['/subscription']);
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

  generateWithAi() {
    if (this.aiGenerating()) return;
    let fileToSend: File | null = null;
    if (this.aiSource() === 'upload') {
      if (!this.aiFile()) {
        this.showError('Please upload a file');
        return;
      }
      fileToSend = this.aiFile();
    } else {
      const text = this.aiPasteText().trim();
      if (!text) {
        this.showError('Please paste some text');
        return;
      }
      const blob = new Blob([text], { type: 'text/plain' });
      fileToSend = new File([blob], 'pasted-content.txt', { type: 'text/plain' });
    }
    this.aiGenerating.set(true);
    this.errorMessage.set(null);
    this.lastUsedAiFile.set(fileToSend);

    this.deckService.generateDeck(fileToSend!).subscribe({
      next: (res) => {
        this.aiGenerating.set(false);
        if (!res.success || !res.data) {
          this.showError(res.message || 'AI generation failed');
          return;
        }
        if (this.aiUsageMax() !== -1) {
          this.aiUsageCount.update((c) => c + 1);
        }
        this.applyGeneratedDeckResponse(res.data);
      },
      error: (err) => {
        this.aiGenerating.set(false);
        this.showError(this.extractErrorMessage(err));
      },
    });
  }

  private applyGeneratedDeckResponse(deck: GeneratedDeckResponse) {
    if (!this.title().trim() && deck.name) this.title.set(deck.name);
    if (!this.description().trim() && deck.description) this.description.set(deck.description);
    if (deck.tags?.length) {
      const existing = this.tags();
      const newTags = deck.tags.filter(t => !existing.includes(t));
      if (newTags.length) this.tags.update(t => [...t, ...newTags]);
    }

    const mapped: Card[] = deck.questions.map((q: GeneratedQuestionResponse) => {
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
        card.correctAnswers = q.correctAnswers
          .map((ans) => card.options!.indexOf(ans))
          .filter((idx) => idx >= 0);
        if (card.correctAnswers.length === 0) card.correctAnswers = [0];
      } else if (cardType === 'true-false') {
        card.isTrue = q.correctAnswers[0]?.toLowerCase() !== 'false';
      } else if (cardType === 'fill-blank') {
        card.blankAnswer = q.correctAnswers[0] || '';
      }
      return card;
    });

    if (mapped.length === 0) return;

    // Check if there are existing non-empty cards
    const existingCards = this.cards().filter(card => card.term.trim());
    if (existingCards.length > 0) {
      // Show confirmation dialog
      this.pendingAiCards.set(mapped);
      this.showAiConfirm.set(true);
    } else {
      // No existing cards, just set the new ones
      this.cards.set(mapped);
      this.showAiPanel.set(false);
      this.aiGenerated.set(true);
      this.showSuccess(`${mapped.length} cards generated!`);
    }
  }

  confirmAiReplace() {
    const pending = this.pendingAiCards();
    this.cards.set(pending);
    this.pendingAiCards.set([]);
    this.showAiConfirm.set(false);
    this.showAiPanel.set(false);
    this.aiGenerated.set(true);
    this.showSuccess(`${pending.length} cards generated!`);
  }

  confirmAiAppend() {
    const pending = this.pendingAiCards();
    const existingCards = this.cards().filter(card => card.term.trim());
    this.cards.set([...existingCards, ...pending]);
    this.pendingAiCards.set([]);
    this.showAiConfirm.set(false);
    this.showAiPanel.set(false);
    this.aiGenerated.set(true);
    this.showSuccess(`${pending.length} cards added!`);
  }

  cancelAiConfirm() {
    this.pendingAiCards.set([]);
    this.showAiConfirm.set(false);
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
