// ── Response DTOs ──

export interface AdminSubscriptionPlanResponse {
  id: string;
  name: string;
  description: string;
  dailyGenerateLimit: number;
  maxQuestionsPerGenerate: number;
  hasExportToPdf: boolean;
  price: number;
  duration: number;
  isActive: boolean;
  order: number;
  createdAt?: string;
  updatedAt?: string | null;
}

export interface AdminDeckSummaryResponse {
  id: string;
  name: string;
  description?: string;
  questionCount: number;
  visibility: string;
  createdAt: string;
  averageRating?: number;
  totalRatings?: number;
}

export interface AdminPaymentResponse {
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

export interface AdminUserResponse {
  id: string;
  email: string;
  firstName: string;
  lastName: string;
  fullName: string;
  roles: string[];
  emailConfirmed: boolean;
  isBanned: boolean;
  createdAt: string;
  avatarUrl?: string | null;
}

export interface OverviewStatsResponse {
  totalRevenue: number;
  monthlyRevenue: number;
  revenueGrowth: number;
  newUsers: number;
  userGrowth: number;
}

export interface RevenueChartResponse {
  date: string;
  amount: number;
}

export interface PlanDistributionResponse {
  planName: string;
  userCount: number;
  percentage?: number;
}

// ── Request DTOs ──

export interface CreatePlanRequest {
  name: string;
  description: string;
  dailyGenerateLimit: number;
  maxQuestionsPerGenerate: number;
  hasExportToPdf: boolean;
  price: number;
  duration: number;
  isActive: boolean;
  order: number;
}

export interface UpdatePlanRequest {
  name: string;
  description: string;
  dailyGenerateLimit: number;
  maxQuestionsPerGenerate: number;
  hasExportToPdf: boolean;
  price: number;
  duration: number;
  isActive: boolean;
  order: number;
}
