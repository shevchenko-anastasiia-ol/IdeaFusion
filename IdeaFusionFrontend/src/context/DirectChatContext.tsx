import React, { createContext, useCallback, useContext, useEffect, useMemo, useRef, useState } from "react";
import { directApi, type DirectAction } from "../api/direct";
import type { ChatAttachmentInput, ChatMessageDto, DirectConversationDto, DirectUpdate } from "../api/types";
import { useAuth } from "./AuthContext";
import { useChat } from "./ChatContext";

// eslint-disable-next-line react-refresh/only-export-components
export const directSeenKey = (id: string) => `dm:lastSeen:${id}`;

/** Somebody you are about to write to for the first time: there is no conversation yet. */
export interface DirectDraft {
  userId: string;
  username: string;
  avatarUrl?: string;
}

interface DirectChatValue {
  conversations: DirectConversationDto[];
  loading: boolean;
  /** The open conversation, or null while writing a first message to `draft`. */
  open: DirectConversationDto | null;
  draft: DirectDraft | null;
  messages: ChatMessageDto[];
  loadingMessages: boolean;
  hasMoreMessages: boolean;
  /** Requests waiting for my answer. */
  incomingRequests: number;
  refresh: () => Promise<void>;
  openConversation: (id: string) => Promise<void>;
  /** From a profile: opens the existing chat with this person, or a draft for the first message. */
  openWithUser: (draft: DirectDraft) => Promise<void>;
  close: () => void;
  send: (text: string, attachments?: ChatAttachmentInput[]) => Promise<void>;
  act: (action: DirectAction) => Promise<void>;
  loadOlder: () => Promise<void>;
  isUnread: (c: DirectConversationDto) => boolean;
  /** The other person's name; falls back to a name known from their profile when the server only has an id. */
  nameOf: (c: DirectConversationDto) => string;
}

const DirectChatContext = createContext<DirectChatValue | null>(null);

const byTime = (a: ChatMessageDto, b: ChatMessageDto) => new Date(a.sentAt).getTime() - new Date(b.sentAt).getTime();
const activity = (c: DirectConversationDto) => new Date(c.lastMessageAt ?? c.createdAt).getTime();

export function DirectChatProvider({ children }: { children: React.ReactNode }) {
  const { user } = useAuth();
  const { hub } = useChat();

  const [conversations, setConversations] = useState<DirectConversationDto[]>([]);
  const [loading, setLoading] = useState(false);
  const [open, setOpen] = useState<DirectConversationDto | null>(null);
  const [draft, setDraft] = useState<DirectDraft | null>(null);
  const [messages, setMessages] = useState<ChatMessageDto[]>([]);
  const [loadingMessages, setLoadingMessages] = useState(false);
  const [hasMoreMessages, setHasMoreMessages] = useState(false);
  const [, setSeenTick] = useState(0);
  // Names learnt from profiles ("Написати"), kept across reloads.
  const [knownNames, setKnownNames] = useState<Record<string, string>>(() => {
    try {
      return JSON.parse(localStorage.getItem("dm:names") ?? "{}") as Record<string, string>;
    } catch {
      return {};
    }
  });
  const rememberName = useCallback((userId: string, name: string) => {
    if (!name || name === userId) return;
    setKnownNames(prev => {
      if (prev[userId] === name) return prev;
      const next = { ...prev, [userId]: name };
      try {
        localStorage.setItem("dm:names", JSON.stringify(next));
      } catch {
        /* storage full or blocked: the in-memory copy still works */
      }
      return next;
    });
  }, []);
  const nameOf = useCallback(
    (c: DirectConversationDto) => (c.otherUsername && c.otherUsername !== c.otherUserId ? c.otherUsername : knownNames[c.otherUserId] ?? c.otherUsername),
    [knownNames],
  );

  const openIdRef = useRef<string | null>(null);
  const cursorRef = useRef<string | undefined>(undefined);
  openIdRef.current = open?.id ?? null;

  const markSeen = useCallback((c: DirectConversationDto) => {
    if (c.lastMessageAt) localStorage.setItem(directSeenKey(c.id), c.lastMessageAt);
    setSeenTick(t => t + 1);
  }, []);

  const upsert = useCallback((c: DirectConversationDto) => {
    setConversations(prev => [c, ...prev.filter(x => x.id !== c.id)].sort((a, b) => activity(b) - activity(a)));
    if (openIdRef.current === c.id) setOpen(c);
  }, []);

  const refresh = useCallback(async () => {
    if (!user) return;
    setLoading(true);
    try {
      setConversations(await directApi.getMine());
    } catch {
      setConversations([]);
    } finally {
      setLoading(false);
    }
  }, [user]);

  useEffect(() => {
    if (!user) {
      setConversations([]);
      setOpen(null);
      setDraft(null);
      setMessages([]);
      return;
    }
    void refresh();
  }, [user, refresh]);

  // Live updates: new messages and status changes arrive in the caller's personal hub group.
  useEffect(() => {
    if (!hub) return;
    const onUpdate = (update: DirectUpdate) => {
      const c = update.conversation;
      upsert(c);
      const m = update.message;
      if (m && openIdRef.current === c.id) {
        setMessages(prev => (prev.some(x => x.id === m.id) ? prev : [...prev, m].sort(byTime)));
        markSeen(c);
      }
    };
    hub.on("DirectUpdate", onUpdate);
    // A reconnect may have missed updates.
    void refresh();
    return () => hub.off("DirectUpdate", onUpdate);
  }, [hub, upsert, markSeen, refresh]);

  const openConversation = useCallback(
    async (id: string) => {
      setDraft(null);
      setMessages([]);
      cursorRef.current = undefined;
      setHasMoreMessages(false);
      setLoadingMessages(true);
      const known = conversations.find(c => c.id === id) ?? null;
      setOpen(known);
      openIdRef.current = id;
      try {
        const page = await directApi.getHistory(id);
        upsert(page.conversation);
        setOpen(page.conversation);
        setMessages(page.items);
        setHasMoreMessages(page.hasMore);
        cursorRef.current = page.nextCursor;
        markSeen(page.conversation);
      } catch {
        setMessages([]);
      } finally {
        setLoadingMessages(false);
      }
    },
    [conversations, upsert, markSeen],
  );

  const openWithUser = useCallback(
    async (d: DirectDraft) => {
      rememberName(d.userId, d.username);
      const existing = conversations.find(c => c.otherUserId === d.userId) ?? (await directApi.withUser(d.userId).catch(() => null));
      if (existing) {
        await openConversation(existing.id);
        return;
      }
      setOpen(null);
      openIdRef.current = null;
      setMessages([]);
      setHasMoreMessages(false);
      setDraft(d);
    },
    [conversations, openConversation, rememberName],
  );

  const close = useCallback(() => {
    setOpen(null);
    setDraft(null);
    setMessages([]);
    openIdRef.current = null;
    cursorRef.current = undefined;
  }, []);

  const send = useCallback(
    async (text: string, attachments: ChatAttachmentInput[] = []) => {
      const trimmed = text.trim();
      if (!trimmed && attachments.length === 0) return;
      const result = open
        ? await directApi.send(open.id, trimmed || undefined, attachments)
        : draft
          ? await directApi.start(draft.userId, trimmed || undefined, attachments)
          : null;
      if (!result) return;
      setDraft(null);
      openIdRef.current = result.conversation.id;
      upsert(result.conversation);
      setOpen(result.conversation);
      setMessages(prev => (prev.some(x => x.id === result.message.id) ? prev : [...prev, result.message].sort(byTime)));
      markSeen(result.conversation);
    },
    [open, draft, upsert, markSeen],
  );

  const act = useCallback(
    async (action: DirectAction) => {
      if (!open) return;
      upsert(await directApi.act(open.id, action));
    },
    [open, upsert],
  );

  const loadOlder = useCallback(async () => {
    const id = openIdRef.current;
    const cursor = cursorRef.current;
    if (!id || !cursor || loadingMessages || !hasMoreMessages) return;
    setLoadingMessages(true);
    try {
      const page = await directApi.getHistory(id, cursor);
      setMessages(prev => [...page.items, ...prev].sort(byTime));
      setHasMoreMessages(page.hasMore);
      cursorRef.current = page.nextCursor;
    } catch {
      setHasMoreMessages(false);
    } finally {
      setLoadingMessages(false);
    }
  }, [loadingMessages, hasMoreMessages]);

  const isUnread = useCallback(
    (c: DirectConversationDto) =>
      !!c.lastMessageAt && c.lastSenderId !== user?.id && localStorage.getItem(directSeenKey(c.id)) !== c.lastMessageAt,
    [user],
  );

  const incomingRequests = conversations.filter(c => c.canAccept).length;

  const value = useMemo<DirectChatValue>(
    () => ({
      conversations,
      loading,
      open,
      draft,
      messages,
      loadingMessages,
      hasMoreMessages,
      incomingRequests,
      refresh,
      openConversation,
      openWithUser,
      close,
      send,
      act,
      loadOlder,
      isUnread,
      nameOf,
    }),
    [conversations, loading, open, draft, messages, loadingMessages, hasMoreMessages, incomingRequests, refresh, openConversation, openWithUser, close, send, act, loadOlder, isUnread, nameOf],
  );

  return <DirectChatContext.Provider value={value}>{children}</DirectChatContext.Provider>;
}

// eslint-disable-next-line react-refresh/only-export-components
export function useDirectChat(): DirectChatValue {
  const ctx = useContext(DirectChatContext);
  if (!ctx) throw new Error("useDirectChat must be used inside DirectChatProvider");
  return ctx;
}
