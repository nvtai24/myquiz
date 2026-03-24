import { Component, inject, signal, OnInit } from '@angular/core';
import Swal from 'sweetalert2';
import { FormsModule } from '@angular/forms';
import { Router, ActivatedRoute } from '@angular/router';
import { DeckService } from '../../services/deck.service';
import { PaymentService } from '../../services/payment.service';
import { AuthService } from '../../services/auth.service';
import {
  CreateDeckRequest,
  CreateQuestionRequest,
  DeckStatus,
  DeckVisibility,
  GeneratedDeckResponse,
  GeneratedQuestionResponse,
  QuestionType,
  UpdateDeckRequest,
  UpdateQuestionRequest,
} from '../../models/deck.models';
import { forkJoin, of, switchMap, map, finalize } from 'rxjs';

interface Card {
  id: number;
  questionId?: number; // set when loaded from server (edit mode)
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
})
export class AddDeck implements OnInit {
  title = signal('');
  description = signal('');
  visibility = signal<'public' | 'private' | 'shared'>('public');
  coverImage = signal<string | null>(null);
  coverFile = signal<File | null>(null);
  savingDraft = signal(false);
  creating = signal(false);
  deleting = signal(false);
  errorMessage = signal<string | null>(null);
  successMessage = signal<string | null>(null);

  tags = signal<string[]>([]);
  tagInput = signal('');

  isEditMode = signal(false);
  editingId = signal<string | null>(null);
  editingStatus = signal<DeckStatus | null>(null);
  loadingDeck = signal(false);
  documentUrl = signal<string | null>(null);
  deletedQuestionIds = signal<number[]>([]);

  /* ── Creation mode ── */
  mode = signal<'manual' | 'ai'>('manual');
  checkLimit = signal(false);
  checkLimitDeck = signal(false);
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
  lastUsedAiFile = signal<File | null>(null);

  aiUsageCount = signal<number>(0);
  aiUsageMax = signal<number>(0); // 0 means no active plan or infinite (but we have limits typically). Wait, Free plan may have 0. If 0, UI might say "Buy plan".
  aiLimitLoading = signal<boolean>(false);

  // snapshot of original question data keyed by questionId (for change detection in edit mode)
  private originalQuestions = new Map<number, string>();

  private nextId = 3;
  cards = signal<Card[]>([
    { id: 1, type: 'multiple-choice', term: '', options: ['', '', '', ''], correctAnswers: [0] },
    { id: 2, type: 'multiple-choice', term: '', options: ['', '', '', ''], correctAnswers: [0] },
  ]);

  dragIndex = signal<number | null>(null);
  grabIndex = signal<number | null>(null); // which card's handle is being held

  private deckService = inject(DeckService);
  private paymentService = inject(PaymentService);
  private authService = inject(AuthService);

  constructor(
    private router: Router,
    private route: ActivatedRoute,
  ) {}

  ngOnInit() {
    this.route.paramMap.subscribe((params) => {
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
          // No active plan
          this.aiUsageMax.set(0);
          this.aiUsageCount.set(0);
        }
      },
      error: () => {
        this.aiLimitLoading.set(false);
      },
    });
  }

  private loadDeck(id: string) {
    this.loadingDeck.set(true);
    this.deckService.getDeckById(id).subscribe({
      next: (res) => {
        this.loadingDeck.set(false);
        if (res.success && res.data) {
          const deck = res.data;
          this.editingStatus.set(this.normalizeDeckStatus(deck.status));
          this.title.set(deck.name);
          this.description.set(deck.description || '');
          this.tags.set(deck.tags ?? []);
          const visRevMap: Record<string, 'public' | 'private' | 'shared'> = {
            Public: 'public',
            Private: 'private',
            Shared: 'shared',
          };
          this.visibility.set(visRevMap[deck.visibility] ?? 'public');
          this.coverImage.set(deck.thumbnailUrl || null);

          const currentUserEmail = this.authService.currentUser()?.email;
          if (deck.visibility === 'Shared' && deck.ownerEmail !== currentUserEmail) {
            this.showError('You cannot edit a shared study set');
            this.router.navigate(['/library']);
            return;
          }

          if (deck.questions && deck.questions.length > 0) {
            const mapped: Card[] = deck.questions.map((q) => {
              const cardType = this.mapQuestionTypeToCardType(q.type);
              const card: Card = {
                id: this.nextId++,
                questionId: q.id,
                type: cardType,
                term: q.content,
                hint: q.hint || undefined,
                explanation: q.explanation || undefined,
                showExtra: !!(q.hint || q.explanation),
              };

              if (cardType === 'multiple-choice') {
                card.options = q.options?.length ? [...q.options] : ['', '', '', ''];
                card.correctAnswers = q.correctAnswers
                  ?.map((ans) => card.options!.indexOf(ans))
                  .filter((i) => i >= 0) || [0];
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
            // store snapshot for change detection
            this.originalQuestions.clear();
            deck.questions.forEach((q) => {
              this.originalQuestions.set(
                q.id,
                JSON.stringify({
                  content: q.content,
                  type: q.type,
                  hint: q.hint || '',
                  explanation: q.explanation || '',
                  options: [...(q.options || [])].sort(),
                  correctAnswers: [...(q.correctAnswers || [])].sort(),
                }),
              );
            });
          }

          if (deck.documents && deck.documents.length > 0) {
            this.documentUrl.set(deck.documents[0].fileUrl);
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
      },
    });
  }

  toggleVisibility() {
    this.visibility.update((v) => (v === 'public' ? 'private' : 'public'));
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
    const card = this.cards()[index];
    if (card.questionId != null) {
      this.deletedQuestionIds.update((ids) => [...ids, card.questionId!]);
    }
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

  updateDefinition(index: number, value: string) {
    this.cards.update((cards) => {
      const updated = [...cards];
      updated[index] = { ...updated[index], definition: value };
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
        if (newAnswers.length === 0) newAnswers = [optionIndex]; // prevent having 0 answers just to simplify logic
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

  get canShowSaveDraft(): boolean {
    return !this.isEditMode() || this.editingStatus() === 'Draft';
  }

  saveDraft() {
    this.submitDeck('Draft');
  }

  create() {
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
    const isEdit = this.isEditMode() && !!this.editingId();

    const createRequest: CreateDeckRequest = {
      name: this.title().trim(),
      description: this.description().trim(),
      visibility,
      status,
      source: this.aiGenerated() ? 'AiGenerated' : 'Manual',
      tags: this.tags(),
      questions: this.buildQuestions(),
      thumbnailUrl: this.coverImage() || undefined,
      documentUrl: this.documentUrl() || undefined,
    };

    const thumbnail$ = this.coverFile()
      ? this.deckService.uploadFile(this.coverFile()!).pipe(
          map((res) => {
            if (res.success && res.data?.url) return res.data.url;
            throw new Error(res.message || 'Failed to upload thumbnail');
          }),
        )
      : of(createRequest.thumbnailUrl);

    const document$ =
      this.aiGenerated() && this.lastUsedAiFile()
        ? this.deckService.uploadFile(this.lastUsedAiFile()!).pipe(
            map((res) => {
              if (res.success && res.data?.url) return res.data.url;
              throw new Error(res.message || 'Failed to upload AI document');
            }),
          )
        : of(createRequest.documentUrl);

    forkJoin({ thumbnail: thumbnail$, document: document$ })
      .pipe(
        switchMap(({ thumbnail, document }) => {
          createRequest.thumbnailUrl = thumbnail;
          createRequest.documentUrl = document;

          if (isEdit) {
            const updateRequest: UpdateDeckRequest = {
              name: createRequest.name,
              description: createRequest.description,
              visibility,
              status,
              tags: this.tags(),
              thumbnailUrl: thumbnail,
              ...this.buildQuestionDiff(),
            };
            return this.deckService.updateDeck(this.editingId()!, updateRequest);
          }

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
            if (isDraft) {
              this.showSuccess('Draft saved successfully!');
            } else {
              this.router.navigate(['/library']);
            }
          } else {
            this.showError(res.message || `Failed to ${isDraft ? 'save draft' : 'create deck'}`);
          }
        },
        error: (err) => {
          this.showError(this.extractErrorMessage(err));
        },
      });
  }

  private buildQuestionDiff(): Pick<
    UpdateDeckRequest,
    'questionsToAdd' | 'questionsToUpdate' | 'questionIdsToDelete'
  > {
    const questionsToAdd: CreateQuestionRequest[] = [];
    const questionsToUpdate: UpdateQuestionRequest[] = [];

    for (const card of this.cards()) {
      if (!card.term.trim()) continue;
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

      const base = {
        content: card.term,
        type,
        hint: card.hint?.trim() || '',
        explanation: card.explanation?.trim() || '',
        options,
        correctAnswers,
      };

      if (card.questionId != null) {
        const snapshot = JSON.stringify({
          content: base.content,
          type: base.type,
          hint: base.hint,
          explanation: base.explanation,
          options: [...base.options].sort(),
          correctAnswers: [...base.correctAnswers].sort(),
        });
        const original = this.originalQuestions.get(card.questionId);
        if (snapshot !== original) {
          questionsToUpdate.push({ id: card.questionId, ...base });
        }
      } else {
        questionsToAdd.push(base);
      }
    }

    return {
      questionsToAdd,
      questionsToUpdate,
      questionIdsToDelete: this.deletedQuestionIds(),
    };
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
      case 'multiple-choice':
        return 'MultipleChoice';
      case 'true-false':
        return 'TrueFalse';
      case 'fill-blank':
        return 'FillInTheBlank';
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

  private normalizeDeckStatus(
    status: DeckStatus | number | string | null | undefined,
  ): DeckStatus | null {
    if (status === 'Draft' || status === 0 || status === '0') return 'Draft';
    if (status === 'Published' || status === 1 || status === '1') return 'Published';
    return null;
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

  deleteDeck() {
    const id = this.editingId();
    if (!id) return;
    Swal.fire({
      title: 'Delete study set?',
      text: 'This action cannot be undone. All cards in this set will be permanently removed.',
      icon: 'warning',
      showCancelButton: true,
      confirmButtonColor: '#ef4444',
      cancelButtonColor: '#6b7280',
      confirmButtonText: 'Yes, delete it',
      cancelButtonText: 'Cancel',
    }).then((result) => {
      if (!result.isConfirmed) return;
      this.deleting.set(true);
      this.deckService.deleteDeck(id).subscribe({
        next: (res) => {
          this.deleting.set(false);
          if (res.success) {
            Swal.fire({
              title: 'Deleted!',
              text: 'Your study set has been deleted.',
              icon: 'success',
              confirmButtonColor: '#4255FF',
              timer: 1800,
              showConfirmButton: false,
            }).then(() => this.router.navigate(['/library']));
          } else {
            this.showError(res.message || 'Failed to delete study set');
          }
        },
        error: (err) => {
          this.deleting.set(false);
          this.showError(this.extractErrorMessage(err));
        },
      });
    });
  }

  cancel() {
    this.router.navigate(['/library']);
  }
  update() {
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

  addFocusTopic() {
    const topic = this.aiFocusInput().trim();
    if (topic && !this.aiFocusTopics().includes(topic)) {
      this.aiFocusTopics.update((t) => [...t, topic]);
      this.aiFocusInput.set('');
    }
  }

  removeFocusTopic(index: number) {
    this.aiFocusTopics.update((t) => t.filter((_, i) => i !== index));
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
    // Auto-fill title/description only if user hasn't typed anything yet
    if (!this.title().trim() && deck.name) {
      this.title.set(deck.name);
    }
    if (!this.description().trim() && deck.description) {
      this.description.set(deck.description);
    }

    // Map GeneratedQuestionResponse[] → Card[]
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
        // Map correctAnswers strings back to indices
        card.correctAnswers = q.correctAnswers
          .map((ans) => card.options!.indexOf(ans))
          .filter((idx) => idx >= 0);
        if (card.correctAnswers.length === 0) card.correctAnswers = [0];
      } else if (cardType === 'true-false') {
        const correct = q.correctAnswers[0]?.toLowerCase();
        card.isTrue = correct !== 'false';
      } else if (cardType === 'fill-blank') {
        card.blankAnswer = q.correctAnswers[0] || '';
      }

      return card;
    });

    this.cards.set(
      mapped.length > 0
        ? mapped
        : [
            {
              id: this.nextId++,
              type: 'multiple-choice',
              term: '',
              options: ['', '', '', ''],
              correctAnswers: [0],
            },
            {
              id: this.nextId++,
              type: 'multiple-choice',
              term: '',
              options: ['', '', '', ''],
              correctAnswers: [0],
            },
          ],
    );

    // Switch to manual mode so the user can review/edit
    this.mode.set('manual');
    this.aiGenerated.set(true);
    this.showSuccess(`${mapped.length} cards generated! Review and edit them before saving.`);
  }

  private mapQuestionTypeToCardType(
    type: QuestionType,
  ): 'multiple-choice' | 'true-false' | 'fill-blank' {
    switch (type) {
      case 'MultipleChoice':
        return 'multiple-choice';
      case 'TrueFalse':
        return 'true-false';
      case 'FillInTheBlank':
        return 'fill-blank';
      default:
        return 'multiple-choice';
    }
  }
}
