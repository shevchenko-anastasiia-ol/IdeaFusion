import { api } from './client';
import type { AuthResponseDto, ChangePasswordDto, LoginDto, RegisterDto, TokenRequestDto, UpdateProfileDto, UserDto } from './types';

export const authApi = {
  register: (dto: RegisterDto) =>
    api.post<void>('/api/auth/register', dto),

  login: (dto: LoginDto) =>
    api.post<AuthResponseDto>('/api/auth/login', dto),

  me: () =>
    api.get<UserDto>('/api/auth/me'),

  refresh: (dto: TokenRequestDto) =>
    api.post<AuthResponseDto>('/api/auth/refresh', dto),

  logout: () =>
    api.post<void>('/api/auth/logout'),

  updateProfile: (dto: UpdateProfileDto) =>
    api.put<UserDto>('/api/auth/profile', dto),

  changePassword: (dto: ChangePasswordDto) =>
    api.post<void>('/api/auth/change-password', dto),

  uploadAvatar: async (file: File): Promise<{ avatarUrl: string }> => {
    const token = localStorage.getItem('accessToken');
    const formData = new FormData();
    formData.append('file', file);
    const res = await fetch('/api/auth/avatar', {
      method: 'POST',
      headers: token ? { Authorization: `Bearer ${token}` } : {},
      body: formData,
    });
    if (!res.ok) {
      const err = await res.json().catch(() => ({}));
      throw new Error((err as { message?: string }).message ?? 'Avatar upload failed');
    }
    return res.json();
  },

  uploadFile: async (file: File): Promise<{ url: string }> => {
    const token = localStorage.getItem('accessToken');
    const formData = new FormData();
    formData.append('file', file);
    const res = await fetch('/api/auth/upload', {
      method: 'POST',
      headers: token ? { Authorization: `Bearer ${token}` } : {},
      body: formData,
    });
    if (!res.ok) {
      const err = await res.json().catch(() => ({}));
      throw new Error((err as { message?: string }).message ?? 'Upload failed');
    }
    return res.json();
  },
};