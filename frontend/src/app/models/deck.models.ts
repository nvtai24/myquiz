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
  createdAt: string;
  updatedAt: string | null;
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
