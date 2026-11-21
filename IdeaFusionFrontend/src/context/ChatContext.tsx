import React, {
  createContext,
  useCallback,
  useContext,
  useEffect,
  useRef,
  useState,
} from "react";
import {
  HubConnection,
  HubConnectionBuilder,
  HubConnectionState,
  LogLevel,
} from "@microsoft/signalr";
import { chatApi } from "../api/chat";
import type { ChatAttachmentInput, ChatMessageDto, ChatSummaryDto } from "../api/types";
import { chatMessagePreview } from "../api/types";
import { useAuth } from "./AuthContext";

// eslint-disable-next-line react-refresh/only-export-components
export const chatSeenKey = (chatId: string) => `chat:lastSeen:${chatId}`;

function getSeenAt(chatId: string): string | null {
  return localStorage.getItem(chatSeenKey(chatId));
}

interface ChatContextValue {
  connected: boolean;
  /** The live hub connection, for other chat features that listen on it (direct messages). */
  hub: HubConnection | null;
  chats: ChatSummaryDto[];
  openChatId: string | null;
  messages: ChatMessageDto[];
  loadingChats: boolean;
  loadingMessages: boolean;
  hasMoreMessages: boolean;
  refreshChats: () => Promise<void>;
  openChat: (chatId: string) => Promise<void>;
  closeChat: () => void;
  sendMessage: (text: string, attachments?: ChatAttachmentInput[]) => Promise<void>;
  loadOlderMessages: () => Promise<void>;
  markChatRead: (chatId: string) => void;
  isUnread: (chat: ChatSummaryDto) => boolean;
}

const ChatContext = createContext<ChatContextValue | null>(null);

export function ChatProvider({ children }: { children: React.ReactNode }) {
  const { user, isAuthenticated, updateToken } = useAuth();

  const [connected, setConnected] = useState(false);
  const [hub, setHub] = useState<HubConnection | null>(null);
  const [chats, setChats] = useState<ChatSummaryDto[]>([]);
  const [loadingChats, setLoadingChats] = useState(false);
  const [openChatId, setOpenChatId] = useState<string | null>(null);
  const [messages, setMessages] = useState<ChatMessageDto[]>([]);
  const [loadingMessages, setLoadingMessages] = useState(false);
  const [hasMoreMessages, setHasMoreMessages] = useState(false);

  const connectionRef = useRef<HubConnection | null>(null);
  const openChatIdRef = useRef<string | null>(null);
  const nextCursorRef = useRef<string | undefined>(undefined);
  const joinedChatIdsRef = useRef<Set<string>>(new Set());

  openChatIdRef.current = openChatId;

  const joinGroup = useCallback((chatId: string) => {
    if (!chatId || joinedChatIdsRef.current.has(chatId)) return;
    const connection = connectionRef.current;
    if (!connection || connection.state !== HubConnectionState.Connected) return;
    joinedChatIdsRef.current.add(chatId);
    connection.invoke("JoinChat", chatId).catch(() => {
      joinedChatIdsRef.current.delete(chatId);
    });
  }, []);

  const bumpChatPreview = useCallback((message: ChatMessageDto) => {
    setChats(prev => {
      const existing = prev.find(c => c.chatId === message.chatId);
      if (!existing) {
        joinGroup(message.chatId);
        return [
          {
            chatId: message.chatId,
            teamId: message.chatId,
            teamName: message.chatId,
            memberCount: 0,
            lastMessage: chatMessagePreview(message),
            lastSenderName: message.senderName,
            lastMessageAt: message.sentAt,
          },
          ...prev,
        ];
      }
      return prev.map(c =>
        c.chatId === message.chatId
          ? {
              ...c,
              lastMessage: chatMessagePreview(message),
              lastSenderName: message.senderName,
              lastMessageAt: message.sentAt,
            }
          : c,
      );
    });
  }, [joinGroup]);

  // ── Підключення до хабу ─────────────────────────────────────────────
  useEffect(() => {
    if (!isAuthenticated) {
      connectionRef.current?.stop().catch(() => {});
      connectionRef.current = null;
      joinedChatIdsRef.current = new Set();
      setConnected(false);
      setOpenChatId(null);
      setChats([]);
      return;
    }

    const connection = new HubConnectionBuilder()
      .withUrl("/hubs/chat", {
        // SignalR accepts a promise here and awaits it before opening the socket,
        // so the handshake always carries a token that is good for 5 more seconds.
        // updateToken returns null when the session is gone; an empty string makes
        // the handshake fail, which is what should happen rather than silently
        // connecting with no credentials.
        accessTokenFactory: async () => (await updateToken(5)) ?? '',
      })
      .withAutomaticReconnect()
      .configureLogging(LogLevel.Warning)
      .build();

    connection.on("ReceiveMessage", (message: ChatMessageDto) => {
      bumpChatPreview(message);

      if (openChatIdRef.current === message.chatId) {
        localStorage.setItem(chatSeenKey(message.chatId), message.sentAt);
        setMessages(prev => {
          if (prev.some(m => m.id === message.id)) return prev;
          return [...prev, message].sort(
            (a, b) => new Date(a.sentAt).getTime() - new Date(b.sentAt).getTime(),
          );
        });
      }
    });

    connection.onreconnected(() => {
      joinedChatIdsRef.current.forEach(chatId => {
        if (connection.state === HubConnectionState.Connected) {
          connection.invoke("JoinChat", chatId).catch(() => {});
        }
      });
    });

    connectionRef.current = connection;

    connection
      .start()
      .then(() => {
        setConnected(true);
        setHub(connection);
      })
      .catch(() => setConnected(false));

    return () => {
      setHub(null);
      connection.off("ReceiveMessage");
      connection.stop().catch(() => {});
      connectionRef.current = null;
      joinedChatIdsRef.current = new Set();
      setConnected(false);
    };
  }, [user, isAuthenticated, updateToken, bumpChatPreview]);

  // ── Завантаження списку чатів ───────────────────────────────────────
  const refreshChats = useCallback(async () => {
    if (!user) return;
    setLoadingChats(true);
    try {
      const list = await chatApi.getMy();
      setChats(list);
      list.forEach(c => joinGroup(c.chatId));
    } catch {
      setChats([]);
    } finally {
      setLoadingChats(false);
    }
  }, [user, joinGroup]);

  useEffect(() => {
    if (!user) {
      setChats([]);
      return;
    }
    refreshChats();
  }, [user, refreshChats]);

  useEffect(() => {
    if (user && connected) refreshChats();
  }, [user, connected, refreshChats]);

  const markChatRead = useCallback((chatId: string) => {
    setChats(prev => {
      const chat = prev.find(c => c.chatId === chatId);
      if (chat?.lastMessageAt) localStorage.setItem(chatSeenKey(chatId), chat.lastMessageAt);
      return prev;
    });
  }, []);

  const isUnread = useCallback((chat: ChatSummaryDto) => {
    if (!chat.lastMessageAt) return false;
    return getSeenAt(chat.chatId) !== chat.lastMessageAt;
  }, []);

  const openChat = useCallback(
    async (chatId: string) => {
      if (!user) return;

      setOpenChatId(chatId);
      setMessages([]);
      nextCursorRef.current = undefined;
      setLoadingMessages(true);
      joinGroup(chatId);

      try {
        const page = await chatApi.getHistory(chatId);
        setMessages(page.items);
        setHasMoreMessages(page.hasMore);
        nextCursorRef.current = page.nextCursor;
        const newest = page.items[page.items.length - 1];
        if (newest) localStorage.setItem(chatSeenKey(chatId), newest.sentAt);
      } catch {
        setMessages([]);
        setHasMoreMessages(false);
      } finally {
        setLoadingMessages(false);
      }
    },
    [user, joinGroup],
  );

  const closeChat = useCallback(() => {
    setOpenChatId(null);
    setMessages([]);
    nextCursorRef.current = undefined;
  }, []);

  const sendMessage = useCallback(
    async (text: string, attachments: ChatAttachmentInput[] = []) => {
      if (!user || !openChatIdRef.current) return;
      const chatId = openChatIdRef.current;
      const trimmed = text.trim();
      if (!trimmed && attachments.length === 0) return;

      try {
        const message = await chatApi.send({ chatId, text: trimmed || undefined, attachments });

        setMessages(prev => {
          if (prev.some(m => m.id === message.id)) return prev;
          return [...prev, message].sort(
            (a, b) => new Date(a.sentAt).getTime() - new Date(b.sentAt).getTime(),
          );
        });
        setChats(prev => {
          const existing = prev.find(c => c.chatId === chatId);
          if (!existing) return prev;
          return prev.map(c =>
            c.chatId === chatId
              ? {
                  ...c,
                  lastMessage: chatMessagePreview(message),
                  lastSenderName: message.senderName,
                  lastMessageAt: message.sentAt,
                }
              : c,
          );
        });
        localStorage.setItem(chatSeenKey(chatId), message.sentAt);
      } catch {
        throw new Error("Не вдалося надіслати повідомлення");
      }
    },
    [user],
  );

  const loadOlderMessages = useCallback(async () => {
    if (!user || !openChatIdRef.current || loadingMessages) return;
    const chatId = openChatIdRef.current;
    const cursor = nextCursorRef.current;
    if (!hasMoreMessages || !cursor) return;

    setLoadingMessages(true);
    try {
      const page = await chatApi.getHistory(chatId, cursor, 50);
      setMessages(prev =>
        [...page.items, ...prev].sort(
          (a, b) => new Date(a.sentAt).getTime() - new Date(b.sentAt).getTime(),
        ),
      );
      setHasMoreMessages(page.hasMore);
      nextCursorRef.current = page.nextCursor;
    } catch {
      setHasMoreMessages(false);
    } finally {
      setLoadingMessages(false);
    }
  }, [user, loadingMessages, hasMoreMessages]);

  return (
    <ChatContext.Provider
      value={{
        connected,
        hub,
        chats,
        openChatId,
        messages,
        loadingChats,
        loadingMessages,
        hasMoreMessages,
        refreshChats,
        openChat,
        closeChat,
        sendMessage,
        loadOlderMessages,
        markChatRead,
        isUnread,
      }}
    >
      {children}
    </ChatContext.Provider>
  );
}

// eslint-disable-next-line react-refresh/only-export-components
export function useChat(): ChatContextValue {
  const ctx = useContext(ChatContext);
  if (!ctx) throw new Error("useChat must be used inside ChatProvider");
  return ctx;
}