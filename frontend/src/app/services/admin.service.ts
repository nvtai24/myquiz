import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable, of } from 'rxjs';
import { catchError, map } from 'rxjs';

export interface AdminSubscriptionPlan {
  id: string;
  name: string;
  description: string;
  dailyGenerateLimit: number;
  numDeckLimit: number;
  price: number;
  duration: number;
  isActive: boolean;
  order: number;
  createdAt?: string;
  updatedAt?: string | null;
}

export interface UpdatePlanRequest {
  name: string;
  description: string;
  dailyGenerateLimit: number;
  numDeckLimit: number;
  price: number;
  duration: number;
  isActive: boolean;
  order: number;
}

export interface CreatePlanRequest extends UpdatePlanRequest {}

export interface AdminDeckSummary {
  id: string;
  name: string;
  description?: string;
  questionCount: number;
  visibility: string;
  createdAt: string;
  averageRating?: number;
  totalRatings?: number;
}

export interface PaginationMeta {
  currentPage: number;
  pageSize: number;
  totalRecords: number;
  totalPages: number;
  hasPrevious: boolean;
  hasNext: boolean;
}

export interface AdminPayment {
  id: string;
  userId: string;
  userEmail: string;
  userFullName: string;
  planName: string;
  orderCode: string;
  amount: number;
  status: string;
  content: string;
  createdAt: string;
  completedAt: string | null;
}

export interface AdminPaymentsResponse {
  success: boolean;
  statusCode: number;
  message: string;
  pagination: PaginationMeta;
  data: AdminPayment[];
}

export interface AdminUser {
  id: string;
  email: string;
  firstName: string;
  lastName: string;
  fullName: string;
  roles: string[];
  emailConfirmed: boolean;
  isBanned: boolean;
  createdAt: string;
}

export interface AdminUsersResponse {
  success: boolean;
  statusCode: number;
  message: string;
  pagination: PaginationMeta;
  data: AdminUser[];
}

@Injectable({ providedIn: 'root' })
export class AdminService {
  private http = inject(HttpClient);

  /** GET /api/subscription-plans/admin — Admin only */
  getSubscriptionPlans(): Observable<{ success: boolean; data: AdminSubscriptionPlan[] }> {
    return this.http.get<{ success: boolean; data: AdminSubscriptionPlan[] }>('/api/subscription-plans/admin');
  }

  /** POST /api/subscription-plans — Admin only */
  createSubscriptionPlan(req: CreatePlanRequest): Observable<any> {
    return this.http.post<any>('/api/subscription-plans', req);
  }

  /** PUT /api/subscription-plans/:id — Admin only */
  updateSubscriptionPlan(id: string, req: UpdatePlanRequest): Observable<any> {
    return this.http.put<any>(`/api/subscription-plans/${id}`, req);
  }

  /** DELETE /api/subscription-plans/:id — Admin only */
  deleteSubscriptionPlan(id: string): Observable<any> {
    return this.http.delete<any>(`/api/subscription-plans/${id}`);
  }

  /** GET /api/Decks — get all public decks for admin overview */
  getAllDecks(): Observable<{ success: boolean; data: AdminDeckSummary[] }> {
    return this.http.get<{ success: boolean; data: AdminDeckSummary[] }>('/api/Decks');
  }

  /** GET /api/ping — check if backend is online */
  checkSystemStatus(): Observable<boolean> {
    return this.http.get<{ success: boolean; statusCode: number }>('/api/ping').pipe(
      map(res => res.success && res.statusCode === 200),
      catchError(() => of(false))
    );
  }

  /** GET /api/admin/users — Get paginated list of users */
  getUsers(page?: number, pageSize?: number, search?: string, role?: string, isBanned?: boolean | null): Observable<AdminUsersResponse> {
    let params = new HttpParams();
    if (page != null) params = params.set('page', page.toString());
    if (pageSize != null) params = params.set('pageSize', pageSize.toString());
    if (search) params = params.set('search', search);
    if (role && role !== 'All') params = params.set('role', role);
    if (isBanned != null) params = params.set('isBanned', isBanned.toString());

    return this.http.get<AdminUsersResponse>('/api/admin/users', { params });
  }

  /** PUT /api/admin/users/{userId}/ban — Ban or unban a user */
  updateUserBanStatus(userId: string, isBanned: boolean): Observable<any> {
    return this.http.put(`/api/admin/users/${userId}/ban`, { isBanned });
  }

  /** PUT /api/admin/users/{userId}/role — Assign role */
  assignUserRole(userId: string, role: string): Observable<any> {
    return this.http.put(`/api/admin/users/${userId}/role`, { role });
  }

  /** GET /api/admin/payments — Get paginated list of payments */
  getPayments(page?: number, pageSize?: number, search?: string, status?: string, fromDate?: string, toDate?: string): Observable<AdminPaymentsResponse> {
    let params = new HttpParams();
    if (page != null) params = params.set('page', page.toString());
    if (pageSize != null) params = params.set('pageSize', pageSize.toString());
    if (search) params = params.set('search', search);
    if (status && status !== 'All') params = params.set('status', status);
    if (fromDate) params = params.set('fromDate', fromDate);
    if (toDate) params = params.set('toDate', toDate);

    return this.http.get<AdminPaymentsResponse>('/api/admin/payments', { params });
  }
}
