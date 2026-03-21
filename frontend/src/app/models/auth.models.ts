// ── Request DTOs ──

export interface LoginRequest {
  email: string;
  password: string;
}

export interface RegisterRequest {
  email: string;
  password: string;
  firstName: string;
  lastName: string;
}

export interface ConfirmEmailRequest {
  userId: string;
  token: string;
}

export interface ForgotPasswordRequest {
  email: string;
}

export interface ResetPasswordRequest {
  email: string;
  token: string;
  newPassword: string;
}

export interface GoogleLoginRequest {
  idToken: string;
}

// ── Response DTOs ──

export interface User {
  id: string;
  email: string;
  firstName: string;
  lastName: string;
  roles: string[];
}

export interface AuthTokenResponse {
  accessToken: string;
  refreshToken: string;
  expiresAt: string;
  user: User;
}

export interface RefreshTokenResponse {
  expiresAt: string;
}

export interface RegisterResponse {
  user: User;
}
