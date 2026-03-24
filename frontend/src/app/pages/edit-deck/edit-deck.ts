import { Component, inject, signal, OnInit } from '@angular/core';
import Swal from 'sweetalert2';
import { FormsModule } from '@angular/forms';
import { Router, ActivatedRoute } from '@angular/router';
import { DeckService } from '../../services/deck.service';
import { AuthService } from '../../services/auth.service';
import {
  CreateQuestionRequest,
  DeckStatus,
  DeckVisibility,
  QuestionType,
  UpdateDeckRequest,
  UpdateQuestionRequest,
} from '../../models/deck.models';
import { forkJoin, of, switchMap, map, finalize } from 'rxjs';
import { CommonModule } from '@angular/common';

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
  selector: 'app-edit-deck',
  standalone: true,
  imports: [FormsModule, CommonModule],
  templateUrl: './edit-deck.html',
})
export class EditDeck implements OnInit {
  title = signal('');
  description = signal('');
  visibility = signal<'public' | 'private' | 'shared'>('public');
  coverImage = signal<string | null>(null);
  coverFile = signal<File | null>(null);
  saving = signal(false);
  deleting = signal(false);
  errorMessage = signal<string | null>(null);
  successMessage = signal<string | null>(null);

  tags = signal<string[]>([]);
  tagInput = signal('');

  editingId = signal<string | null>(null);
  editingStatus = signal<DeckStatus | null>(null);
  loadingDeck = signal(false);
  documentUrl = signal<string | null>(null);
  deletedQuestionIds = signal<number[]>([]);

  // snapshot of original question data keyed by questionId (for change detection in edit mode)
  private originalQuestions = new Map<number, string>();

  private nextId = 1;
  cards = signal<Card[]>([]);

  dragIndex = signal<number | null>(null);
  grabIndex = signal<number | null>(null); // which card's handle is being held

  private deckService = inject(DeckService);
  private authService = inject(AuthService);

  constructor(
    private router: Router,
    private route: ActivatedRoute,
  ) {}

  ngOnInit() {
    this.route.paramMap.subscribe((params) => {
      const id = params.get('id');
      if (id) {
        this.editingId.set(id);
        this.loadDeck(id);
      } else {
        this.router.navigate(['/library']);
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

  save(status?: DeckStatus) {
    if (!this.title().trim()) {
      this.showError('Title is required');
      return;
    }
    if (this.saving()) return;
    this.saving.set(true);
    this.errorMessage.set(null);

    const visMap: Record<string, DeckVisibility> = {
      public: 'Public',
      private: 'Private',
      shared: 'Shared',
    };
    const visibility: DeckVisibility = visMap[this.visibility()] ?? 'Public';
    const targetStatus = status ?? this.editingStatus() ?? 'Published';

    const currentCoverImage = this.coverImage();
    const newCoverFile = this.coverFile();

    // Only upload if there's a new file (not just an existing URL)
    const thumbnail$ = newCoverFile
      ? this.deckService.uploadFile(newCoverFile).pipe(
          map((res) => {
            if (res.success && res.data?.url) return res.data.url;
            throw new Error(res.message || 'Failed to upload thumbnail');
          }),
        )
      : of(currentCoverImage);

    thumbnail$
      .pipe(
        switchMap((thumbnail) => {
          const updateRequest: UpdateDeckRequest = {
            name: this.title().trim(),
            description: this.description().trim(),
            visibility,
            status: targetStatus,
            tags: this.tags(),
            thumbnailUrl: thumbnail ?? undefined,
            ...this.buildQuestionDiff(),
          };
          return this.deckService.updateDeck(this.editingId()!, updateRequest);
        }),
        finalize(() => this.saving.set(false)),
      )
      .subscribe({
        next: (res) => {
          if (res.success) {
            this.showSuccess(targetStatus === 'Published' && this.editingStatus() === 'Draft' ? 'Deck published successfully!' : 'Changes saved successfully!');
            setTimeout(() => this.router.navigate(['/library']), 1500);
          } else {
            this.showError(res.message || 'Failed to save changes');
          }
        },
        error: (err) => this.showError(this.extractErrorMessage(err)),
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

  private mapCardType(type: 'fill-blank' | 'multiple-choice' | 'true-false'): QuestionType {
    switch (type) {
      case 'multiple-choice': return 'MultipleChoice';
      case 'true-false': return 'TrueFalse';
      case 'fill-blank': return 'FillInTheBlank';
    }
  }

  private mapQuestionTypeToCardType(type: QuestionType): 'multiple-choice' | 'true-false' | 'fill-blank' {
    switch (type) {
      case 'MultipleChoice': return 'multiple-choice';
      case 'TrueFalse': return 'true-false';
      case 'FillInTheBlank': return 'fill-blank';
      default: return 'multiple-choice';
    }
  }

  private normalizeDeckStatus(status: any): DeckStatus | null {
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
}
