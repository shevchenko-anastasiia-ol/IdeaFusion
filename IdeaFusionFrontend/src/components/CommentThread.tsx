import { useState } from "react";
import { Link } from "react-router-dom";
import { useAuthorDisplay } from "../lib/users";
import { commentsApi } from "../api/posts";
import type { AggregatorCommentDto } from "../api/types";
import { timeAgo } from "../api/types";
import { useAuth } from "../context/AuthContext";
import { Avatar, Button, IconButton, Textarea, useDialogs, useToast, cx } from "../ui";
import { IconComment, IconEdit, IconTrash } from "../ui/icons";
import s from "./CommentThread.module.css";

interface Props {
  comment: AggregatorCommentDto;
  postId: number;
  depth?: number;
  onRefresh: () => Promise<void> | void;
}

/** A comment with its replies. Replies are one level deep, as the API models them. */
export default function CommentThread({ comment, postId, depth = 0, onRefresh }: Props) {
  const { user, contentPostAuthorId, isModerator } = useAuth();
  const { confirm } = useDialogs();
  const toast = useToast();
  const ownName = user?.userName ?? user?.email?.split("@")[0];
  const isOwn = !!contentPostAuthorId && comment.author.postAuthorId === contentPostAuthorId;
  const avatarUrl = comment.author.avatarUrl ?? (comment.author.userName === ownName ? user?.avatarUrl : undefined);
  // A real name instead of the stored user name, which for most accounts is their email.
  const person = useAuthorDisplay(comment.author);
  const profileLink = comment.author.externalId ? `/users/${comment.author.externalId}` : null;

  const [mode, setMode] = useState<"view" | "edit" | "reply">("view");
  const [editBody, setEditBody] = useState(comment.body);
  const [replyBody, setReplyBody] = useState("");
  const [busy, setBusy] = useState(false);

  async function saveEdit() {
    if (!editBody.trim() || !user) return;
    setBusy(true);
    try {
      await commentsApi.update(comment.commentId, editBody.trim());
      setMode("view");
      await onRefresh();
    } catch (err) {
      toast.fromError(err, "Не вдалося зберегти коментар");
    } finally {
      setBusy(false);
    }
  }

  async function sendReply() {
    if (!replyBody.trim() || !user) return;
    setBusy(true);
    try {
      await commentsApi.create({ postId, parentCommentId: comment.commentId, body: replyBody.trim() });
      setReplyBody("");
      setMode("view");
      await onRefresh();
    } catch (err) {
      toast.fromError(err, "Не вдалося надіслати відповідь");
    } finally {
      setBusy(false);
    }
  }

  async function remove(asModerator: boolean) {
    const ok = await confirm({
      title: "Видалити коментар?",
      message: asModerator ? "Коментар буде видалено назавжди. Дію записано в журнал модерації." : "Коментар буде видалено назавжди.",
      confirmLabel: "Видалити",
      danger: true,
    });
    if (!ok) return;
    setBusy(true);
    try {
      if (asModerator) await commentsApi.moderateDelete(comment.commentId);
      else await commentsApi.delete(comment.commentId);
      await onRefresh();
    } catch (err) {
      toast.fromError(err, "Не вдалося видалити коментар");
    } finally {
      setBusy(false);
    }
  }

  return (
    <li className={cx(s.item, depth > 0 && s.nested)}>
      <div className={s.comment}>
        {profileLink ? (
          <Link to={profileLink} className={s.avatarLink} aria-label={`Профіль ${person.name}`}>
            <Avatar name={person.name} src={avatarUrl} seed={comment.author.userId} size={depth > 0 ? 26 : 30} />
          </Link>
        ) : (
          <Avatar name={person.name} src={avatarUrl} seed={comment.author.userId} size={depth > 0 ? 26 : 30} />
        )}
        <div className={s.body}>
          <div className={s.head}>
            {/* Name and time share one line; a long name is cut with "…" (full name and email
                in the tooltip) and the time drops below it rather than being squeezed. */}
            <div className={s.who} title={person.email ? `${person.name} · ${person.email}` : person.name}>
              {profileLink ? (
                <Link to={profileLink} className={s.name}>
                  {person.name}
                </Link>
              ) : (
                <span className={s.name}>{person.name}</span>
              )}
              <span className={s.time}>{timeAgo(comment.createdAt)}</span>
            </div>
            <span className={s.actions}>
              {user && depth === 0 && (
                <IconButton label="Відповісти" size="sm" onClick={() => setMode(m => (m === "reply" ? "view" : "reply"))}>
                  <IconComment size={15} />
                </IconButton>
              )}
              {isOwn && (
                <>
                  <IconButton
                    label="Редагувати"
                    size="sm"
                    onClick={() => {
                      setEditBody(comment.body);
                      setMode(m => (m === "edit" ? "view" : "edit"));
                    }}
                  >
                    <IconEdit size={15} />
                  </IconButton>
                  <IconButton label="Видалити" size="sm" disabled={busy} onClick={() => void remove(false)}>
                    <IconTrash size={15} />
                  </IconButton>
                </>
              )}
              {isModerator && !isOwn && (
                <IconButton label="Видалити коментар (модерація)" size="sm" disabled={busy} onClick={() => void remove(true)} className={s.danger}>
                  <IconTrash size={15} />
                </IconButton>
              )}
            </span>
          </div>

          {mode === "edit" ? (
            <div className={s.editor}>
              <Textarea value={editBody} rows={2} onChange={e => setEditBody(e.target.value)} aria-label="Текст коментаря" autoFocus />
              <div className={s.editorActions}>
                <Button size="sm" variant="ghost" onClick={() => setMode("view")}>
                  Скасувати
                </Button>
                <Button size="sm" variant="primary" loading={busy} disabled={!editBody.trim()} onClick={() => void saveEdit()}>
                  Зберегти
                </Button>
              </div>
            </div>
          ) : (
            <p className={s.text}>{comment.body}</p>
          )}

          {mode === "reply" && (
            <div className={s.editor}>
              <Textarea value={replyBody} rows={2} placeholder="Ваша відповідь…" onChange={e => setReplyBody(e.target.value)} aria-label="Відповідь" autoFocus />
              <div className={s.editorActions}>
                <Button size="sm" variant="ghost" onClick={() => setMode("view")}>
                  Скасувати
                </Button>
                <Button size="sm" variant="primary" loading={busy} disabled={!replyBody.trim()} onClick={() => void sendReply()}>
                  Відповісти
                </Button>
              </div>
            </div>
          )}
        </div>
      </div>

      {comment.replies && comment.replies.length > 0 && (
        <ul className={s.replies}>
          {comment.replies.map(r => (
            <CommentThread key={r.commentId} comment={r} depth={depth + 1} postId={postId} onRefresh={onRefresh} />
          ))}
        </ul>
      )}
    </li>
  );
}
