import { CommonModule } from '@angular/common';
import { Component, inject, OnDestroy, signal } from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterModule } from '@angular/router';
import { AuthService } from '../../services/auth.service';
import { finalize, Subscription } from 'rxjs';

@Component({
  selector: 'app-forgot-password',
  imports: [ReactiveFormsModule, CommonModule, RouterModule],
  templateUrl: './forgot-password.html',
  styleUrl: './forgot-password.css',
})
export class ForgotPassword implements OnDestroy {
  private authService = inject(AuthService);
  private router = inject(Router);
  private valueChangesSub?: Subscription;
  private errorTimer?: ReturnType<typeof setTimeout>;

  isLoading = signal(false);
  showErrors = signal(false);
  // 'idle' | 'success'
  pageState = signal<'idle' | 'success'>('idle');
  successMessage = signal('');
  errorMessage = signal<string | null>(null);

  forgotForm = new FormGroup({
    email: new FormControl('', {
      nonNullable: true,
      validators: [Validators.required, Validators.email]
    }),
  });

  ngOnDestroy() {
    this.valueChangesSub?.unsubscribe();
    clearTimeout(this.errorTimer);
  }

  private showValidationErrors() {
    this.showErrors.set(true);
    clearTimeout(this.errorTimer);
    this.errorTimer = setTimeout(() => this.showErrors.set(false), 3000);
    this.valueChangesSub?.unsubscribe();
    this.valueChangesSub = this.forgotForm.valueChanges.subscribe(() => {
      this.showErrors.set(false);
      this.valueChangesSub?.unsubscribe();
      clearTimeout(this.errorTimer);
    });
  }

  onSubmit() {
    if (this.forgotForm.invalid) {
      this.forgotForm.markAllAsTouched();
      this.showValidationErrors();
      return;
    }

    this.isLoading.set(true);
    this.errorMessage.set(null);
    const { email } = this.forgotForm.getRawValue();

    this.authService.forgotPassword({ email })
      .pipe(finalize(() => this.isLoading.set(false)))
      .subscribe({
        next: (res) => {
          this.successMessage.set(res.message || 'Check your email for a reset link.');
          this.pageState.set('success');
        },
        error: (err) => {
          this.errorMessage.set(err.error?.message || 'Something went wrong. Please try again.');
          setTimeout(() => this.errorMessage.set(null), 5000);
        },
      });
  }

  goToLogin() {
    this.router.navigate(['/login']);
  }
}
