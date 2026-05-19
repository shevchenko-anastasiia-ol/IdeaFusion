import { api } from './client';
import type {
  PostFullDetailsDto,
  PostWithEngagementDto,
  TeamWithPostsDto,
  UserDashboardDto,
} from './types';

export const aggregatorApi = {
  getFeed: (currentUserId?: number | null, skip = 0, take = 10, sortBy?: 'time' | 'popular') => {
    const params = new URLSearchParams({ skip: String(skip), take: String(take) });
    if (currentUserId != null) params.set('currentUserId', String(currentUserId));
    if (sortBy === 'popular') params.set('sortBy', 'popular');
    return api.get<PostWithEngagementDto[]>(`/api/aggregator/posts/feed?${params}`);
  },

  getPostFull: (postId: number, currentUserId?: number | null) => {
    const q = currentUserId != null ? `?currentUserId=${currentUserId}` : '';
    return api.get<PostFullDetailsDto>(`/api/aggregator/posts/${postId}/full${q}`);
  },

  getPortfolio: (contentUserId: number, identityUserId?: string | null, currentUserId?: number | null) => {
    const params = new URLSearchParams({ contentUserId: String(contentUserId) });
    if (identityUserId) params.set('identityUserId', identityUserId);
    if (currentUserId != null) params.set('currentUserId', String(currentUserId));
    return api.get<PostWithEngagementDto[]>(`/api/aggregator/posts/portfolio?${params}`);
  },

  getPostsByAuthor: (postAuthorId: number, currentUserId?: number) => {
    const q = currentUserId != null ? `?currentUserId=${currentUserId}` : '';
    return api.get<PostWithEngagementDto[]>(`/api/aggregator/posts/by-author/${postAuthorId}${q}`);
  },

  getTeamFull: (teamId: string) =>
    api.get<TeamWithPostsDto>(`/api/aggregator/teams/${teamId}/full`),

  getUserDashboard: (userId: number, identityUserId?: string) => {
    const q = identityUserId ? `?identityUserId=${encodeURIComponent(identityUserId)}` : '';
    return api.get<UserDashboardDto>(`/api/aggregator/users/${userId}/dashboard${q}`);
  },
};