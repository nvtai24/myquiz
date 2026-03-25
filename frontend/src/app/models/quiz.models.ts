// ── Request DTOs ──

export interface UserAnswerRequest {
  questionId: number;
  answer: string[];
}

export interface SubmitQuizAttemptRequest {
  deckId: string;
  startedAt: string;
  endedAt: string;
  totalTime: number;
  userAnswers: UserAnswerRequest[];
}

// ── Response DTOs ──

export interface UserAnswerResponse {
  id: string;
  questionId: number;
  question: string;
  type: string;
  hint: string | null;
  explanation: string | null;
  options: string[];
  correctAnswers: string[];
  answer: string[];
  isCorrect: boolean;
}

export interface QuizAttemptResponse {
  id: string;
  deckName: string;
  deckId: string;
  userId: string;
  startedAt: string;
  endedAt: string;
  totalTime: number;
  totalQuestions: number;
  correctAnswers: number;
  score: number;
  userAnswers: UserAnswerResponse[];
}
