import { CommonModule, isPlatformBrowser } from '@angular/common';
import { AfterViewInit, Component, ElementRef, inject, OnDestroy, OnInit, PLATFORM_ID, signal, ViewChild } from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { AuthService } from '../../services/auth.service';
import { GoogleAuthService } from '../../services/google-auth.service';
import { ActivatedRoute, Router, RouterModule } from '@angular/router';
import { finalize, Subscription } from 'rxjs';

@Component({
  selector: 'app-login',
  imports: [ReactiveFormsModule, RouterModule, CommonModule],
  templateUrl: './login.html',
  styleUrl: './login.css',
})
export class Login implements OnInit, AfterViewInit, OnDestroy {
  private authService = inject(AuthService);
  private router = inject(Router);
  private route = inject(ActivatedRoute);
  private googleAuthService = inject(GoogleAuthService);
  private platformId = inject(PLATFORM_ID);
  private valueChangesSub?: Subscription;
  private errorTimer?: ReturnType<typeof setTimeout>;

  @ViewChild('googleBtnContainer') googleBtnContainer!: ElementRef<HTMLDivElement>;

  private readonly REMEMBER_KEY = 'myquiz_remember_email';

  private get redirectUrl(): string {
    const returnUrl = this.route.snapshot.queryParamMap.get('returnUrl');
    return returnUrl && returnUrl.startsWith('/') ? returnUrl : '/dashboard';
  }

  ngOnInit() {
    this.loadRememberedEmail();
  }

  private loadRememberedEmail() {
    if (isPlatformBrowser(this.platformId)) {
      const savedEmail = localStorage.getItem(this.REMEMBER_KEY);
      if (savedEmail) {
        this.loginForm.patchValue({
          email: savedEmail,
          rememberMe: true
        });
      }
    }
  }

  private saveOrClearRememberedEmail() {
    if (isPlatformBrowser(this.platformId)) {
      const { email, rememberMe } = this.loginForm.getRawValue();
      if (rememberMe) {
        localStorage.setItem(this.REMEMBER_KEY, email);
      } else {
        localStorage.removeItem(this.REMEMBER_KEY);
      }
    }
  }

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

  showPassword = signal(false);
  errorMessage = signal<string | null>(null);
  isLoading = signal(false);

  // Kiểm soát lỗi validation: tự ẩn sau 3s + ẩn khi gõ
  showErrors = signal(false);

  handleGoogleLogin(idToken: string) {
    this.isLoading.set(true);
    this.errorMessage.set(null);

    this.authService.loginWithGoogle(idToken)
      .pipe(finalize(() => this.isLoading.set(false)))
      .subscribe({
        next: () => {
          this.router.navigateByUrl(this.redirectUrl);
        },
        error: (err) => {
          this.errorMessage.set(err.error?.message || 'Google Login failed. Please try again.');
          setTimeout(() => this.errorMessage.set(null), 5000);
        }
      });
  }

  loginForm = new FormGroup({
    email: new FormControl('', {
      nonNullable: true,
      validators: [Validators.required, Validators.email]
    }),
    password: new FormControl('', {
      nonNullable: true,
      validators: [Validators.required, Validators.minLength(6)]
    }),
    rememberMe: new FormControl(false, { nonNullable: true }),
  });

  togglePassword() {
    this.showPassword.update(v => !v);
  }

  private showValidationErrors() {
    this.showErrors.set(true);
    clearTimeout(this.errorTimer);
    this.errorTimer = setTimeout(() => this.showErrors.set(false), 3000);

    this.valueChangesSub?.unsubscribe();
    this.valueChangesSub = this.loginForm.valueChanges.subscribe(() => {
      this.showErrors.set(false);
      this.valueChangesSub?.unsubscribe();
      clearTimeout(this.errorTimer);
    });
  }

  onSubmit() {
    if (this.loginForm.invalid) {
      this.loginForm.markAllAsTouched();
      this.showValidationErrors();
      return;
    }

    this.isLoading.set(true);
    this.errorMessage.set(null);

    const rawValue = this.loginForm.getRawValue();

    this.authService.login({ email: rawValue.email, password: rawValue.password })
      .pipe(finalize(() => this.isLoading.set(false)))
      .subscribe({
        next: () => {
          this.saveOrClearRememberedEmail();
          this.router.navigateByUrl(this.redirectUrl);
        },
        error: (err) => {
          this.errorMessage.set(err.error?.message || 'Login failed. Please check your email and password.');
          setTimeout(() => this.errorMessage.set(null), 5000);
        },
      });
  }
}
