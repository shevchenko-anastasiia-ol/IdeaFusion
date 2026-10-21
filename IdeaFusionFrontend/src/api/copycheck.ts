import { api, upload } from './client';
import { updateToken } from '../auth/keycloak';

/** Image copy detection (ContentService → ml/service). Results are advice, never sanctions. */

export type CopyZone = 'red' | 'yellow' | 'green';
export type CopyMatchType = 'exact' | 'modified' | 'fragment' | 'partial' | 'similar_composition' | 'none';

export interface CopyMatch {
  media_id: number;
  post_id: number;
  owner_user_id: string;
  team_id: string | null;
  p_copy: number;
  zone: CopyZone;
  match_type: CopyMatchType;
  signals: Record<string, number>;
  query_region: number[][] | null;
  reference_region: number[][] | null;
}

export interface CopyCheckResult {
  zone: CopyZone;
  score: number;
  match_type: CopyMatchType;
  best: CopyMatch | null;
  matches: CopyMatch[];
  model_version?: string;
  timings_ms?: Record<string, number>;
}

export interface CopyCheckRecord {
  id: number;
  media_id: number | null;
  post_id: number | null;
  owner_user_id: string | null;
  zone: CopyZone;
  match_type: CopyMatchType;
  score: number;
  matched_media_id: number | null;
  matched_post_id: number | null;
  result: CopyCheckResult;
  status: 'open' | 'confirmed_copy' | 'dismissed';
  decided_by: string | null;
  decided_at: string | null;
  created_at: string;
}

export const MATCH_TYPE_LABELS: Record<CopyMatchType, string> = {
  exact: 'точна копія',
  modified: 'змінена копія',
  fragment: 'фрагмент роботи',
  partial: 'робота як частина зображення',
  similar_composition: 'схожа композиція',
  none: 'збігів немає',
};

export const ZONE_LABELS: Record<CopyZone, string> = {
  red: 'Ймовірна копія',
  yellow: 'Варто перевірити',
  green: 'Збігів не знайдено',
};

export const copyCheckApi = {
  /** Checks an image before publishing; the caller's own and team works are excluded. */
  preview: (file: File, collaborationSnapshotId?: number | null) => {
    const form = new FormData();
    form.append('file', file);
    if (collaborationSnapshotId != null) form.append('collaborationSnapshotId', String(collaborationSnapshotId));
    return upload<CopyCheckResult>('/api/copycheck/preview', form);
  },

  queue: (params: { zones?: CopyZone[]; status?: string; limit?: number } = {}) => {
    const q = new URLSearchParams();
    (params.zones ?? ['red', 'yellow']).forEach(z => q.append('zone', z));
    q.set('status', params.status ?? 'open');
    q.set('limit', String(params.limit ?? 100));
    return api.get<CopyCheckRecord[]>(`/api/copycheck/moderation?${q}`);
  },

  decide: (id: number, decision: 'confirmed_copy' | 'dismissed' | 'open') =>
    api.post<CopyCheckRecord>(`/api/copycheck/moderation/${id}/decision`, { decision }),

  /** The explanation picture needs the bearer token, so it is fetched as a blob URL. */
  explanationUrl: async (id: number): Promise<string | null> => {
    const token = await updateToken(5);
    const res = await fetch(`/api/copycheck/moderation/${id}/explanation`, {
      headers: token ? { Authorization: `Bearer ${token}` } : {},
    });
    if (!res.ok) return null;
    return URL.createObjectURL(await res.blob());
  },
};
