import { createContext, useCallback, useContext, useEffect, useMemo, useRef, useState, type ReactNode } from "react";
import { Link } from "react-router-dom";
import { useChat } from "../context/ChatContext";
import { useDirectChat, type DirectDraft } from "../context/DirectChatContext";
import { useAuth } from "../context/AuthContext";
import { timeAgo } from "../api/types";
import type { ChatAttachmentDto, ChatAttachmentInput, ChatMessageDto, DirectConversationDto } from "../api/types";
import type { DirectAction } from "../api/direct";
import { mediaApi } from "../api/posts";
import { Avatar, EmptyState, IconButton, Loading, cx } from "../ui";
import { IconAudio, IconChat, IconChevronLeft, IconClose, IconFile, IconLock, IconSend, IconUpload } from "../ui/icons";
import { plural } from "../lib/plural";
import styles from "./ChatWidget.module.css";

/* ── Panel state, shared with the top bar button and with profiles ("Написати") ── */
export type ChatTab = "teams" | "direct";

interface ChatPanelState {
  open: boolean;
  tab: ChatTab;
  setTab: (tab: ChatTab) => void;
  toggle: () => void;
  close: () => void;
  /** Opens the panel on a one-to-one chat with this person (or a first-message draft). */
  openDirect: (draft: DirectDraft) => void;
}
const ChatPanelContext = createContext<ChatPanelState | null>(null);

export function ChatPanelProvider({ children }: { children: ReactNode }) {
  const [open, setOpen] = useState(false);
  const [tab, setTab] = useState<ChatTab>("teams");
  const { openWithUser } = useDirectChat();
  const toggle = useCallback(() => setOpen(o => !o), []);
  const close = useCallback(() => setOpen(false), []);
  const openDirect = useCallback(
    (draft: DirectDraft) => {
      setTab("direct");
      setOpen(true);
      void openWithUser(draft);
    },
    [openWithUser],
  );
  const value = useMemo(() => ({ open, tab, setTab, toggle, close, openDirect }), [open, tab, toggle, close, openDirect]);
  return <ChatPanelContext.Provider value={value}>{children}</ChatPanelContext.Provider>;
}

// eslint-disable-next-line react-refresh/only-export-components
export function useChatPanel(): ChatPanelState {
  const ctx = useContext(ChatPanelContext);
  if (!ctx) throw new Error("useChatPanel must be used inside ChatPanelProvider");
  return ctx;
}

/* ── Attachments ────────────────────────────────────────────────────── */

/** Same ceiling as the media endpoint behind the gateway. */
const MAX_FILE_BYTES = 50 * 1024 * 1024;
const MAX_FILES = 10;

interface PendingFile {
  id: string;
  file: File;
  preview?: string;
  status: "uploading" | "done" | "error";
  uploaded?: ChatAttachmentInput;
}

function formatSize(bytes: number): string {
  if (bytes < 1024) return `${bytes} Б`;
  if (bytes < 1024 * 1024) return `${Math.round(bytes / 1024)} КБ`;
  return `${(bytes / 1024 / 1024).toFixed(1)} МБ`;
}

function Attachments({ items, mine }: { items: ChatAttachmentDto[]; mine: boolean }) {
  const media = items.filter(a => a.kind === "image" || a.kind === "video");
  const other = items.filter(a => a.kind !== "image" && a.kind !== "video");
  return (
    <div className={styles.attachments}>
      {media.length > 0 && (
        <div className={cx(styles.mediaGrid, media.length === 1 && styles.mediaGridSingle)}>
          {media.map(a =>
            a.kind === "image" ? (
              <a key={a.url} href={a.url} target="_blank" rel="noreferrer" className={styles.mediaItem}>
                <img src={a.url} alt={a.name ?? "Зображення"} loading="lazy" />
              </a>
            ) : (
              <video key={a.url} className={styles.mediaItem} src={a.url} controls preload="metadata" />
            ),
          )}
        </div>
      )}
      {other.map(a =>
        a.kind === "audio" ? (
          <audio key={a.url} className={styles.audio} src={a.url} controls preload="none" />
        ) : (
          <a key={a.url} href={a.url} target="_blank" rel="noreferrer" download={a.name} className={cx(styles.fileChip, mine && styles.fileChipMine)}>
            <IconFile size={16} />
            <span className={styles.fileName}>{a.name ?? "Файл"}</span>
            {a.size > 0 && <span className={styles.fileSize}>{formatSize(a.size)}</span>}
          </a>
        ),
      )}
    </div>
  );
}

/* ── Message list (team and direct chats) ───────────────────────────── */

function MessageList({
  messages,
  loading,
  hasMore,
  onLoadOlder,
  showSenders,
  empty,
  resetKey,
}: {
  messages: ChatMessageDto[];
  loading: boolean;
  hasMore: boolean;
  onLoadOlder: () => void;
  showSenders: boolean;
  empty: ReactNode;
  resetKey: string;
}) {
  const { user } = useAuth();
  const listRef = useRef<HTMLDivElement | null>(null);

  useEffect(() => {
    listRef.current?.scrollTo({ top: listRef.current.scrollHeight });
  }, [resetKey, messages.length]);

  const handleScroll = () => {
    const el = listRef.current;
    if (!el || loading || !hasMore) return;
    if (el.scrollTop < 40) onLoadOlder();
  };

  return (
    <div className={styles.messages} ref={listRef} onScroll={handleScroll}>
      {hasMore && <div className={styles.olderHint}>Прокрутіть угору, щоб завантажити старіші</div>}
      {loading && messages.length === 0 && <Loading label="Завантаження історії…" />}
      {!loading && messages.length === 0 && empty}
      {messages.map(m => {
        const mine = m.senderId === user?.id;
        return (
          <div key={m.id} className={cx(styles.row, mine ? styles.rowMine : styles.rowOther)}>
            {!mine && showSenders && (
              <Link to={`/users/${m.senderId}`} className={styles.avatarLink} title={`Профіль ${m.senderName}`}>
                <Avatar name={m.senderName} src={m.senderAvatarUrl} seed={m.senderId} size={26} />
              </Link>
            )}
            <div className={styles.bubbleCol}>
              {!mine && showSenders && (
                <Link to={`/users/${m.senderId}`} className={styles.sender}>
                  {m.senderName}
                </Link>
              )}
              {m.attachments && m.attachments.length > 0 && <Attachments items={m.attachments} mine={mine} />}
              {m.text && <div className={cx(styles.bubble, mine ? styles.bubbleMine : styles.bubbleOther)}>{m.text}</div>}
              <span className={styles.time}>{timeAgo(m.sentAt)}</span>
            </div>
          </div>
        );
      })}
    </div>
  );
}

/* ── Composer with uploads ──────────────────────────────────────────── */

function Composer({
  onSend,
  resetKey,
  placeholder = "Повідомлення…",
}: {
  onSend: (text: string, attachments: ChatAttachmentInput[]) => Promise<void>;
  resetKey: string;
  placeholder?: string;
}) {
  const fileRef = useRef<HTMLInputElement | null>(null);
  const [text, setText] = useState("");
  const [pending, setPending] = useState<PendingFile[]>([]);
  const [sending, setSending] = useState(false);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    setText("");
    setError(null);
    setPending([]);
  }, [resetKey]);

  // Object URLs for previews are released when a file leaves the list.
  const previewsRef = useRef<string[]>([]);
  useEffect(() => {
    const now = pending.map(p => p.preview).filter((u): u is string => !!u);
    previewsRef.current.filter(u => !now.includes(u)).forEach(u => URL.revokeObjectURL(u));
    previewsRef.current = now;
  }, [pending]);
  useEffect(() => () => previewsRef.current.forEach(u => URL.revokeObjectURL(u)), []);

  /** Uploads straight away, so by the time the user hits send the files are usually ready. */
  const addFiles = (list: FileList | File[]) => {
    const files = Array.from(list);
    if (files.length === 0) return;
    setError(null);
    const room = MAX_FILES - pending.length;
    if (room <= 0) {
      setError(`Не більше ${MAX_FILES} файлів в одному повідомленні`);
      return;
    }
    const tooBig = files.filter(f => f.size > MAX_FILE_BYTES);
    if (tooBig.length) setError(`Файл більший за 50 МБ: ${tooBig.map(f => f.name).join(", ")}`);
    const accepted = files.filter(f => f.size <= MAX_FILE_BYTES).slice(0, room);
    if (files.length - tooBig.length > room) setError(`Не більше ${MAX_FILES} файлів в одному повідомленні`);

    const added: PendingFile[] = accepted.map(file => ({
      id: `${file.name}-${file.size}-${Math.random().toString(36).slice(2)}`,
      file,
      preview: file.type.startsWith("image/") || file.type.startsWith("video/") ? URL.createObjectURL(file) : undefined,
      status: "uploading",
    }));
    setPending(prev => [...prev, ...added]);

    for (const item of added) {
      mediaApi
        .upload(item.file)
        .then(url =>
          setPending(prev =>
            prev.map(p =>
              p.id === item.id
                ? { ...p, status: "done", uploaded: { url, name: item.file.name, contentType: item.file.type || undefined, size: item.file.size } }
                : p,
            ),
          ),
        )
        .catch(() => setPending(prev => prev.map(p => (p.id === item.id ? { ...p, status: "error" } : p))));
    }
  };

  const removePending = (id: string) => setPending(prev => prev.filter(p => p.id !== id));
  const uploading = pending.some(p => p.status === "uploading");
  const failed = pending.some(p => p.status === "error");
  const ready = pending.filter(p => p.status === "done" && p.uploaded).map(p => p.uploaded!);
  const canSend = !sending && !uploading && !failed && (!!text.trim() || ready.length > 0);

  const handleSend = async () => {
    if (!canSend) return;
    setSending(true);
    setError(null);
    try {
      await onSend(text.trim(), ready);
      setText("");
      setPending([]);
    } catch (e) {
      setError(e instanceof Error ? e.message : "Не вдалося надіслати повідомлення");
    } finally {
      setSending(false);
    }
  };

  return (
    <div
      className={styles.composerWrap}
      onDragOver={e => {
        if (e.dataTransfer.types.includes("Files")) e.preventDefault();
      }}
      onDrop={e => {
        if (e.dataTransfer.files.length) {
          e.preventDefault();
          addFiles(e.dataTransfer.files);
        }
      }}
    >
      {error && <div className={styles.error} role="alert">{error}</div>}

      {pending.length > 0 && (
        <ul className={styles.pending} aria-label="Вкладення">
          {pending.map(p => (
            <li key={p.id} className={cx(styles.pendingItem, p.status === "error" && styles.pendingError)} title={p.file.name}>
              {p.preview && p.file.type.startsWith("image/") ? (
                <img src={p.preview} alt="" />
              ) : p.preview ? (
                <video src={p.preview} muted />
              ) : (
                <span className={styles.pendingIcon}>{p.file.type.startsWith("audio/") ? <IconAudio size={18} /> : <IconFile size={18} />}</span>
              )}
              {p.status === "uploading" && <span className={styles.pendingOverlay}>…</span>}
              {p.status === "error" && <span className={styles.pendingOverlay}>помилка</span>}
              <button type="button" className={styles.pendingRemove} aria-label={`Прибрати ${p.file.name}`} onClick={() => removePending(p.id)}>
                <IconClose size={12} />
              </button>
            </li>
          ))}
        </ul>
      )}

      <form
        className={styles.composer}
        onSubmit={e => {
          e.preventDefault();
          void handleSend();
        }}
      >
        <input
          ref={fileRef}
          type="file"
          multiple
          hidden
          accept="image/*,video/*,audio/*,.pdf,.doc,.docx,.xls,.xlsx,.ppt,.pptx,.txt,.zip,.rar,.7z,.psd,.ai,.fig,.blend"
          onChange={e => {
            if (e.target.files) addFiles(e.target.files);
            e.target.value = "";
          }}
        />
        <button
          type="button"
          className={styles.attach}
          aria-label="Додати фото, відео або файл"
          title="Додати фото, відео або файл"
          disabled={pending.length >= MAX_FILES}
          onClick={() => fileRef.current?.click()}
        >
          <IconUpload size={16} />
        </button>
        <input
          className={styles.input}
          value={text}
          placeholder={pending.length ? "Підпис…" : placeholder}
          aria-label="Повідомлення"
          onChange={e => setText(e.target.value)}
          onPaste={e => {
            const files = Array.from(e.clipboardData.files);
            if (files.length) {
              e.preventDefault();
              addFiles(files);
            }
          }}
          maxLength={2000}
        />
        <button type="submit" className={styles.send} disabled={!canSend} aria-label="Надіслати">
          <IconSend size={16} />
        </button>
      </form>
    </div>
  );
}

/* ── Team chats ─────────────────────────────────────────────────────── */

function ChatDetail() {
  const { openChatId, messages, loadingMessages, hasMoreMessages, loadOlderMessages, sendMessage, closeChat, chats } = useChat();
  const chat = useMemo(() => chats.find(c => c.chatId === openChatId), [chats, openChatId]);

  return (
    <div className={styles.detail}>
      <div className={styles.detailHeader}>
        <IconButton label="До списку чатів" size="sm" onClick={closeChat}>
          <IconChevronLeft />
        </IconButton>
        <Avatar name={chat?.teamName ?? "Чат"} src={chat?.teamAvatarUrl} seed={chat?.teamId} size={28} square />
        <div className={styles.detailTitleWrap}>
          <span className={styles.detailTitle}>{chat?.teamName ?? "Чат команди"}</span>
          {chat && (
            <span className={styles.detailSub}>
              {chat.memberCount} {plural(chat.memberCount, ["учасник", "учасники", "учасників"])}
            </span>
          )}
        </div>
      </div>

      <MessageList
        messages={messages}
        loading={loadingMessages}
        hasMore={hasMoreMessages}
        onLoadOlder={() => void loadOlderMessages()}
        showSenders
        resetKey={openChatId ?? ""}
        empty={<EmptyState icon={<IconChat />} title="Тут поки тихо" text="Напишіть перше повідомлення команді." />}
      />

      <Composer resetKey={openChatId ?? ""} onSend={(text, files) => sendMessage(text, files)} />
    </div>
  );
}

function ChatList() {
  const { chats, loadingChats, openChat, markChatRead, isUnread } = useChat();

  if (loadingChats && chats.length === 0) return <Loading label="Завантаження чатів…" />;
  if (!loadingChats && chats.length === 0) {
    return <EmptyState icon={<IconChat />} title="Чатів ще немає" text="Чат з'являється, щойно ви створюєте команду або приєднуєтесь до неї." />;
  }

  return (
    <ul className={styles.list}>
      {chats.map(chat => {
        const unread = isUnread(chat);
        return (
          <li key={chat.chatId}>
            <button
              type="button"
              className={styles.item}
              onClick={() => {
                void openChat(chat.chatId);
                markChatRead(chat.chatId);
              }}
            >
              <Avatar name={chat.teamName} src={chat.teamAvatarUrl} seed={chat.teamId} size={36} square />
              <span className={styles.itemBody}>
                <span className={styles.itemTop}>
                  <span className={cx(styles.itemName, unread && styles.itemNameUnread)}>{chat.teamName}</span>
                  <span className={styles.itemTime}>{chat.lastMessageAt ? timeAgo(chat.lastMessageAt) : ""}</span>
                </span>
                <span className={styles.itemBottom}>
                  <span className={styles.preview}>
                    {chat.lastMessage ? `${chat.lastSenderName ? chat.lastSenderName + ": " : ""}${chat.lastMessage}` : "Немає повідомлень"}
                  </span>
                  {unread && <span className={styles.unreadDot} aria-label="Непрочитане" />}
                </span>
              </span>
            </button>
          </li>
        );
      })}
    </ul>
  );
}

/* ── Direct chats ───────────────────────────────────────────────────── */

function DirectRow({ c, onOpen, unread }: { c: DirectConversationDto; onOpen: () => void; unread: boolean }) {
  const { user } = useAuth();
  const { nameOf } = useDirectChat();
  const name = nameOf(c);
  const status =
    c.status === "blocked" ? "заблоковано" : c.canAccept ? "запит на листування" : c.status === "pending" ? "очікує відповіді" : null;
  const preview = c.lastMessage ? `${c.lastSenderId === user?.id ? "Ви: " : ""}${c.lastMessage}` : "Немає повідомлень";
  return (
    <li>
      <button type="button" className={styles.item} onClick={onOpen}>
        <Avatar name={name} src={c.otherAvatarUrl} seed={c.otherUserId} size={36} />
        <span className={styles.itemBody}>
          <span className={styles.itemTop}>
            <span className={cx(styles.itemName, unread && styles.itemNameUnread)}>{name}</span>
            <span className={styles.itemTime}>{c.lastMessageAt ? timeAgo(c.lastMessageAt) : ""}</span>
          </span>
          <span className={styles.itemBottom}>
            <span className={styles.preview}>
              {status && <span className={cx(styles.dmStatus, c.status === "blocked" && styles.dmStatusBlocked)}>{status} · </span>}
              {preview}
            </span>
            {unread && <span className={styles.unreadDot} aria-label="Непрочитане" />}
          </span>
        </span>
      </button>
    </li>
  );
}

function DirectList() {
  const { conversations, loading, openConversation, isUnread } = useDirectChat();
  const requests = conversations.filter(c => c.canAccept);
  const rest = conversations.filter(c => !c.canAccept);

  if (loading && conversations.length === 0) return <Loading label="Завантаження…" />;
  if (conversations.length === 0) {
    return (
      <EmptyState
        icon={<IconChat />}
        title="Особистих повідомлень немає"
        text="Щоб написати людині, відкрийте її профіль і натисніть «Написати». Перше повідомлення прийде їй як запит."
      />
    );
  }

  return (
    <div className={styles.dmLists}>
      {requests.length > 0 && (
        <>
          <div className={styles.sectionLabel}>Запити · {requests.length}</div>
          <ul className={styles.list}>
            {requests.map(c => (
              <DirectRow key={c.id} c={c} unread onOpen={() => void openConversation(c.id)} />
            ))}
          </ul>
        </>
      )}
      {rest.length > 0 && (
        <>
          {requests.length > 0 && <div className={styles.sectionLabel}>Чати</div>}
          <ul className={styles.list}>
            {rest.map(c => (
              <DirectRow key={c.id} c={c} unread={isUnread(c)} onOpen={() => void openConversation(c.id)} />
            ))}
          </ul>
        </>
      )}
    </div>
  );
}

/** What stands between the user and the composer: a request to answer, a wait, or a block. */
function DirectBanner({ c, busy, onAct }: { c: DirectConversationDto; busy: boolean; onAct: (a: DirectAction) => void }) {
  const name = useDirectChat().nameOf(c);
  if (c.canAccept) {
    return (
      <div className={styles.banner}>
        <p>
          <b>{name}</b> хоче з вами листуватися. Поки ви не приймете запит, відповісти не вийде. Якщо відхилити, чат заблокується для обох — розблокувати
          зможете лише ви.
        </p>
        <div className={styles.bannerActions}>
          <button type="button" className={styles.bannerPrimary} disabled={busy} onClick={() => onAct("accept")}>
            Прийняти
          </button>
          <button type="button" className={styles.bannerGhost} disabled={busy} onClick={() => onAct("decline")}>
            Відхилити
          </button>
        </div>
      </div>
    );
  }
  if (c.status === "blocked") {
    return (
      <div className={cx(styles.banner, styles.bannerBlocked)}>
        <p>
          <IconLock size={14} /> {c.blockedByMe ? "Ви заблокували цей чат. Ніхто з вас не може в нього писати." : `${name} заблокував(-ла) цей чат. Писати в нього не можна.`}
        </p>
        {c.canUnblock && (
          <div className={styles.bannerActions}>
            <button type="button" className={styles.bannerPrimary} disabled={busy} onClick={() => onAct("unblock")}>
              Розблокувати
            </button>
          </div>
        )}
      </div>
    );
  }
  if (c.status === "pending" && c.requestedByMe) {
    return (
      <div className={styles.banner}>
        <p>
          Запит надіслано. {name} побачить ваші повідомлення і вирішить, чи приймати його.{" "}
          {c.pendingMessagesLeft > 0
            ? `Поки що можна надіслати ще ${c.pendingMessagesLeft} ${plural(c.pendingMessagesLeft, ["повідомлення", "повідомлення", "повідомлень"])}.`
            : "Більше повідомлень можна буде надіслати після відповіді."}
        </p>
      </div>
    );
  }
  return null;
}

function DirectDetail() {
  const { open, draft, messages, loadingMessages, hasMoreMessages, loadOlder, send, act, close, nameOf } = useDirectChat();
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const otherId = open?.otherUserId ?? draft?.userId ?? "";
  const otherName = open ? nameOf(open) : draft?.username ?? "Користувач";
  const otherAvatar = open?.otherAvatarUrl ?? draft?.avatarUrl;
  const key = open?.id ?? `draft:${otherId}`;

  const doAct = async (action: DirectAction) => {
    setBusy(true);
    setError(null);
    try {
      await act(action);
    } catch (e) {
      setError(e instanceof Error ? e.message : "Не вдалося виконати дію");
    } finally {
      setBusy(false);
    }
  };

  const canSend = draft ? true : !!open?.canSend;

  return (
    <div className={styles.detail}>
      <div className={styles.detailHeader}>
        <IconButton label="До списку" size="sm" onClick={close}>
          <IconChevronLeft />
        </IconButton>
        <Link to={`/users/${otherId}`} className={styles.avatarLink} title={`Профіль ${otherName}`}>
          <Avatar name={otherName} src={otherAvatar} seed={otherId} size={28} />
        </Link>
        <div className={styles.detailTitleWrap}>
          <Link to={`/users/${otherId}`} className={styles.detailTitle}>
            {otherName}
          </Link>
          <span className={styles.detailSub}>особисті повідомлення</span>
        </div>
        {open && open.status === "accepted" && (
          <button type="button" className={styles.headerAction} disabled={busy} onClick={() => void doAct("block")}>
            Заблокувати
          </button>
        )}
      </div>

      {open && <DirectBanner c={open} busy={busy} onAct={a => void doAct(a)} />}
      {error && <div className={styles.error} role="alert">{error}</div>}

      <MessageList
        messages={messages}
        loading={loadingMessages}
        hasMore={hasMoreMessages}
        onLoadOlder={() => void loadOlder()}
        showSenders={false}
        resetKey={key}
        empty={
          <EmptyState
            icon={<IconChat />}
            title={draft ? "Нова розмова" : "Повідомлень немає"}
            text={draft ? `Перше повідомлення прийде ${otherName} як запит на листування.` : undefined}
          />
        }
      />

      {canSend && <Composer resetKey={key} placeholder={draft ? "Перше повідомлення…" : "Повідомлення…"} onSend={(text, files) => send(text, files)} />}
    </div>
  );
}

/* ── Panel ──────────────────────────────────────────────────────────── */

/** Docked chat panel (right edge on desktop, full screen on mobile). */
export function ChatPanel() {
  const { user } = useAuth();
  const { openChatId, closeChat, connected, chats, isUnread } = useChat();
  const direct = useDirectChat();
  const { open, close, tab, setTab } = useChatPanel();

  useEffect(() => {
    if (!open) return;
    function onKey(e: KeyboardEvent) {
      if (e.key === "Escape") close();
    }
    window.addEventListener("keydown", onKey);
    return () => window.removeEventListener("keydown", onKey);
  }, [open, close]);

  if (!user || !open) return null;

  const teamUnread = chats.filter(c => isUnread(c)).length;
  const directUnread = direct.conversations.filter(c => c.canAccept || direct.isUnread(c)).length;
  const inDirect = tab === "direct" && (direct.open || direct.draft);
  const inTeam = tab === "teams" && openChatId;

  return (
    <aside className={styles.panel} aria-label="Чати">
      <header className={styles.header}>
        <span className={styles.title}>
          Чати
          <span className={cx(styles.conn, connected ? styles.connOn : styles.connOff)}>{connected ? "online" : "offline"}</span>
        </span>
        <IconButton
          label="Закрити чати"
          size="sm"
          onClick={() => {
            closeChat();
            direct.close();
            close();
          }}
        >
          <IconClose size={16} />
        </IconButton>
      </header>
      {!inDirect && !inTeam && (
        <div className={styles.tabs} role="tablist">
          <button type="button" role="tab" aria-selected={tab === "teams"} className={cx(styles.tab, tab === "teams" && styles.tabActive)} onClick={() => setTab("teams")}>
            Команди{teamUnread > 0 && <span className={styles.tabCount}>{teamUnread}</span>}
          </button>
          <button type="button" role="tab" aria-selected={tab === "direct"} className={cx(styles.tab, tab === "direct" && styles.tabActive)} onClick={() => setTab("direct")}>
            Особисті{directUnread > 0 && <span className={styles.tabCount}>{directUnread}</span>}
          </button>
        </div>
      )}
      {tab === "teams" ? openChatId ? <ChatDetail /> : <ChatList /> : inDirect ? <DirectDetail /> : <DirectList />}
    </aside>
  );
}

export default ChatPanel;
