import { CommonModule, isPlatformBrowser } from '@angular/common';
import { AfterViewInit, Component, ElementRef, inject, OnDestroy, PLATFORM_ID, signal, ViewChild } from '@angular/core';
import { AbstractControl, FormControl, FormGroup, ReactiveFormsModule, ValidationErrors, ValidatorFn, Validators } from '@angular/forms';
import { AuthService } from '../../services/auth.service';
import { GoogleAuthService } from '../../services/google-auth.service';
import { Router, RouterModule } from '@angular/router';
import { finalize, Subscription } from 'rxjs';

// Custom validator: kiểm tra confirmPassword khớp password
export const passwordMatchValidator: ValidatorFn = (group: AbstractControl): ValidationErrors | null => {
  const pass = group.get('password')?.value;
  const confirm = group.get('confirmPassword')?.value;
  return pass && confirm && pass !== confirm ? { passwordMismatch: true } : null;
};

// Custom validator: mật khẩu mạnh (chữ hoa + ký tự đặc biệt) — khớp với backend
export const strongPasswordValidator: ValidatorFn = (control: AbstractControl): ValidationErrors | null => {
  const value: string = control.value ?? '';
  if (!value) return null; // required validator sẽ handle
  const hasUpperCase = /[A-Z]/.test(value);
  const hasSpecialChar = /[^a-zA-Z0-9]/.test(value);
  if (!hasUpperCase) return { missingUpperCase: true };
  if (!hasSpecialChar) return { missingSpecialChar: true };
  return null;
};

@Component({
  selector: 'app-register',
  imports: [ReactiveFormsModule, CommonModule, RouterModule],
  templateUrl: './register.html',
  styleUrl: './register.css',
})
export class register implements AfterViewInit, OnDestroy {
  private authService = inject(AuthService);
  private router = inject(Router);
  private googleAuthService = inject(GoogleAuthService);
  private platformId = inject(PLATFORM_ID);
  private valueChangesSub?: Subscription;
  private errorTimer?: ReturnType<typeof setTimeout>;

  @ViewChild('googleBtnContainer') googleBtnContainer!: ElementRef<HTMLDivElement>;

  ngAfterViewInit() {
    if (isPlatformBrowser(this.platformId) && this.googleBtnContainer) {
      this.googleAuthService.renderButton(
        this.googleBtnContainer.nativeElement,
        (idToken) => this.handleGoogleLogin(idToken)
      );
    }
  }

  ngOnDestroy() {
    this.valueChangesSub?.unsubscribe();
    clearTimeout(this.errorTimer);
  }

  handleGoogleLogin(idToken: string) {
    this.isLoading.set(true);
    this.errorMessage.set(null);

    this.authService.loginWithGoogle(idToken)
      .pipe(finalize(() => this.isLoading.set(false)))
      .subscribe({
        next: () => {
          this.router.navigate(['/dashboard']);
        },
        error: (err) => {
          this.errorMessage.set(err.error?.message || 'Google Sign-In failed. Please try again.');
          setTimeout(() => this.errorMessage.set(null), 5000);
        }
      });
  }

  showPassword = signal(false);
  showConfirmPassword = signal(false);
  errorMessage = signal<string | null>(null);
  isLoading = signal(false);

  // Kiểm soát hiển thị lỗi validation (tự ẩn sau 3s + ẩn khi user gõ)
  showErrors = signal(false);

  // Overlay sau khi đăng ký thành công
  showVerifyOverlay = signal(false);
  registeredEmail = signal('');

  registerForm = new FormGroup({
    firstName: new FormControl('', {
      nonNullable: true,
      validators: [Validators.required]
    }),
    lastName: new FormControl('', {
      nonNullable: true,
      validators: [Validators.required]
    }),
    email: new FormControl('', {
      nonNullable: true,
      validators: [Validators.required, Validators.email]
    }),
    password: new FormControl('', {
      nonNullable: true,
      validators: [Validators.required, Validators.minLength(8), strongPasswordValidator]
    }),
    confirmPassword: new FormControl('', {
      nonNullable: true,
      validators: [Validators.required]
    }),
  }, { validators: passwordMatchValidator });

  private showValidationErrors() {
    this.showErrors.set(true);

    // Tự ẩn sau 3 giây
    clearTimeout(this.errorTimer);
    this.errorTimer = setTimeout(() => this.showErrors.set(false), 3000);

    // Ẩn ngay khi user bắt đầu gõ vào bất kỳ ô nào
    this.valueChangesSub?.unsubscribe();
    this.valueChangesSub = this.registerForm.valueChanges.subscribe(() => {
      this.showErrors.set(false);
      this.valueChangesSub?.unsubscribe();
      clearTimeout(this.errorTimer);
    });
  }

  togglePassword() {
    this.showPassword.update(v => !v);
  }

  toggleConfirmPassword() {
    this.showConfirmPassword.update(v => !v);
  }

  onSubmit() {
    if (this.registerForm.invalid) {
      this.registerForm.markAllAsTouched();
      this.showValidationErrors();
      return;
    }

    this.isLoading.set(true);
    this.errorMessage.set(null);

    const { firstName, lastName, email, password } = this.registerForm.getRawValue();

    this.authService.register({ firstName, lastName, email, password })
      .pipe(finalize(() => this.isLoading.set(false)))
      .subscribe({
        next: (response) => {
          if (response.success) {
            this.registeredEmail.set(email);
            this.showVerifyOverlay.set(true);
          } else {
            this.errorMessage.set(response.message || 'Đăng ký thất bại. Vui lòng thử lại.');
            setTimeout(() => this.errorMessage.set(null), 5000);
          }
        },
        error: (err) => {
          this.errorMessage.set(err.error?.message || 'Đăng ký thất bại. Vui lòng kiểm tra lại thông tin.');
          setTimeout(() => this.errorMessage.set(null), 5000);
        },
      });
  }

  goToLogin() {
    this.router.navigate(['/login']);
  }
}
