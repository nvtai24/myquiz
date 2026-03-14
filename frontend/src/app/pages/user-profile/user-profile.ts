import { Component, OnInit, inject, signal } from '@angular/core';
import { RouterModule } from '@angular/router';
import { CommonModule } from '@angular/common';
import { AuthService } from '../../services/auth.service';
import { PaymentService } from '../../services/payment.service';
import { UserSubscriptionResponse } from '../../models/payment.models';

@Component({
  selector: 'app-user-profile',
  standalone: true,
  imports: [RouterModule, CommonModule],
  templateUrl: './user-profile.html',
  styleUrl: './user-profile.css',
})
export class UserProfile implements OnInit {
  private authService = inject(AuthService);
  private paymentService = inject(PaymentService);

  currentUser = this.authService.currentUser;
  subscription = signal<UserSubscriptionResponse | null>(null);
  loadingSubscription = signal(true);
  subscriptionError = signal(false);

  get displayName(): string {
    const u = this.currentUser();
    if (!u) return '';
    const first = u.firstName?.trim() ?? '';
    const last = u.lastName?.trim() ?? '';
    return [first, last].filter(Boolean).join(' ') || u.email;
  }

  get primaryRole(): string {
    const u = this.currentUser();
    if (!u || !u.roles?.length) return 'USER';
    return u.roles[0].toUpperCase();
  }

  ngOnInit(): void {
    this.paymentService.getMySubscription().subscribe({
      next: (data) => {
        this.subscription.set(data);
        this.loadingSubscription.set(false);
      },
      error: () => {
        this.loadingSubscription.set(false);
        this.subscriptionError.set(true);
      }
    });
  }

  formatDate(dateStr: string): string {
    if (!dateStr) return '';
    return new Date(dateStr).toLocaleDateString('vi-VN');
  }
}
