// ── Enums (match backend string serialization) ──

export type DeckVisibility = 'Public' | 'Private' | 'Shared';
export type DeckStatus = 'Draft' | 'Archived';
export type DeckSource = 'AiGenerated' | 'Manual';
export type QuestionType = 'MultipleChoice' | 'TrueFalse' | 'FillInTheBlank';

// ── API Response wrapper ──

export interface ApiResponse<T> {
  success: boolean;
  statusCode: number;
  message: string;
  data: T;
  errors: string[] | null;
  timestamp: string;
}

// ── Deck Summary (GET /api/Decks) ──

export interface DeckSummary {
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
  createdAt: string;
  updatedAt: string | null;
}

// ── Question (GET /api/Decks/{id}) ──

export interface QuestionResponse {
  id: number;
  content: string;
  type: QuestionType;
  hint: string;
  explanation: string;
  options: string[];
  correctAnswers: string[];
}

// ── Deck Detail (GET /api/Decks/{id}) ──

export interface DeckDetailResponse extends DeckSummary {
  questions: QuestionResponse[];
  documents: DeckDocumentResponse[];
}

export interface DeckDocumentResponse {
  id: string;
  fileName: string;
  fileUrl: string;
  contentType: string;
  createdAt: string;
}

// ── Deck Rating ──

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

export interface CreateDeckRatingRequest {
  rating: number;
  comment?: string;
}

// ── Create Deck Request (POST /api/Decks) ──

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
}

// ── AI Generate Response (POST /api/Decks/generate) ──

export interface GeneratedQuestion {
  content: string;
  type: QuestionType;
  hint: string;
  explanation: string;
  options: string[];
  correctAnswers: string[];
}

export interface GeneratedDeck {
  name: string;
  description: string;
  tags: string[];
  questions: GeneratedQuestion[];
}
