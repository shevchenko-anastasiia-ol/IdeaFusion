import React, { createContext, useCallback, useContext, useEffect, useMemo, useState } from 'react';
import {
  accountConsoleUrl,
  getRoles,
  getToken,
  initKeycloak,
  isAdministrator as kcIsAdministrator,
  isAuthenticated,
  isModerator as kcIsModerator,
  login as kcLogin,
  logout as kcLogout,
  updateToken as kcUpdateToken,
} from '../auth/keycloak';
import { authApi } from '../api/auth';
import { postauthorApi } from '../api/posts';
import type { UpdateProfileDto, UserDto } from '../api/types';

interface AuthContextValue {
  /** The profile row for the logged-in user, or null when logged out. */
  user: UserDto | null;
  /** Content-service id for the token's subject, from /api/postauthor/me. */
  contentUserId: number | null;
  contentPostAuthorId: number | null;
  /** The raw access token, for SignalR and anything outside `api`. */
  token: string | null;
  roles: string[];
  isModerator: boolean;
  isAdministrator: boolean;
  isAuthenticated: boolean;
  /** True until the silent SSO check has settled. */
  loading: boolean;
  /** Why Keycloak could not initialise, when it could not. */
  initError: string | null;
  /** Keycloak account console, where password and 2FA are actually managed. */
  accountConsoleUrl: string;
  login: () => Promise<void>;
  loginWithGoogle: () => Promise<void>;
  logout: () => Promise<void>;
  updateToken: (minValidity?: number) => Promise<string | null>;
  updateAvatar: (file: File) => Promise<void>;
  updateProfile: (dto: UpdateProfileDto) => Promise<void>;
}

const AuthContext = createContext<AuthContextValue | null>(null);

const GOOGLE_IDP_HINT = 'google';

export function AuthProvider({ children }: { children: React.ReactNode }) {
  const [user, setUser]                       = useState<UserDto | null>(null);
  const [contentUserId, setContentUserId]     = useState<number | null>(null);
  const [contentPostAuthorId, setContentAuthor] = useState<number | null>(null);
  const [token, setToken]                     = useState<string | null>(null);
  const [roles, setRoles]                     = useState<string[]>([]);
  const [authenticated, setAuthenticated]     = useState(false);
  const [loading, setLoading]                 = useState(true);
  const [initError, setInitError]             = useState<string | null>(null);

  /** Pulls the profile and the content-side ids for whoever holds the token. */
  const loadSession = useCallback(async () => {
    try {
      const me = await authApi.me();
      setUser(me);
    } catch {
      setUser(null);
    }

    // Both ids come from the token's own subject, so there is nothing to pass in
    // and no way to ask for somebody else's row.
    try {
      const author = await postauthorApi.me();
      setContentUserId(author.userId);
      setContentAuthor(author.postAuthorId);
    } catch {
      setContentUserId(null);
      setContentAuthor(null);
    }

    setToken(getToken());
    setRoles(getRoles());
  }, []);

  useEffect(() => {
    let cancelled = false;

    initKeycloak()
      .then(async wasAuthenticated => {
        if (cancelled) return;
        setAuthenticated(wasAuthenticated && isAuthenticated());
        await loadSession();
      })
      .catch((err: unknown) => {
        if (cancelled) return;
        // Previously swallowed, which made a failed code exchange indistinguishable
        // from "simply logged out". A silent SSO check that finds no session is the
        // expected path and must not look like an error.
        const message = err instanceof Error ? err.message : String(err);
        if (!/login cancelled|does not have a login|not authenticated|no session/i.test(message)) {
          console.error('[auth] Keycloak init failed', err);
          setInitError(message);
        }
        setAuthenticated(false);
      })
      .finally(() => {
        if (!cancelled) setLoading(false);
      });

    return () => { cancelled = true; };
  }, [loadSession]);

  // keycloak-js refreshes on its own schedule; React only needs to be told when a
  // new token lands, so the hub and any memoized header pick it up.
  useEffect(() => {
    if (!authenticated) return;
    const timer = window.setInterval(async () => {
      const fresh = await kcUpdateToken(60);
      if (fresh) setToken(fresh);
    }, 30_000);
    return () => window.clearInterval(timer);
  }, [authenticated]);

  const login = useCallback(async () => {
    setLoading(true);
    await kcLogin({ redirectUri: window.location.origin });
    // The browser navigates to Keycloak; this line is only reached if it is blocked.
    setLoading(false);
  }, []);

  const loginWithGoogle = useCallback(async () => {
    setLoading(true);
    await kcLogin({ redirectUri: window.location.origin, idpHint: GOOGLE_IDP_HINT });
    setLoading(false);
  }, []);

  const logout = useCallback(async () => {
    await kcLogout();
    setUser(null);
    setContentUserId(null);
    setContentAuthor(null);
    setToken(null);
    setRoles([]);
    setAuthenticated(false);
  }, []);

  const updateToken = useCallback(
    (minValidity = 5) => kcUpdateToken(minValidity),
    [],
  );

  const updateAvatar = useCallback(async (file: File) => {
    const { avatarUrl } = await authApi.uploadAvatar(file);
    setUser(prev => prev ? { ...prev, avatarUrl } : prev);
    // Refresh the cached copy in the content service, scoped to our own row.
    postauthorApi.updateOwnAvatar(avatarUrl).catch(() => {});
  }, []);

  const updateProfile = useCallback(async (dto: UpdateProfileDto) => {
    const updated = await authApi.updateProfile(dto);
    setUser(prev => prev ? { ...prev, ...updated } : prev);
  }, []);

  const value = useMemo<AuthContextValue>(() => ({
    user,
    contentUserId,
    contentPostAuthorId,
    token,
    roles,
    isModerator: kcIsModerator(),
    isAdministrator: kcIsAdministrator(),
    isAuthenticated: authenticated,
    loading,
    initError,
    accountConsoleUrl,
    login,
    loginWithGoogle,
    logout,
    updateToken,
    updateAvatar,
    updateProfile,
  }), [
    user, contentUserId, contentPostAuthorId, token, roles, authenticated,
    loading, initError, login, loginWithGoogle, logout, updateToken, updateAvatar, updateProfile,
  ]);

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}

// eslint-disable-next-line react-refresh/only-export-components
export function useAuth(): AuthContextValue {
  const ctx = useContext(AuthContext);
  if (!ctx) throw new Error('useAuth must be used inside AuthProvider');
  return ctx;
}
