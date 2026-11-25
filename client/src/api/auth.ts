import type {
  LoginRequest,
  LoginResponse,
  RegisterRequest,
  RegisterResponse,
  User,
} from '../types/auth';
import { apiFetch } from './http';

export function registerUser(request: RegisterRequest): Promise<RegisterResponse> {
  return apiFetch<RegisterResponse>('/api/v1/auth/register', {
    method: 'POST',
    body: JSON.stringify(request),
    skipAuthRedirect: true,
  });
}

export function loginUser(request: LoginRequest): Promise<LoginResponse> {
  return apiFetch<LoginResponse>('/api/v1/auth/login', {
    method: 'POST',
    body: JSON.stringify(request),
    skipAuthRedirect: true,
  });
}

export function fetchCurrentUser(): Promise<User> {
  return apiFetch<User>('/api/v1/auth/me');
}
