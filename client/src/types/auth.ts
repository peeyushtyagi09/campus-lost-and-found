export type UserRole = 'User' | 'Staff' | 'Admin';

export interface User {
  id: string;
  displayName: string;
  email: string;
  role: UserRole;
}

export interface RegisterRequest {
  displayName: string;
  email: string;
  password: string;
}

export interface RegisterResponse {
  user: User;
}

export interface LoginRequest {
  email: string;
  password: string;
}

export interface LoginResponse {
  accessToken: string;
  expiresAt: string;
  user: User;
}
