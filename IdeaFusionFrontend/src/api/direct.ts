import { api } from './client';
import type {
  ChatAttachmentInput,
  DirectConversationDto,
  DirectHistoryPage,
  DirectSendResult,
} from './types';

const BASE = '/api/chat/direct';

export type DirectAction = 'accept' | 'decline' | 'block' | 'unblock';

export const directApi = {
  getMine: () => api.get<DirectConversationDto[]>(BASE),

  /** null when the two people have never written to each other. */
  withUser: async (userId: string): Promise<DirectConversationDto | null> =>
    (await api.get<DirectConversationDto | null>(`${BASE}/with/${encodeURIComponent(userId)}`)) ?? null,

  getHistory: (id: string, before?: string, pageSize = 50) => {
    const params = new URLSearchParams({ pageSize: String(pageSize) });
    if (before) params.set('before', before);
    return api.get<DirectHistoryPage>(`${BASE}/${encodeURIComponent(id)}/messages?${params.toString()}`);
  },

  /** The first message to somebody opens a request; later ones go into the same chat. */
  start: (recipientId: string, text?: string, attachments: ChatAttachmentInput[] = []) =>
    api.post<DirectSendResult>(`${BASE}/start`, { recipientId, text, attachments }),

  send: (id: string, text?: string, attachments: ChatAttachmentInput[] = []) =>
    api.post<DirectSendResult>(`${BASE}/${encodeURIComponent(id)}/messages`, { text, attachments }),

  act: (id: string, action: DirectAction) =>
    api.post<DirectConversationDto>(`${BASE}/${encodeURIComponent(id)}/${action}`),

  blockUser: (userId: string) =>
    api.post<DirectConversationDto>(`${BASE}/users/${encodeURIComponent(userId)}/block`),
};
