import { api, upload } from './client';
import type {
  CommentCreateDto,
  CommentDto,
  LikeCreateDto,
  PostAuthorMeDto,
  PostDto,
  PostUpdateFrontendDto,
  TagDto,
} from './types';

export const postsApi = {
  getAll: () =>
    api.get<PostDto[]>('/api/post'),

  getById: (id: number) =>
    api.get<PostDto>(`/api/post/${id}`),

  getByAuthor: (authorId: number) =>
    api.get<PostDto[]>(`/api/post/by-author/${authorId}`),

  update: (id: number, dto: PostUpdateFrontendDto): Promise<PostDto> => {
    const form = new FormData();
    form.append('Title', dto.title);
    if (dto.description) form.append('Description', dto.description);
    if (dto.externalLink) form.append('ExternalLink', dto.externalLink);
    dto.tagIds.forEach(tid => form.append('TagIds', String(tid)));
    dto.newMediaFiles?.forEach(f => form.append('NewMediaFiles', f));

    return upload<PostDto>(`/api/post/${id}`, form, 'PUT');
  },

  delete: (id: number) =>
    api.delete<void>(`/api/post/${id}`),

  archive: (id: number) =>
    api.patch<void>(`/api/post/${id}/archive`),

  // Moderator-only: deletes a post regardless of who wrote it.
  moderateDelete: (id: number) =>
    api.delete<void>(`/api/post/${id}/moderate`),
};

export const commentsApi = {
  getByPost: (postId: number) =>
    api.get<CommentDto[]>(`/api/comment/by-post/${postId}`),

  create: (dto: CommentCreateDto) =>
    api.post<CommentDto>('/api/comment', dto),

  update: (id: number, body: string) =>
    api.put<CommentDto>(`/api/comment/${id}`, { body }),

  delete: (id: number) =>
    api.delete<void>(`/api/comment/${id}`),

  // Moderator-only: deletes a comment regardless of who wrote it.
  moderateDelete: (id: number) =>
    api.delete<void>(`/api/comment/${id}/moderate`),
};

// Who owns a like or a saved post is decided by the backend from the token, so
// none of these take a userId any more.
export const likesApi = {
  count: (postId: number) =>
    api.get<number>(`/api/like/count?postId=${postId}`),

  exists: (postId: number) =>
    api.get<boolean>(`/api/like/exists?postId=${postId}`),

  add: (dto: LikeCreateDto) =>
    api.post<void>('/api/like', dto),

  remove: (postId: number) =>
    api.delete<void>(`/api/like?postId=${postId}`),
};

export const tagsApi = {
  getAll: () =>
    api.get<TagDto[]>('/api/tag'),

  getByPost: (postId: number) =>
    api.get<TagDto[]>(`/api/tag/by-post/${postId}`),

  create: (name: string) =>
    api.post<TagDto>('/api/tag', { name }),
};

export const postauthorApi = {
  // Resolved from the token. The row is created here on first call.
  me: () =>
    api.get<PostAuthorMeDto>('/api/postauthor/me'),

  // Read-only, and 404s for a name that has never written.
  getByUserName: (userName: string) =>
    api.get<PostAuthorMeDto>(
      `/api/postauthor/by-username/${encodeURIComponent(userName)}`),

  updateOwnAvatar: (avatarUrl?: string) => {
    const params = avatarUrl ? `?avatarUrl=${encodeURIComponent(avatarUrl)}` : '';
    return api.patch<PostAuthorMeDto>(`/api/postauthor/me/avatar${params}`);
  },
};

export const mediaApi = {
  upload: (file: File): Promise<string> => {
    const form = new FormData();
    form.append('file', file);
    return upload<string>('/api/media/upload', form);
  },
};

export const savedPostsApi = {
  getMine: () =>
    api.get<{ savedPostId: number; userId: number; savedAt: string; postId: number; postTitle: string; postMediaUrls: string[] }[]>(
      '/api/savedpost/me'
    ),

  save: (postId: number) =>
    api.post<void>('/api/savedpost', { postId }),

  unsave: (postId: number) =>
    api.delete<void>(`/api/savedpost?postId=${postId}`),
};
