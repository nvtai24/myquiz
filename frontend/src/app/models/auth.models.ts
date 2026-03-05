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

export interface  User {
    id: string;
    email: string;
    firstName: string;
    lastName: string;
    roles: string[];
}

export interface LoginResponse {
   success: boolean;
   statusCode: number;
   message: string;
   data: {
    user: User;
   }
}

export interface RegisterResponse {
    success: boolean;
    statusCode: number;
    message: string;
    data: { user: User; } | null;
    errors: any;
}

export interface ConfirmEmailResponse {
    success: boolean;
    statusCode: number;
    message: string;
    data: any;
    errors: any;
}

export interface ForgotPasswordResponse {
    success: boolean;
    statusCode: number;
    message: string;
    data: any;
    errors: any;
}

export interface ResetPasswordResponse {
    success: boolean;
    statusCode: number;
    message: string;
    data: any;
    errors: any;
}
