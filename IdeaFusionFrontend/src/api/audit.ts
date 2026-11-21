import { api } from './client';

/** Mirrors IdentityDomain/Entities/AuditLogEntry.cs. */
export type AuditCategory = 'Security' | 'User' | 'Content' | 'Moderation' | 'System';

/** One row of the audit trail. */
export interface AuditLogEntryDto {
  id: string;
  createdAt: string;
  actorId: string;
  actorName?: string | null;
  action: string;
  targetType: string;
  targetId: string;
  targetName?: string | null;
  details?: string | null;
  category: AuditCategory;
}

/** Mirrors IdentityBLL/DTOs/AuditDtos.cs AuditQueryDto. Every field combines with AND. */
export interface AuditQueryDto {
  limit?: number;
  from?: string;
  to?: string;
  actorId?: string;
  action?: string;
  targetType?: string;
}

function toQueryString(query: AuditQueryDto = {}): string {
  const params = new URLSearchParams();
  if (query.limit != null) params.set('limit', String(query.limit));
  if (query.from) params.set('from', query.from);
  if (query.to) params.set('to', query.to);
  if (query.actorId) params.set('actorId', query.actorId);
  if (query.action) params.set('action', query.action);
  if (query.targetType) params.set('targetType', query.targetType);
  const qs = params.toString();
  return qs ? `?${qs}` : '';
}

export const auditApi = {
  /** The full trail. Administrators only — the API enforces that. */
  getAll: (query: AuditQueryDto = {}) =>
    api.get<AuditLogEntryDto[]>(`/api/audit${toQueryString(query)}`),

  /**
   * The content slice. Moderators and administrators. The service filters this
   * server-side, so asking for more than a moderator may see returns nothing extra.
   */
  getModeration: (query: AuditQueryDto = {}) =>
    api.get<AuditLogEntryDto[]>(`/api/audit/moderation${toQueryString(query)}`),
};
