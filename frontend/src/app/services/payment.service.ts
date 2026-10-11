import { inject, Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { map } from 'rxjs/operators';
import { ApiResponse } from '../models/api.models';
import {
  SubscriptionPlanResponse,
  UserSubscriptionResponse,
  CreatePaymentOrderRequest,
  PaymentOrderResponse
} from '../models/payment.models';
import { LimitAiGenerateResponse } from '../models/deck.models';

@Injectable({
  providedIn: 'root'
})
export class PaymentService {
  private http = inject(HttpClient);
  private baseUrl = '/api';

  getSubscriptionPlans(): Observable<SubscriptionPlanResponse[]> {
    return this.http
      .get<ApiResponse<SubscriptionPlanResponse[]>>(`${this.baseUrl}/subscription-plans`)
      .pipe(map(res => res.data!));
  }

  getMySubscription(): Observable<UserSubscriptionResponse | null> {
    return this.http
      .get<ApiResponse<UserSubscriptionResponse>>(`${this.baseUrl}/payments/my-subscription`)
      .pipe(map(res => res.data ?? null));
  }
  
  getMySubscriptionLimit(): Observable<LimitAiGenerateResponse | null> {
    return this.http
      .get<ApiResponse<LimitAiGenerateResponse>>(`${this.baseUrl}/Quota`)
      .pipe(map(res => res.data ?? null));
  } 

  createOrder(planId: string): Observable<PaymentOrderResponse> {
    const body: CreatePaymentOrderRequest = { subscriptionPlanId: planId };
    return this.http
      .post<ApiResponse<PaymentOrderResponse>>(`${this.baseUrl}/payments/create-order`, body)
      .pipe(map(res => res.data!));
  }
}
