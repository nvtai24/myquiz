import { Component, inject, signal, OnInit } from '@angular/core';
import { ActivatedRoute, Router, RouterModule } from '@angular/router';
import { AuthService } from '../../services/auth.service';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-verify-email',
  imports: [CommonModule, RouterModule],
  templateUrl: './verify-email.html',
  styleUrl: './verify-email.css',
})
export class VerifyEmail implements OnInit {
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private authService = inject(AuthService);

  email = signal('');
  isLoading = signal(false);
  errorMessage = signal<string | null>(null);

  // 'pending' = đang chờ người dùng vào email bấm link
  // 'verifying' = đang gọi API xác thực
  // 'success' = xác thực thành công
  // 'failed' = xác thực thất bại
  pageState = signal<'pending' | 'verifying' | 'success' | 'failed'>('pending');

  ngOnInit() {
    const emailParam = this.route.snapshot.queryParamMap.get('email');
    if (emailParam) {
      this.email.set(emailParam);
    }

    // Nếu có userId + token trong URL (người dùng click link từ email)
    const userId = this.route.snapshot.queryParamMap.get('userId');
    const token = this.route.snapshot.queryParamMap.get('token');

    if (userId && token) {
      this.confirmEmail(userId, token);
    }
    // Nếu không có thì ở trạng thái 'pending' (chờ bấm link)
  }

  private confirmEmail(userId: string, token: string) {
    this.pageState.set('verifying');
    this.isLoading.set(true);

    this.authService.confirmEmail({ userId, token }).subscribe({
      next: (response) => {
        this.isLoading.set(false);
        if (response.success) {
          this.pageState.set('success');
        } else {
          this.pageState.set('failed');
          this.errorMessage.set(response.message || 'Verification failed. Please try again.');
        }
      },
      error: (err) => {
        this.isLoading.set(false);
        this.pageState.set('failed');
        this.errorMessage.set(err.error?.message || 'Xác thực thất bại. Vui lòng kiểm tra lại mã xác thực.');
      }
    });
  }

  goToLogin() {
    this.router.navigate(['/login']);
  }

  goToRegister() {
    this.router.navigate(['/register']);
  }
}
