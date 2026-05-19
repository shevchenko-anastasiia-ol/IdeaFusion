import { api } from './client';
import type { CommentCreateDto, CommentDto, LikeCreateDto, PostDto, PostUpdateFrontendDto, TagDto } from './types';

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
    form.append('UpdatedBy', dto.updatedBy);
    dto.newMediaFiles?.forEach(f => form.append('NewMediaFiles', f));

    const token = localStorage.getItem('accessToken');
    return fetch(`/api/post/${id}`, {
      method: 'PUT',
      headers: token ? { Authorization: `Bearer ${token}` } : {},
      body: form,
    }).then(r => {
      if (!r.ok) throw new Error(`Update failed: ${r.status}`);
      return r.json() as Promise<PostDto>;
    });
  },

  delete: (id: number) =>
    api.delete<void>(`/api/post/${id}`),

  archive: (id: number) =>
    api.patch<void>(`/api/post/${id}/archive`),
};

export const commentsApi = {
  getByPost: (postId: number) =>
    api.get<CommentDto[]>(`/api/comment/by-post/${postId}`),

  create: (dto: CommentCreateDto) =>
    api.post<CommentDto>('/api/comment', dto),

  delete: (id: number) =>
    api.delete<void>(`/api/comment/${id}`),
};

export const likesApi = {
  count: (postId: number) =>
    api.get<number>(`/api/like/count?postId=${postId}`),

  exists: (postId: number, userId: number) =>
    api.get<boolean>(`/api/like/exists?postId=${postId}&userId=${userId}`),

  add: (dto: LikeCreateDto) =>
    api.post<void>('/api/like', dto),

  remove: (postId: number, userId: number) =>
    api.delete<void>(`/api/like?postId=${postId}&userId=${userId}`),
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
  getByUserName: (userName: string, avatarUrl?: string) => {
    const params = avatarUrl ? `?avatarUrl=${encodeURIComponent(avatarUrl)}` : '';
    return api.get<number>(`/api/postauthor/by-username/${encodeURIComponent(userName)}${params}`);
  },
};

export const mediaApi = {
  upload: (file: File): Promise<string> => {
    const form = new FormData();
    form.append('file', file);
    const token = localStorage.getItem('accessToken');
    return fetch('/api/media/upload', {
      method: 'POST',
      headers: token ? { Authorization: `Bearer ${token}` } : {},
      body: form,
    }).then(r => {
      if (!r.ok) throw new Error(`Upload failed: ${r.status}`);
      return r.json() as Promise<string>;
    });
  },
};

export const savedPostsApi = {
  getByUser: (userId: number) =>
    api.get<{ savedPostId: number; userId: number; savedAt: string; postId: number; postTitle: string; postMediaUrls: string[] }[]>(
      `/api/savedpost/by-user/${userId}`
    ),

  save: (postId: number, userId: number) =>
    api.post<void>('/api/savedpost', { postId, userId }),

  unsave: (postId: number, userId: number) =>
    api.delete<void>(`/api/savedpost?postId=${postId}&userId=${userId}`),
};