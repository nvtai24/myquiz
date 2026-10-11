import { Component, OnInit, inject, signal, computed } from '@angular/core';
import { RouterModule } from '@angular/router';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { HttpClient } from '@angular/common/http';
import { AuthService } from '../../services/auth.service';
import { PaymentService } from '../../services/payment.service';
import { UserSubscriptionResponse } from '../../models/payment.models';
import { ProfileResponse } from '../../models/profile.models';
import { ApiResponse } from '../../models/api.models';

@Component({
  selector: 'app-user-profile',
  standalone: true,
  imports: [RouterModule, CommonModule, FormsModule],
  templateUrl: './user-profile.html',
  styleUrl: './user-profile.css',
})
export class UserProfile implements OnInit {
  private authService = inject(AuthService);
  private paymentService = inject(PaymentService);
  private http = inject(HttpClient);

  currentUser = this.authService.currentUser;
  subscription = signal<UserSubscriptionResponse | null>(null);
  loadingSubscription = signal(true);
  subscriptionError = signal(false);

  // Profile data
  profile = signal<ProfileResponse | null>(null);
  loadingProfile = signal(true);

  // Form state
  firstName = signal('');
  lastName = signal('');
  email = signal('');
  avatarUrl = signal<string | null>(null);
  avatarFile = signal<File | null>(null);

  // Password state
  currentPassword = signal('');
  newPassword = signal('');
  confirmPassword = signal('');
  showCurrentPassword = signal(false);
  showNewPassword = signal(false);
  showConfirmPassword = signal(false);

  // UI state
  editMode = signal(false);
  saving = signal(false);
  changingPassword = signal(false);
  saveSuccess = signal<string | null>(null);
  saveError = signal<string | null>(null);
  passwordSuccess = signal<string | null>(null);
  passwordError = signal<string | null>(null);
  showPasswordSection = signal(false);

  // Password strength
  passwordStrength = computed(() => {
    const pw = this.newPassword();
    if (!pw) return 0;
    let score = 0;
    if (pw.length >= 8) score++;
    if (/[A-Z]/.test(pw)) score++;
    if (/[0-9]/.test(pw)) score++;
    if (/[^A-Za-z0-9]/.test(pw)) score++;
    return score;
  });

  get strengthLabel(): string {
    const s = this.passwordStrength();
    if (s === 0) return '';
    if (s === 1) return 'Weak';
    if (s === 2) return 'Medium';
    if (s === 3) return 'Good';
    return 'Strong';
  }

  get strengthColor(): string {
    const s = this.passwordStrength();
    if (s <= 1) return '#ef4444';
    if (s === 2) return '#f59e0b';
    if (s === 3) return '#3b82f6';
    return '#10b981';
  }

  get displayName(): string {
    const p = this.profile();
    if (p) {
      const first = p.firstName?.trim() ?? '';
      const last = p.lastName?.trim() ?? '';
      return [first, last].filter(Boolean).join(' ') || p.email;
    }
    const u = this.currentUser();
    if (!u) return '';
    const first = u.firstName?.trim() ?? '';
    const last = u.lastName?.trim() ?? '';
    return [first, last].filter(Boolean).join(' ') || u.email;
  }

  get badgeLabel(): string {
    const p = this.profile();
    const roles = p?.roles ?? this.currentUser()?.roles ?? [];

    if (roles.some(r => r.toLowerCase() === 'admin')) {
      return 'ADMIN';
    }

    if (p?.currentPlan) {
      return p.currentPlan.toUpperCase();
    }

    return 'FREE';
  }

  get isAdmin(): boolean {
    const p = this.profile();
    const roles = p?.roles ?? this.currentUser()?.roles ?? [];
    return roles.some(r => r.toLowerCase() === 'admin');
  }

  get userEmail(): string {
    return this.profile()?.email ?? this.currentUser()?.email ?? '';
  }

  ngOnInit(): void {
    this.loadProfile();

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

  loadProfile(): void {
    this.loadingProfile.set(true);
    this.http.get<ApiResponse<ProfileResponse>>('/api/profile').subscribe({
      next: (res) => {
        const data = res.data;
        if (data) {
          this.profile.set(data);
          this.firstName.set(data.firstName ?? '');
          this.lastName.set(data.lastName ?? '');
          this.email.set(data.email ?? '');
          this.avatarUrl.set(data.avatarUrl ?? null);
        }
        this.loadingProfile.set(false);
      },
      error: () => {
        this.loadingProfile.set(false);
        const u = this.currentUser();
        if (u) {
          this.firstName.set(u.firstName ?? '');
          this.lastName.set(u.lastName ?? '');
          this.email.set(u.email ?? '');
        }
      }
    });
  }

  formatDate(dateStr: string): string {
    if (!dateStr) return '';
    return new Date(dateStr).toLocaleDateString('en-US');
  }

  toggleEditMode() {
    this.editMode.update(v => !v);
    if (!this.editMode()) {
      const p = this.profile();
      if (p) {
        this.firstName.set(p.firstName ?? '');
        this.lastName.set(p.lastName ?? '');
        this.email.set(p.email ?? '');
        this.avatarUrl.set(p.avatarUrl ?? null);
        this.avatarFile.set(null);
      }
    }
  }

  onAvatarSelect(event: Event) {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0];
    if (file && file.type.startsWith('image/')) {
      this.avatarFile.set(file);
      const reader = new FileReader();
      reader.onload = () => this.avatarUrl.set(reader.result as string);
      reader.readAsDataURL(file);
    }
  }

  removeAvatar() {
    this.avatarUrl.set(null);
    this.avatarFile.set(null);
  }

  saveChanges() {
    this.saveSuccess.set(null);
    this.saveError.set(null);
    this.saving.set(true);

    const formData = new FormData();
    formData.append('FirstName', this.firstName());
    formData.append('LastName', this.lastName());

    const avatarFile = this.avatarFile();
    if (avatarFile) {
      formData.append('avatar', avatarFile);
    }

    this.http.put<ApiResponse<ProfileResponse>>('/api/profile', formData).subscribe({
      next: (res) => {
        if (res.data) {
          this.profile.set(res.data);
          this.avatarUrl.set(res.data.avatarUrl ?? null);
          
          this.authService.updateUser({
            firstName: res.data.firstName,
            lastName: res.data.lastName,
            avatarUrl: res.data.avatarUrl ?? undefined,
          });
        }
        this.avatarFile.set(null);
        this.saving.set(false);
        this.saveSuccess.set('Profile updated successfully!');
        this.editMode.set(false);
        setTimeout(() => this.saveSuccess.set(null), 3000);
      },
      error: (err) => {
        this.saving.set(false);
        this.saveError.set(err?.error?.message ?? 'Failed to update profile.');
        setTimeout(() => this.saveError.set(null), 3000);
      }
    });
  }

  togglePasswordSection() {
    this.showPasswordSection.update(v => !v);
    if (!this.showPasswordSection()) {
      this.currentPassword.set('');
      this.newPassword.set('');
      this.confirmPassword.set('');
      this.passwordError.set(null);
      this.passwordSuccess.set(null);
    }
  }

  changePassword() {
    this.passwordSuccess.set(null);
    this.passwordError.set(null);

    if (!this.currentPassword() || !this.newPassword() || !this.confirmPassword()) {
      this.passwordError.set('Please fill in all password fields.');
      return;
    }

    if (this.newPassword() !== this.confirmPassword()) {
      this.passwordError.set('New passwords do not match.');
      return;
    }

    if (this.newPassword().length < 8) {
      this.passwordError.set('New password must be at least 8 characters.');
      return;
    }

    this.changingPassword.set(true);
    this.http.post('/api/Auth/change-password', {
      currentPassword: this.currentPassword(),
      newPassword: this.newPassword()
    }).subscribe({
      next: () => {
        this.changingPassword.set(false);
        this.passwordSuccess.set('Password changed successfully!');
        this.currentPassword.set('');
        this.newPassword.set('');
        this.confirmPassword.set('');
        setTimeout(() => {
          this.passwordSuccess.set(null);
          this.showPasswordSection.set(false);
        }, 3000);
      },
      error: (err) => {
        this.changingPassword.set(false);
        this.passwordError.set(err?.error?.message ?? 'Failed to change password.');
      }
    });
  }

  // Forgot password state
  sendingForgotPassword = signal(false);
  forgotPasswordSuccess = signal<string | null>(null);
  forgotPasswordError = signal<string | null>(null);

  forgotPassword() {
    const email = this.userEmail;
    if (!email) {
      this.forgotPasswordError.set('Email not found.');
      return;
    }

    this.sendingForgotPassword.set(true);
    this.forgotPasswordSuccess.set(null);
    this.forgotPasswordError.set(null);

    this.http.post('/api/Auth/forgot-password', { email }).subscribe({
      next: () => {
        this.sendingForgotPassword.set(false);
        this.forgotPasswordSuccess.set('Password reset email sent. Please check your inbox.');
        setTimeout(() => this.forgotPasswordSuccess.set(null), 5000);
      },
      error: (err) => {
        this.sendingForgotPassword.set(false);
        this.forgotPasswordError.set(err?.error?.message ?? 'Failed to send password reset email.');
        setTimeout(() => this.forgotPasswordError.set(null), 5000);
      }
    });
  }
}
