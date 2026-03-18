import { inject, Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import {
  ApiResponse,
  CreateDeckRequest,
  CreateDeckRatingRequest,
  DeckDetailResponse,
  DeckRatingSummaryResponse,
  DeckSummary,
  GeneratedDeck
} from '../models/deck.models';

@Injectable({
  providedIn: 'root',
})
export class DeckService {
  private http = inject(HttpClient);
  private apiUrl = '/api/Decks';

  getUserDecks(): Observable<ApiResponse<DeckSummary[]>> {
    return this.http.get<ApiResponse<DeckSummary[]>>(this.apiUrl);
  }

  getDeckById(id: string): Observable<ApiResponse<DeckDetailResponse>> {
    return this.http.get<ApiResponse<DeckDetailResponse>>(`${this.apiUrl}/${id}`);
  }

  getRatings(deckId: string): Observable<ApiResponse<DeckRatingSummaryResponse>> {
    return this.http.get<ApiResponse<DeckRatingSummaryResponse>>(`${this.apiUrl}/${deckId}/ratings`);
  }

  submitRating(deckId: string, request: CreateDeckRatingRequest): Observable<ApiResponse<string>> {
    return this.http.post<ApiResponse<string>>(`${this.apiUrl}/${deckId}/ratings`, request);
  }

  createDeck(request: CreateDeckRequest, file?: File): Observable<ApiResponse<string>> {
    const formData = new FormData();
    formData.append('name', request.name);
    formData.append('description', request.description);
    formData.append('visibility', request.visibility);
    formData.append('status', request.status);
    formData.append('source', request.source);

    request.tags.forEach((tag, i) => {
      formData.append(`tags[${i}]`, tag);
    });

    request.questions.forEach((q, i) => {
      formData.append(`questions[${i}].content`, q.content);
      formData.append(`questions[${i}].type`, q.type);
      if (q.hint) formData.append(`questions[${i}].hint`, q.hint);
      if (q.explanation) formData.append(`questions[${i}].explanation`, q.explanation);
      (q.options || []).forEach((opt, j) => formData.append(`questions[${i}].options[${j}]`, opt));
      (q.correctAnswers || []).forEach((ans, j) => formData.append(`questions[${i}].correctAnswers[${j}]`, ans));
    });

    if (file) formData.append('thumbnail', file, file.name);

    return this.http.post<ApiResponse<string>>(this.apiUrl, formData);
  }

  updateDeck(id: string, request: CreateDeckRequest, file?: File): Observable<ApiResponse<string>> {
    const formData = new FormData();
    formData.append('name', request.name);
    formData.append('description', request.description);
    formData.append('visibility', request.visibility);
    formData.append('status', request.status);
    formData.append('source', request.source);

    request.tags.forEach((tag, i) => {
      formData.append(`tags[${i}]`, tag);
    });

    request.questions.forEach((q, i) => {
      formData.append(`questions[${i}].content`, q.content);
      formData.append(`questions[${i}].type`, q.type);
      if (q.hint) formData.append(`questions[${i}].hint`, q.hint);
      if (q.explanation) formData.append(`questions[${i}].explanation`, q.explanation);
      (q.options || []).forEach((opt, j) => formData.append(`questions[${i}].options[${j}]`, opt));
      (q.correctAnswers || []).forEach((ans, j) => formData.append(`questions[${i}].correctAnswers[${j}]`, ans));
    });

    if (file) formData.append('thumbnail', file, file.name);

    return this.http.put<ApiResponse<string>>(`${this.apiUrl}/${id}`, formData);
  }

  generateDeck(file: File): Observable<ApiResponse<GeneratedDeck>> {
    const formData = new FormData();
    formData.append('file', file, file.name);
    return this.http.post<ApiResponse<GeneratedDeck>>(`${this.apiUrl}/generate`, formData);
  }

  invite(id: string, email: string): Observable<ApiResponse<string>> {
    return this.http.post<ApiResponse<string>>(`${this.apiUrl}/${id}/invite`, { email });
  }

  acceptInvite(token: string): Observable<ApiResponse<string>> {
    return this.http.post<ApiResponse<string>>(`${this.apiUrl}/invite/accept?token=${token}`, {});
  }
}
