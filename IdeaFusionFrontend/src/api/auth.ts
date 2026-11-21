import { api, upload } from './client';
import type {
  KeycloakUserSummaryDto,
  TwoFactorStatusDto,
  UpdateProfileDto,
  UpdateUserRolesDto,
  UserDto,
  UserRolesResultDto,
} from './types';

/** The three realm roles, matching ServiceDefaults/Security/IdeaFusionRoles.cs. */
export const REALM_ROLES = ['author', 'moderator', 'administrator'] as const;
export type RealmRole = (typeof REALM_ROLES)[number];

/**
 * Keycloak owns registration, login, logout and refresh: this client holds no
 * credentials and mints no tokens, so there are deliberately no endpoints here
 * for any of those. Session state goes through src/auth/keycloak.ts instead.
 */
export const authApi = {
  me: () =>
    api.get<UserDto>('/api/auth/me'),

  /**
   * Whether the caller has an authenticator app, plus the Keycloak account console
   * URL where 2FA is actually configured.
   */
  twoFactorStatus: () =>
    api.get<TwoFactorStatusDto>('/api/auth/me/2fa'),

  /** Search Keycloak users. Administrators only. */
  searchUsers: (search = '') =>
    api.get<KeycloakUserSummaryDto[]>(`/api/auth/users?search=${encodeURIComponent(search)}`),

  /** Grant realm roles. Administrators only. Audited. */
  grantRoles: (userId: string, dto: UpdateUserRolesDto) =>
    api.post<UserRolesResultDto>(`/api/auth/users/${userId}/roles`, dto),

  /** Revoke realm roles. Administrators only. Audited. */
  revokeRoles: (userId: string, dto: UpdateUserRolesDto) =>
    api.delete<UserRolesResultDto>(`/api/auth/users/${userId}/roles`, dto),

  updateProfile: (dto: UpdateProfileDto) =>
    api.put<UserDto>('/api/auth/profile', dto),

  uploadAvatar: (file: File): Promise<{ avatarUrl: string }> => {
    const form = new FormData();
    form.append('file', file);
    return upload<{ avatarUrl: string }>('/api/auth/avatar', form);
  },

  uploadFile: (file: File): Promise<{ url: string }> => {
    const form = new FormData();
    form.append('file', file);
    return upload<{ url: string }>('/api/auth/upload', form);
  },
};