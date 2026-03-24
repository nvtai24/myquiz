import { inject, Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { ApiResponse, PagedResponse } from '../models/api.models';
import {
  CreateDeckRequest,
  CreateDeckRatingRequest,
  DeckDetailResponse,
  DeckMemberResponse,
  DeckRatingCheckResponse,
  DeckRatingSummaryResponse,
  DeckSummaryResponse,
  GeneratedDeckResponse,
  UpdateDeckRequest
} from '../models/deck.models';

@Injectable({
  providedIn: 'root',
})
export class DeckService {
  private http = inject(HttpClient);
  private endpoint = '/api/Decks';

  getUserDecks(): Observable<ApiResponse<DeckSummaryResponse[]>> {
    return this.http.get<ApiResponse<DeckSummaryResponse[]>>(this.endpoint);
  }

  getMyDecks(page = 1, size = 6): Observable<PagedResponse<DeckSummaryResponse>> {
    const params = new HttpParams().set('page', page).set('size', size);
    return this.http.get<PagedResponse<DeckSummaryResponse>>(`${this.endpoint}/my`, { params });
  }

  getDraftDecks(page = 1, size = 6): Observable<PagedResponse<DeckSummaryResponse>> {
    const params = new HttpParams().set('page', page).set('size', size);
    return this.http.get<PagedResponse<DeckSummaryResponse>>(`${this.endpoint}/drafts`, { params });
  }

  getSharedDecks(page = 1, size = 6): Observable<PagedResponse<DeckSummaryResponse>> {
    const params = new HttpParams().set('page', page).set('size', size);
    return this.http.get<PagedResponse<DeckSummaryResponse>>(`${this.endpoint}/shared`, { params });
  }

  getDeckById(id: string): Observable<ApiResponse<DeckDetailResponse>> {
    return this.http.get<ApiResponse<DeckDetailResponse>>(`${this.endpoint}/${id}`);
  }

  getRatings(deckId: string): Observable<ApiResponse<DeckRatingSummaryResponse>> {
    return this.http.get<ApiResponse<DeckRatingSummaryResponse>>(`${this.endpoint}/${deckId}/ratings`);
  }

  submitRating(deckId: string, request: CreateDeckRatingRequest): Observable<ApiResponse<string>> {
    return this.http.post<ApiResponse<string>>(`${this.endpoint}/${deckId}/ratings`, request);
  }

  checkRating(deckId: string): Observable<ApiResponse<DeckRatingCheckResponse>> {
    return this.http.get<ApiResponse<DeckRatingCheckResponse>>(`${this.endpoint}/${deckId}/ratings/check`);
  }

  uploadFile(file: File): Observable<ApiResponse<{ url: string }>> {
    const formData = new FormData();
    formData.append('file', file, file.name);
    return this.http.post<ApiResponse<{ url: string }>>('/api/Files/upload', formData);
  }

  createDeck(request: CreateDeckRequest): Observable<ApiResponse<string>> {
    return this.http.post<ApiResponse<string>>(this.endpoint, this.mapRequestToDto(request));
  }

  updateDeck(id: string, request: UpdateDeckRequest): Observable<ApiResponse<string>> {
    const visibilityMap: Record<string, number> = { 'Public': 0, 'Private': 1, 'Shared': 2 };
    const statusMap: Record<string, number> = { 'Draft': 0, 'Published': 1 };
    const body = {
      ...request,
      visibility: visibilityMap[request.visibility] ?? 0,
      status: statusMap[request.status] ?? 0,
    };
    return this.http.put<ApiResponse<string>>(`${this.endpoint}/${id}`, body);
  }

  private mapRequestToDto(request: CreateDeckRequest) {
    const visibilityMap: Record<string, number> = { 'Public': 0, 'Private': 1, 'Shared': 2 };
    const statusMap: Record<string, number> = { 'Draft': 0, 'Published': 1 };
    const sourceMap: Record<string, number> = { 'Manual': 0, 'AiGenerated': 1 };

    return {
      ...request,
      visibility: visibilityMap[request.visibility] ?? 0,
      status: statusMap[request.status] ?? 0,
      source: sourceMap[request.source] ?? 0
    };
  }

  generateDeck(file: File): Observable<ApiResponse<GeneratedDeckResponse>> {
    const formData = new FormData();
    formData.append('file', file, file.name);
    return this.http.post<ApiResponse<GeneratedDeckResponse>>(`${this.endpoint}/generate`, formData);
  }

  deleteDeck(id: string): Observable<ApiResponse<void>> {
    return this.http.delete<ApiResponse<void>>(`${this.endpoint}/${id}`);
  }

  invite(id: string, email: string): Observable<ApiResponse<string>> {
    return this.http.post<ApiResponse<string>>(`${this.endpoint}/${id}/invite`, { email });
  }

  getMembers(deckId: string): Observable<ApiResponse<DeckMemberResponse[]>> {
    return this.http.get<ApiResponse<DeckMemberResponse[]>>(`${this.endpoint}/${deckId}/members`);
  }

  acceptInvite(token: string): Observable<ApiResponse<string>> {
    return this.http.post<ApiResponse<string>>(`${this.endpoint}/invite/accept?token=${token}`, {});
  }

  searchDecks(searchTerm: string, page: number = 1, size: number = 12): Observable<PagedResponse<DeckSummaryResponse>> {
    return this.http.get<PagedResponse<DeckSummaryResponse>>(`${this.endpoint}/search`, {
      params: { searchTerm, page: page.toString(), size: size.toString() }
    });
  }
}
