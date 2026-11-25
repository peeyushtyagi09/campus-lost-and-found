import { useCallback, useEffect, useMemo, useRef, useState, type ReactNode } from 'react';
import { fetchCurrentUser, loginUser, registerUser } from '../api/auth';
import { configureHttpAuth } from '../api/http';
import type { LoginRequest, RegisterRequest, User } from '../types/auth';
import { AuthContext, type AuthContextValue } from './authContextValue';

const STORAGE_KEY = 'clf_session';

interface StoredSession {
  accessToken: string;
  expiresAt: string;
  user: User;
}

function readStoredSession(): StoredSession | null {
  try {
    const raw = sessionStorage.getItem(STORAGE_KEY);
    if (!raw) {
      return null;
    }
    const parsed = JSON.parse(raw) as StoredSession;
    if (!parsed.accessToken || !parsed.user?.id) {
      return null;
    }
    if (parsed.expiresAt && new Date(parsed.expiresAt).getTime() <= Date.now()) {
      sessionStorage.removeItem(STORAGE_KEY);
      return null;
    }
    return parsed;
  } catch {
    return null;
  }
}

export function AuthProvider({ children }: { children: ReactNode }) {
  const initialSession = useMemo(() => readStoredSession(), []);
  const [token, setToken] = useState<string | null>(initialSession?.accessToken ?? null);
  const [user, setUser] = useState<User | null>(initialSession?.user ?? null);
  const [isInitializing, setIsInitializing] = useState<boolean>(Boolean(initialSession));

  const tokenRef = useRef<string | null>(token);
  tokenRef.current = token;

  const logout = useCallback(() => {
    tokenRef.current = null;
    setToken(null);
    setUser(null);
    try {
      sessionStorage.removeItem(STORAGE_KEY);
    } catch {
      // Ignore storage failures in restricted contexts
    }
  }, []);

  useEffect(() => {
    configureHttpAuth(() => tokenRef.current, logout);
  }, [logout]);

  useEffect(() => {
    if (!initialSession) {
      return;
    }

    let cancelled = false;
    fetchCurrentUser()
      .then((freshUser) => {
        if (!cancelled) {
          setUser(freshUser);
        }
      })
      .catch(() => {
        if (!cancelled) {
          logout();
        }
      })
      .finally(() => {
        if (!cancelled) {
          setIsInitializing(false);
        }
      });

    return () => {
      cancelled = true;
    };
  }, [initialSession, logout]);

  const login = useCallback(async (request: LoginRequest): Promise<User> => {
    const response = await loginUser(request);
    tokenRef.current = response.accessToken;
    setToken(response.accessToken);
    setUser(response.user);
    try {
      sessionStorage.setItem(
        STORAGE_KEY,
        JSON.stringify({
          accessToken: response.accessToken,
          expiresAt: response.expiresAt,
          user: response.user,
        } satisfies StoredSession)
      );
    } catch {
      // Memory-only fallback remains active
    }
    return response.user;
  }, []);

  const register = useCallback(async (request: RegisterRequest): Promise<User> => {
    const response = await registerUser(request);
    return response.user;
  }, []);

  const value = useMemo<AuthContextValue>(
    () => ({
      user,
      token,
      isAuthenticated: Boolean(token && user),
      isInitializing,
      login,
      register,
      logout,
    }),
    [user, token, isInitializing, login, register, logout]
  );

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}
