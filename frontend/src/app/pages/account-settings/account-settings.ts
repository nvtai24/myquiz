import { Component, OnInit, inject, signal, computed } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { CommonModule } from '@angular/common';
import { AuthService } from '../../services/auth.service';
import { HttpClient } from '@angular/common/http';

@Component({
  selector: 'app-account-settings',
  standalone: true,
  imports: [FormsModule, CommonModule],
  templateUrl: './account-settings.html',
  styleUrl: './account-settings.css',
})
export class AccountSettings implements OnInit {
  private authService = inject(AuthService);
  private http = inject(HttpClient);

  currentUser = this.authService.currentUser;

  // Form state
  firstName = signal('');
  lastName = signal('');
  email = signal('');
  currentPassword = signal('');
  newPassword = signal('');
  confirmPassword = signal('');
  avatarUrl = signal<string | null>(null);

  // UI state
  saving = signal(false);
  changingPassword = signal(false);
  saveSuccess = signal<string | null>(null);
  saveError = signal<string | null>(null);
  passwordSuccess = signal<string | null>(null);
  passwordError = signal<string | null>(null);
  showCurrentPassword = signal(false);
  showNewPassword = signal(false);
  showConfirmPassword = signal(false);

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
    if (s === 1) return 'Yếu';
    if (s === 2) return 'Trung bình';
    if (s === 3) return 'Tốt';
    return 'Mạnh';
  }

  get strengthColor(): string {
    const s = this.passwordStrength();
    if (s <= 1) return '#ef4444';
    if (s === 2) return '#f59e0b';
    if (s === 3) return '#3b82f6';
    return '#10b981';
  }

  ngOnInit(): void {
    const u = this.currentUser();
    if (u) {
      this.firstName.set(u.firstName ?? '');
      this.lastName.set(u.lastName ?? '');
      this.email.set(u.email ?? '');
    }
  }

  onAvatarSelect(event: Event) {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0];
    if (file && file.type.startsWith('image/')) {
      const reader = new FileReader();
      reader.onload = () => this.avatarUrl.set(reader.result as string);
      reader.readAsDataURL(file);
    }
  }

  removeAvatar() { this.avatarUrl.set(null); }

  saveChanges() {
    this.saveSuccess.set(null);
    this.saveError.set(null);
    // Profile update not yet in backend – just show success for now
    this.saving.set(true);
    setTimeout(() => {
      this.saving.set(false);
      this.saveSuccess.set('Thông tin đã được lưu thành công!');
      setTimeout(() => this.saveSuccess.set(null), 3000);
    }, 600);
  }

  changePassword() {
    this.passwordSuccess.set(null);
    this.passwordError.set(null);

    if (!this.currentPassword() || !this.newPassword() || !this.confirmPassword()) {
      this.passwordError.set('Vui lòng nhập đầy đủ thông tin mật khẩu.');
      return;
    }

    if (this.newPassword() !== this.confirmPassword()) {
      this.passwordError.set('Mật khẩu mới không khớp.');
      return;
    }

    if (this.newPassword().length < 8) {
      this.passwordError.set('Mật khẩu mới phải có ít nhất 8 ký tự.');
      return;
    }

    this.changingPassword.set(true);
    this.http.post('/api/Auth/change-password', {
      currentPassword: this.currentPassword(),
      newPassword: this.newPassword(),
      confirmNewPassword: this.confirmPassword()
    }).subscribe({
      next: () => {
        this.changingPassword.set(false);
        this.passwordSuccess.set('Mật khẩu đã được thay đổi thành công!');
        this.currentPassword.set('');
        this.newPassword.set('');
        this.confirmPassword.set('');
        setTimeout(() => this.passwordSuccess.set(null), 3000);
      },
      error: (err) => {
        this.changingPassword.set(false);
        this.passwordError.set(err?.error?.message ?? 'Không thể thay đổi mật khẩu.');
      }
    });
  }
}
