import { api } from './client';
import type {
  ChatHistoryPage,
  ChatMessageDto,
  ChatSummaryDto,
  SendChatMessageCommand,
} from './types';

export const chatApi = {
  // The server reads the subject off the token, so there is nothing to pass here.
  getMy: () => api.get<ChatSummaryDto[]>('/api/chat/my'),

  getHistory: (chatId: string, before?: string, pageSize = 50) => {
    const params = new URLSearchParams({ pageSize: String(pageSize) });
    if (before) params.set('before', before);
    return api.get<ChatHistoryPage>(`/api/chat/${encodeURIComponent(chatId)}/messages?${params.toString()}`);
  },

  send: (cmd: SendChatMessageCommand) =>
    api.post<ChatMessageDto>(`/api/chat/${encodeURIComponent(cmd.chatId)}/messages`, cmd),
};