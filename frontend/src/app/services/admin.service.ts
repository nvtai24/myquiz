import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable, of } from 'rxjs';
import { catchError, map } from 'rxjs';

export interface AdminSubscriptionPlan {
  id: string;
  name: string;
  price: number;
  billingCycle: string;
  features: string[];
  activeUsers?: number;
  isActive: boolean;
  isDefault: boolean;
}

export interface UpdatePlanRequest {
  name: string;
  price: number;
  billingCycle: string;
  features: string[];
  isActive: boolean;
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
}
