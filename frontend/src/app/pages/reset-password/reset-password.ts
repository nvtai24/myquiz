import { CommonModule } from '@angular/common';
import { Component, inject, OnInit, signal } from '@angular/core';
import { AbstractControl, FormControl, FormGroup, ReactiveFormsModule, ValidationErrors, ValidatorFn, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterModule } from '@angular/router';
import { AuthService } from '../../services/auth.service';
import { finalize } from 'rxjs';
import { strongPasswordValidator } from '../register/register';

// Validator: 2 password phải khớp
export const newPasswordMatchValidator: ValidatorFn = (group: AbstractControl): ValidationErrors | null => {
  const pass = group.get('newPassword')?.value;
  const confirm = group.get('confirmPassword')?.value;
  return pass && confirm && pass !== confirm ? { passwordMismatch: true } : null;
};

@Component({
  selector: 'app-reset-password',
  imports: [ReactiveFormsModule, CommonModule, RouterModule],
  templateUrl: './reset-password.html',
  styleUrl: './reset-password.css',
})
export class ResetPassword implements OnInit {
  private authService = inject(AuthService);
  private router = inject(Router);
  private route = inject(ActivatedRoute);

  isLoading = signal(false);
  showErrors = signal(false);
  showPassword = signal(false);
  showConfirmPassword = signal(false);
  pageState = signal<'form' | 'success' | 'invalid'>('form');
  errorMessage = signal<string | null>(null);

  private email = '';
  private token = '';

  resetForm = new FormGroup({
    newPassword: new FormControl('', {
      nonNullable: true,
      validators: [Validators.required, Validators.minLength(8), strongPasswordValidator]
    }),
    confirmPassword: new FormControl('', {
      nonNullable: true,
      validators: [Validators.required]
    }),
  }, { validators: newPasswordMatchValidator });

  ngOnInit() {
    // Giống verify-email.ts: Angular queryParamMap tự decode %2B→+ đúng rồi
    this.email = this.route.snapshot.queryParamMap.get('email') ?? '';
    this.token = this.route.snapshot.queryParamMap.get('token') ?? '';

    if (!this.email || !this.token) {
      this.pageState.set('invalid');
    }
  }

  togglePassword() { this.showPassword.update(v => !v); }
  toggleConfirmPassword() { this.showConfirmPassword.update(v => !v); }

  onSubmit() {
    if (this.resetForm.invalid) {
      this.resetForm.markAllAsTouched();
      this.showErrors.set(true);
      setTimeout(() => this.showErrors.set(false), 3000);
      return;
    }

    this.isLoading.set(true);
    this.errorMessage.set(null);
    const { newPassword } = this.resetForm.getRawValue();

    this.authService.resetPassword({ email: this.email, token: this.token, newPassword })
      .pipe(finalize(() => this.isLoading.set(false)))
      .subscribe({
        next: (res) => {
          if (res.success) {
            this.pageState.set('success');
          } else {
            this.errorMessage.set(res.message || 'Reset failed. Please try again.');
          }
        },
        error: (err) => {
          this.errorMessage.set(err.error?.message || 'Reset failed. The link may be expired.');
          setTimeout(() => this.errorMessage.set(null), 5000);
        },
      });
  }

  goToLogin() {
    this.router.navigate(['/login']);
  }
}
