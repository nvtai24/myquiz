// ── Enums ──

export type DeckVisibility = 'Public' | 'Private' | 'Shared';
export type DeckStatus = 'Draft' | 'Published';
export type DeckSource = 'AiGenerated' | 'Manual';
export type QuestionType = 'MultipleChoice' | 'TrueFalse' | 'FillInTheBlank';

// ── Response DTOs ──

export interface DeckSummaryResponse {
  id: string;
  name: string;
  description: string;
  visibility: DeckVisibility;
  status: DeckStatus;
  tags: string[];
  questionCount: number;
  thumbnailUrl?: string;
  averageRating?: number;
  totalRatings?: number;
  ownerName: string;
  ownerEmail: string;
  createdAt: string;
  updatedAt: string | null;
}

export interface QuestionResponse {
  id: number;
  content: string;
  type: QuestionType;
  hint: string;
  explanation: string;
  options: string[];
  correctAnswers: string[];
}

export interface DeckDocumentResponse {
  id: string;
  fileName: string;
  fileUrl: string;
  contentType: string;
  createdAt: string;
}

export interface DeckDetailResponse extends DeckSummaryResponse {
  questions: QuestionResponse[];
  documents: DeckDocumentResponse[];
}

export interface DeckRatingResponse {
  id: string;
  deckId: string;
  userId: string;
  userName: string;
  userEmail: string;
  rating: number;
  comment: string | null;
  createdAt: string;
}

export interface DeckRatingSummaryResponse {
  averageRating: number;
  totalRatings: number;
  ratings: DeckRatingResponse[];
}

export interface GeneratedQuestionResponse {
  content: string;
  type: QuestionType;
  hint: string;
  explanation: string;
  options: string[];
  correctAnswers: string[];
}

export interface GeneratedDeckResponse {
  name: string;
  description: string;
  tags: string[];
  questions: GeneratedQuestionResponse[];
}

// ── Request DTOs ──

export interface CreateQuestionRequest {
  content: string;
  type: QuestionType;
  hint: string;
  explanation: string;
  options: string[];
  correctAnswers: string[];
}

export interface CreateDeckRequest {
  name: string;
  description: string;
  visibility: DeckVisibility;
  status: DeckStatus;
  source: DeckSource;
  tags: string[];
  questions: CreateQuestionRequest[];
  thumbnailUrl?: string;
  documentUrl?: string;
}

export interface UpdateQuestionRequest {
  id: number;
  content: string;
  type: QuestionType;
  hint: string;
  explanation: string;
  options: string[];
  correctAnswers: string[];
}

export interface UpdateDeckRequest {
  name: string;
  description: string;
  visibility: DeckVisibility;
  status: DeckStatus;
  tags: string[];
  thumbnailUrl?: string;
  questionsToAdd: CreateQuestionRequest[];
  questionsToUpdate: UpdateQuestionRequest[];
  questionIdsToDelete: number[];
}

export interface CreateDeckRatingRequest {
  rating: number;
  comment?: string;
}

export interface DeckRatingCheckResponse {
  hasRated: boolean;
  rating: number | null;
  comment: string | null;
}

export interface LimitAiGenerateResponse {
  planName: string;
  dailyGenerateLimit: number;
  dailyGenerateUsed: number;
  dailyGenerateRemaining: number;
  numDeckLimit: number;
  numDeckUsed: number;
  numDeckRemaining: number;
}