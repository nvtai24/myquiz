export interface SubscriptionPlanResponse {
  id: string;
  name: string;
  description: string;
  dailyGenerateLimit: number;
  numDeckLimit: number;
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
  numDeckLimit: number;
  price: number;
  startDate: string;
  endDate: string;
  isExpired: boolean;
  currentGenerateCount: number;
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

export interface ApiResponse<T> {
  success: boolean;
  statusCode: number;
  message: string;
  data: T;
}
