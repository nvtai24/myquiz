import { CommonModule } from '@angular/common';
import { Component, inject, signal } from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { AuthService } from '../../services/auth.service';
import { Router } from '@angular/router';
import { finalize } from 'rxjs';

@Component({
  selector: 'app-login',
  imports: [ReactiveFormsModule],
  templateUrl: './login.html',
  styleUrl: './login.css',
})
export class Login {
  private authService = inject(AuthService);
  private router = inject(Router);

  ngOnInit() {
    if (this.authService.isLoggedIn()) {
      this.router.navigate(['/dashboard']);
    }
  }

  showPassword = signal(false);
  errorMessage = signal<string | null>(null);
  isLoading = signal(false);

  loginForm = new FormGroup({
    email: new FormControl('', {
      nonNullable: true, 
      validators: [Validators.required, Validators.email]
    }),
    password: new FormControl('', {
      nonNullable: true,
      validators: [Validators.required, Validators.minLength(6)]
    }),
  });

  togglePassword() {
    this.showPassword.update(v => !v);
  }

  onSubmit() {
    if (this.loginForm.invalid) {
      this.loginForm.markAllAsTouched(); 
      return;
    }

    this.isLoading.set(true);
    this.errorMessage.set(null); 

    const rawValue = this.loginForm.getRawValue();

    this.authService.login(rawValue)
      .pipe(finalize(() => this.isLoading.set(false)))
      .subscribe({
        next: () => {
          this.router.navigate(['/dashboard']);
        },
        error: (err) => {
          this.errorMessage.set(err.error?.message || 'Login failed. Please check your email and password.');
          setTimeout(() => this.errorMessage.set(null), 5000);
        },
      });
  }
}
  