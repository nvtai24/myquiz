import { inject, Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { ApiResponse } from '../models/api.models';
import { QuizAttemptResponse, SubmitQuizAttemptRequest } from '../models/quiz.models';

@Injectable({ providedIn: 'root' })
export class QuizService {
  private http = inject(HttpClient);
  private endpoint = '/api/QuizAttempts';

  submitAttempt(request: SubmitQuizAttemptRequest): Observable<ApiResponse<QuizAttemptResponse>> {
    return this.http.post<ApiResponse<QuizAttemptResponse>>(this.endpoint, request);
  }
}
