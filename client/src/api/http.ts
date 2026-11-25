import type { ProblemDetails } from '../types/common';

const BASE_URL = (import.meta.env.VITE_API_URL as string | undefined) ?? '';

let authTokenProvider: (() => string | null) | null = null;
let unauthorizedHandler: (() => void) | null = null;

export function configureHttpAuth(getToken: () => string | null, onUnauthorized: () => void): void {
  authTokenProvider = getToken;
  unauthorizedHandler = onUnauthorized;
}

export function resolveApiUrl(path: string): string {
  if (path.startsWith('http://') || path.startsWith('https://')) {
    return path;
  }
  return `${BASE_URL}${path}`;
}

export class ApiError extends Error {
  public readonly status: number;
  public readonly problem?: ProblemDetails;

  constructor(status: number, message: string, problem?: ProblemDetails) {
    super(message);
    this.name = 'ApiError';
    this.status = status;
    this.problem = problem;
  }
}

export interface ApiFetchOptions extends RequestInit {
  skipAuthRedirect?: boolean;
}

export async function apiFetch<T>(path: string, init?: ApiFetchOptions): Promise<T> {
  const { skipAuthRedirect, headers: customHeaders, body, ...rest } = init ?? {};
  const headers = new Headers(customHeaders);

  const isFormData = typeof FormData !== 'undefined' && body instanceof FormData;
  if (!isFormData && !headers.has('Content-Type')) {
    headers.set('Content-Type', 'application/json');
  }

  const token = authTokenProvider?.();
  if (token && !headers.has('Authorization')) {
    headers.set('Authorization', `Bearer ${token}`);
  }

  const response = await fetch(resolveApiUrl(path), {
    ...rest,
    body,
    headers,
  });

  if (response.status === 401 && !skipAuthRedirect && token) {
    unauthorizedHandler?.();
  }

  if (!response.ok) {
    let problem: ProblemDetails | undefined;
    try {
      problem = (await response.json()) as ProblemDetails;
    } catch {
      problem = undefined;
    }
    throw new ApiError(
      response.status,
      problem?.title ?? (response.statusText || 'Request failed'),
      problem
    );
  }

  if (response.status === 204) {
    return undefined as T;
  }

  return (await response.json()) as T;
}
