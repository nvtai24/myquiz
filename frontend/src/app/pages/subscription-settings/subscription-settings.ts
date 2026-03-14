import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { PaymentService } from '../../services/payment.service';
import { SubscriptionPlanResponse, UserSubscriptionResponse, PaymentOrderResponse } from '../../models/payment.models';

@Component({
  selector: 'app-subscription-settings',
  standalone: true,
  imports: [CommonModule, RouterModule],
  templateUrl: './subscription-settings.html',
  styleUrl: './subscription-settings.css',
})
export class SubscriptionSettings implements OnInit {
  private paymentService = inject(PaymentService);

  plans = signal<SubscriptionPlanResponse[]>([]);
  mySubscription = signal<UserSubscriptionResponse | null>(null);

  loadingPlans = signal(true);
  loadingSubscription = signal(true);
  plansError = signal(false);

  // Payment modal state
  showModal = signal(false);
  paymentOrder = signal<PaymentOrderResponse | null>(null);
  creatingOrder = signal(false);
  orderError = signal<string | null>(null);
  copySuccess = signal(false);

  ngOnInit(): void {
    this.paymentService.getSubscriptionPlans().subscribe({
      next: (data) => {
        this.plans.set(data.sort((a, b) => a.order - b.order));
        this.loadingPlans.set(false);
      },
      error: () => {
        this.loadingPlans.set(false);
        this.plansError.set(true);
      }
    });

    this.paymentService.getMySubscription().subscribe({
      next: (data) => {
        this.mySubscription.set(data);
        this.loadingSubscription.set(false);
      },
      error: () => {
        this.loadingSubscription.set(false);
      }
    });
  }

  isCurrentPlan(plan: SubscriptionPlanResponse): boolean {
    const sub = this.mySubscription();
    if (!sub || sub.isExpired) return false;
    return sub.subscriptionPlanId === plan.id;
  }

  isFree(plan: SubscriptionPlanResponse): boolean {
    return plan.price === 0;
  }

  selectPlan(plan: SubscriptionPlanResponse): void {
    if (this.isCurrentPlan(plan) || this.isFree(plan)) return;
    this.creatingOrder.set(true);
    this.orderError.set(null);
    this.paymentOrder.set(null);
    this.showModal.set(true);

    this.paymentService.createOrder(plan.id).subscribe({
      next: (order) => {
        this.paymentOrder.set(order);
        this.creatingOrder.set(false);
      },
      error: (err) => {
        this.creatingOrder.set(false);
        this.orderError.set(err?.error?.message ?? 'Không thể tạo lệnh thanh toán. Vui lòng thử lại.');
      }
    });
  }

  closeModal(): void {
    this.showModal.set(false);
    this.paymentOrder.set(null);
    this.orderError.set(null);
    this.creatingOrder.set(false);
    this.copySuccess.set(false);
  }

  copyToClipboard(text: string): void {
    navigator.clipboard.writeText(text).then(() => {
      this.copySuccess.set(true);
      setTimeout(() => this.copySuccess.set(false), 2000);
    });
  }

  formatDate(dateStr: string): string {
    if (!dateStr) return '';
    return new Date(dateStr).toLocaleDateString('vi-VN');
  }

  formatDatetime(dateStr: string): string {
    if (!dateStr) return '';
    return new Date(dateStr).toLocaleString('vi-VN');
  }

  formatCurrency(amount: number): string {
    return amount.toLocaleString('vi-VN') + ' đ';
  }

  formatPlanPrice(plan: SubscriptionPlanResponse): string {
    if (plan.price === 0) return 'Miễn phí';
    return plan.price.toLocaleString('vi-VN') + ' đ';
  }
}
