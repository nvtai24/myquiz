import { Component, OnInit, OnDestroy, inject, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { PaymentService } from '../../services/payment.service';
import { SubscriptionPlanResponse, UserSubscriptionResponse, PaymentOrderResponse } from '../../models/payment.models';
import { forkJoin } from 'rxjs';

@Component({
  selector: 'app-subscription-settings',
  standalone: true,
  imports: [CommonModule, RouterModule],
  templateUrl: './subscription-settings.html',
  styleUrl: './subscription-settings.css',
})
export class SubscriptionSettings implements OnInit, OnDestroy {
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

  // Countdown
  countdown = signal<number>(15 * 60);
  private countdownInterval: ReturnType<typeof setInterval> | null = null;

  countdownDisplay = computed(() => {
    const s = this.countdown();
    const m = Math.floor(s / 60).toString().padStart(2, '0');
    const sec = (s % 60).toString().padStart(2, '0');
    return `${m}:${sec}`;
  });

  isUrgent = computed(() => this.countdown() <= 60);

  ngOnInit(): void {
    forkJoin({
      plans: this.paymentService.getSubscriptionPlans(),
      mySubscription: this.paymentService.getMySubscription()
    }).subscribe({
      next: ({ plans, mySubscription }) => {
        this.mySubscription.set(mySubscription);
        this.loadingPlans.set(false);
        this.loadingSubscription.set(false);
        const sorted = plans.sort((a, b) => a.order - b.order);
        const currentOrder = (mySubscription?.isExpired || !mySubscription)
          ? 0
          : sorted.find(plan => plan.name === mySubscription?.planName)?.order || 0;
        this.plans.set(sorted.filter(plan => plan.order >= currentOrder));
      },
      error: () => {
        this.loadingPlans.set(false);
        this.loadingSubscription.set(false);
        this.plansError.set(true);
      }
    });
  }

  ngOnDestroy(): void {
    this.clearCountdown();
  }

  private startCountdown(): void {
    this.countdown.set(15 * 60);
    this.clearCountdown();

    this.countdownInterval = setInterval(() => {
      this.countdown.update(v => {
        if (v <= 1) {
          this.clearCountdown();
          this.closeModal();
          return 0;
        }
        return v - 1;
      });
    }, 1000);
  }

  private clearCountdown(): void {
    if (this.countdownInterval) {
      clearInterval(this.countdownInterval);
      this.countdownInterval = null;
    }
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
        this.startCountdown(); // ← bắt đầu đếm ngược sau khi có order
      },
      error: (err) => {
        this.creatingOrder.set(false);
        this.orderError.set(err?.error?.message ?? 'Không thể tạo lệnh thanh toán. Vui lòng thử lại.');
      }
    });
  }

  closeModal(): void {
    this.clearCountdown(); // ← dọn interval trước khi đóng
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
    if (plan.price === 0) return 'Free';
    return plan.price.toLocaleString('vi-VN') + ' đ';
  }
}