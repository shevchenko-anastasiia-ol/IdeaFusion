import React, { createContext, useCallback, useContext, useEffect, useState } from 'react';
import { authApi } from '../api/auth';
import { postauthorApi } from '../api/posts';
import type { LoginDto, RegisterDto, UpdateProfileDto, UserDto } from '../api/types';

interface AuthContextValue {
  user: UserDto | null;
  contentUserId: number | null;
  contentPostAuthorId: number | null;
  loading: boolean;
  login: (dto: LoginDto) => Promise<void>;
  register: (dto: RegisterDto) => Promise<void>;
  logout: () => Promise<void>;
  updateAvatar: (file: File) => Promise<void>;
  updateProfile: (dto: UpdateProfileDto) => Promise<void>;
}

const AuthContext = createContext<AuthContextValue | null>(null);

async function fetchContentIds(me: UserDto): Promise<{ userId: number | null; postAuthorId: number | null }> {
  const userName = me.userName ?? me.email.split('@')[0];
  try {
    const ids = await postauthorApi.getByUserName(userName, me.avatarUrl);
    return { userId: ids.userId, postAuthorId: ids.postAuthorId };
  } catch {
    return { userId: null, postAuthorId: null };
  }
}

export function AuthProvider({ children }: { children: React.ReactNode }) {
  const [user, setUser]                           = useState<UserDto | null>(null);
  const [contentUserId, setContentUserId]         = useState<number | null>(null);
  const [contentPostAuthorId, setContentPostAuthorId] = useState<number | null>(null);
  const [loading, setLoading]                     = useState(true);

  useEffect(() => {
    const token = localStorage.getItem('accessToken');
    if (!token) { setLoading(false); return; }
    authApi.me()
      .then(async (me) => {
        setUser(me);
        const ids = await fetchContentIds(me);
        setContentUserId(ids.userId);
        setContentPostAuthorId(ids.postAuthorId);
      })
      .catch(() => {
        localStorage.removeItem('accessToken');
        localStorage.removeItem('refreshToken');
      })
      .finally(() => setLoading(false));
  }, []);

  const login = useCallback(async (dto: LoginDto) => {
    const tokens = await authApi.login(dto);
    localStorage.setItem('accessToken', tokens.accessToken);
    localStorage.setItem('refreshToken', tokens.refreshToken);
    const me = await authApi.me();
    setUser(me);
    const ids = await fetchContentIds(me);
    setContentUserId(ids.userId);
    setContentPostAuthorId(ids.postAuthorId);
  }, []);

  const register = useCallback(async (dto: RegisterDto) => {
    await authApi.register(dto);
  }, []);

  const logout = useCallback(async () => {
    try { await authApi.logout(); } catch { /* помилку виходу на сервері ігноруємо */ }
    localStorage.removeItem('accessToken');
    localStorage.removeItem('refreshToken');
    setUser(null);
    setContentUserId(null);
    setContentPostAuthorId(null);
  }, []);

  const updateAvatar = useCallback(async (file: File) => {
    const { avatarUrl } = await authApi.uploadAvatar(file);
    setUser(prev => {
      if (!prev) return prev;
      const updated = { ...prev, avatarUrl };
      const userName = updated.userName ?? updated.email.split('@')[0];
      postauthorApi.getByUserName(userName, avatarUrl).catch(() => {});
      return updated;
    });
  }, []);

  const updateProfile = useCallback(async (dto: UpdateProfileDto) => {
    const updated = await authApi.updateProfile(dto);
    setUser(prev => prev ? { ...prev, ...updated } : prev);
  }, []);

  return (
    <AuthContext.Provider value={{ user, contentUserId, contentPostAuthorId, loading, login, register, logout, updateAvatar, updateProfile }}>
      {children}
    </AuthContext.Provider>
  );
}

// eslint-disable-next-line react-refresh/only-export-components
export function useAuth(): AuthContextValue {
  const ctx = useContext(AuthContext);
  if (!ctx) throw new Error('useAuth must be used inside AuthProvider');
  return ctx;
}