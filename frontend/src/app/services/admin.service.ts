import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable, of } from 'rxjs';
import { catchError, map } from 'rxjs';
import { ApiResponse, PagedResponse } from '../models/api.models';
import { AdminSubscriptionPlanResponse, UpdatePlanRequest, CreatePlanRequest, AdminDeckSummaryResponse, AdminPaymentResponse, OverviewStatsResponse, RevenueChartResponse, PlanDistributionResponse, AdminUserResponse } from '../models/admin.models';



@Injectable({ providedIn: 'root' })
export class AdminService {
  private http = inject(HttpClient);

  /** GET /api/subscription-plans/admin — Admin only */
  getSubscriptionPlans(): Observable<ApiResponse<AdminSubscriptionPlanResponse[]>> {
    return this.http.get<ApiResponse<AdminSubscriptionPlanResponse[]>>('/api/subscription-plans/admin');
  }

  /** POST /api/subscription-plans — Admin only */
  createSubscriptionPlan(req: CreatePlanRequest): Observable<ApiResponse<void>> {
    return this.http.post<ApiResponse<void>>('/api/subscription-plans', req);
  }

  /** PUT /api/subscription-plans/:id — Admin only */
  updateSubscriptionPlan(id: string, req: UpdatePlanRequest): Observable<ApiResponse<void>> {
    return this.http.put<ApiResponse<void>>(`/api/subscription-plans/${id}`, req);
  }

  /** DELETE /api/subscription-plans/:id — Admin only */
  deleteSubscriptionPlan(id: string): Observable<ApiResponse<void>> {
    return this.http.delete<ApiResponse<void>>(`/api/subscription-plans/${id}`);
  }

  /** GET /api/Decks — get all public decks for admin overview */
  getAllDecks(): Observable<ApiResponse<AdminDeckSummaryResponse[]>> {
    return this.http.get<ApiResponse<AdminDeckSummaryResponse[]>>('/api/Decks');
  }

  /** GET /api/ping — check if backend is online */
  checkSystemStatus(): Observable<boolean> {
    return this.http.get<ApiResponse<void>>('/api/ping').pipe(
      map(res => res.success && res.statusCode === 200),
      catchError(() => of(false))
    );
  }

  /** GET /api/admin/users — Get paginated list of users */
  getUsers(page?: number, pageSize?: number, search?: string, role?: string, isBanned?: boolean | null): Observable<PagedResponse<AdminUserResponse>> {
    let params = new HttpParams();
    if (page != null) params = params.set('page', page.toString());
    if (pageSize != null) params = params.set('pageSize', pageSize.toString());
    if (search) params = params.set('search', search);
    if (role && role !== 'All') params = params.set('role', role);
    if (isBanned != null) params = params.set('isBanned', isBanned.toString());

    return this.http.get<PagedResponse<AdminUserResponse>>('/api/admin/users', { params });
  }

  /** PUT /api/admin/users/{userId}/ban — Ban or unban a user */
  updateUserBanStatus(userId: string, isBanned: boolean): Observable<ApiResponse<void>> {
    return this.http.put<ApiResponse<void>>(`/api/admin/users/${userId}/ban`, { isBanned });
  }

  /** PUT /api/admin/users/{userId}/role — Assign role */
  assignUserRole(userId: string, role: string): Observable<ApiResponse<void>> {
    return this.http.put<ApiResponse<void>>(`/api/admin/users/${userId}/role`, { role });
  }

  /** GET /api/admin/payments — Get paginated list of payments */
  getPayments(page?: number, pageSize?: number, search?: string, status?: string, fromDate?: string, toDate?: string): Observable<PagedResponse<AdminPaymentResponse>> {
    let params = new HttpParams();
    if (page != null) params = params.set('page', page.toString());
    if (pageSize != null) params = params.set('pageSize', pageSize.toString());
    if (search) params = params.set('search', search);
    if (status && status !== 'All') params = params.set('status', status);
    if (fromDate) params = params.set('fromDate', fromDate);
    if (toDate) params = params.set('toDate', toDate);

    return this.http.get<PagedResponse<AdminPaymentResponse>>('/api/admin/payments', { params });
  }

  // --- DASHBOARD CHARTS & ANALYTICS ---

  getSummaryStats(): Observable<ApiResponse<OverviewStatsResponse>> {
    return this.http.get<ApiResponse<OverviewStatsResponse>>('/api/admin/stats/summary');
  }

  getRevenueChart(days: number): Observable<ApiResponse<RevenueChartResponse[]>> {
    return this.http.get<ApiResponse<RevenueChartResponse[]>>(`/api/admin/stats/revenue-chart?days=${days}`);
  }

  getPlanDistribution(): Observable<ApiResponse<PlanDistributionResponse[]>> {
    return this.http.get<ApiResponse<PlanDistributionResponse[]>>('/api/admin/stats/plan-distribution');
  }
}
