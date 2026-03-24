export interface SubscriptionPlanResponse {
  id: string;
  name: string;
  description: string;
  dailyGenerateLimit: number;
  maxQuestionsPerGenerate: number;
  hasExportToPdf: boolean;
  price: number;
  duration: number; // days
  isActive: boolean;
  order: number;
}

export interface UserSubscriptionResponse {
  subscriptionPlanId: string;
  planName: string;
  planDescription: string;
  dailyGenerateLimit: number;
  maxQuestionsPerGenerate: number;
  price: number;
  startDate: string;
  endDate: string;
  isExpired: boolean;
}

export interface CreatePaymentOrderRequest {
  subscriptionPlanId: string;
}

export interface PaymentOrderResponse {
  paymentTransactionId: string;
  orderCode: string;
  amount: number;
  bankAccount: string;
  accountName: string;
  bankCode: string;
  qrCodeUrl: string;
  transferContent: string;
  planName: string;
  createdAt: string;
  expiresAt: string;
}

