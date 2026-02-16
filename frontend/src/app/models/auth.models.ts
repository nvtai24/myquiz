export interface LoginRequest {
    email: string;
    password: string;
}

export interface  User {
    id: string;
    email: string;
    firstName: string;
    lastName: string;
    role: string[];
}

export interface LoginResponse {
   success: boolean;
   statusCode: number;
   message: string;
   data: {
    accessToken: string;
    refreshToken: string;
    expiresAt: string;
    user: User;
   }
}
