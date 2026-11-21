import { useState, type MouseEvent } from "react";
import { Link, useNavigate } from "react-router-dom";
import { likesApi, postsApi, savedPostsApi } from "../api/posts";
import type { PostWithEngagementDto } from "../api/types";
import { timeAgo } from "../api/types";
import { useAuth } from "../context/AuthContext";
import { Avatar, Menu, useDialogs, useToast, cx } from "../ui";
import { IconAlert, IconBookmark, IconBookmarkFilled, IconComment, IconEdit, IconHeart, IconHeartFilled, IconShare, IconTrash } from "../ui/icons";
import { useOpenCopyFlags } from "../lib/copyFlags";
import { plural } from "../lib/plural";
import { useAuthorDisplay } from "../lib/users";
import PostMedia from "./PostMedia";
import s from "./PostCard.module.css";

interface Props {
  item: PostWithEngagementDto;
  /** Called after a moderator removed the post. */
  onDelete?: (postId: number) => void;
  /** Shows the edit action (own portfolio). */
  canEdit?: boolean;
  /** Called when the save state changes (saved page drops unsaved posts). */
  onSavedChange?: (postId: number, saved: boolean) => void;
}

/**
 * One post in the feed, laid out like a social feed entry: who posted it, the work itself,
 * then actions, counts and the caption. Likes and saves update optimistically and roll
 * back if the request fails. Moderators also see an open image copy match, if any.
 */
export default function PostCard({ item, onDelete, canEdit, onSavedChange }: Props) {
  const navigate = useNavigate();
  const { user, isModerator, isAuthenticated } = useAuth();
  const { confirm } = useDialogs();
  const toast = useToast();
  const { post, commentsCount } = item;

  const [liked, setLiked] = useState(item.isLikedByCurrentUser);
  const [saved, setSaved] = useState(item.isSavedByCurrentUser);
  const [likes, setLikes] = useState(item.likesCount);
  const [saves, setSaves] = useState(item.savedCount ?? 0);
  const [expanded, setExpanded] = useState(false);
  const copyFlags = useOpenCopyFlags();
  const flag = (copyFlags.get(post.postId) ?? []).sort((a, b) => b.score - a.score)[0];

  const isCollab = !!post.collaboration;
  const person = useAuthorDisplay(isCollab ? null : post.author);
  const authorName = post.collaboration?.name ?? person.name;
  const ownName = user?.userName ?? user?.email?.split("@")[0];
  const avatarUrl =
    post.collaboration?.avatarUrl ?? post.author?.avatarUrl ?? (post.author?.userName === ownName ? user?.avatarUrl : undefined);
  const authorLink = isCollab
    ? post.collaboration?.externalId
      ? `/teams/${post.collaboration.externalId}`
      : null
    : post.author?.externalId
      ? `/users/${post.author.externalId}`
      : null;

  function requireAuth(): boolean {
    if (isAuthenticated) return true;
    navigate("/login", { state: { from: `/posts/${post.postId}` } });
    return false;
  }

  async function toggleLike(e: MouseEvent) {
    e.stopPropagation();
    if (!requireAuth()) return;
    const next = !liked;
    setLiked(next);
    setLikes(c => c + (next ? 1 : -1));
    try {
      if (next) await likesApi.add({ postId: post.postId });
      else await likesApi.remove(post.postId);
    } catch (err) {
      setLiked(!next);
      setLikes(c => c + (next ? -1 : 1));
      toast.fromError(err, "Не вдалося оновити вподобання");
    }
  }

  async function toggleSave(e: MouseEvent) {
    e.stopPropagation();
    if (!requireAuth()) return;
    const next = !saved;
    setSaved(next);
    setSaves(c => c + (next ? 1 : -1));
    try {
      if (next) await savedPostsApi.save(post.postId);
      else await savedPostsApi.unsave(post.postId);
      onSavedChange?.(post.postId, next);
    } catch (err) {
      setSaved(!next);
      setSaves(c => c + (next ? -1 : 1));
      toast.fromError(err, "Не вдалося оновити збережене");
    }
  }

  async function moderateDelete() {
    const ok = await confirm({
      title: "Видалити пост?",
      message: `«${post.title}» буде видалено назавжди. Дію записано в журнал модерації.`,
      confirmLabel: "Видалити",
      danger: true,
    });
    if (!ok) return;
    try {
      await postsApi.moderateDelete(post.postId);
      toast.success("Пост видалено");
      onDelete?.(post.postId);
    } catch (err) {
      toast.fromError(err, "Помилка видалення");
    }
  }

  const open = () => navigate(`/posts/${post.postId}`);
  const postUrl = `${window.location.origin}/posts/${post.postId}`;

  async function share(e: MouseEvent) {
    e.stopPropagation();
    try {
      if (navigator.share) {
        await navigator.share({ title: post.title, url: postUrl });
        return;
      }
      await navigator.clipboard.writeText(postUrl);
      toast.success("Посилання скопійовано");
    } catch (err) {
      // The user closing the share sheet is not an error.
      if (err instanceof DOMException && err.name === "AbortError") return;
      toast.error("Не вдалося поділитися посиланням");
    }
  }

  const avatar = (
    <Avatar name={authorName} src={avatarUrl} seed={post.author?.userId ?? post.collaboration?.collaborationId} size={38} square={isCollab} />
  );
  const longDesc = (post.description?.length ?? 0) > 160;

  return (
    <article className={s.card}>
      <header className={s.head}>
        {authorLink ? (
          <Link to={authorLink} className={s.authorLink}>
            {avatar}
          </Link>
        ) : (
          avatar
        )}
        <div className={s.who}>
          {authorLink ? (
            <Link to={authorLink} className={s.authorName}>
              {authorName}
            </Link>
          ) : (
            <span className={s.authorName}>{authorName}</span>
          )}
          {!isCollab && person.email && <span className={s.email}>{person.email}</span>}
          <span className={s.meta}>
            {isCollab ? "команда · " : ""}
            {timeAgo(post.createdAt)}
          </span>
        </div>
        <div className={s.headRight}>
          {flag && (
            <Link to={`/posts/${post.postId}#copy-check`} className={cx(s.flag, flag.zone === "red" && s.flagRed)} title="Відкрита перевірка зображення">
              <IconAlert size={13} /> схоже на іншу роботу · {Math.round(flag.score * 100)}%
            </Link>
          )}
          <Menu
            label="Дії з постом"
            items={[
              { label: "Відкрити пост", onSelect: open },
              { label: "Редагувати", icon: <IconEdit size={16} />, hidden: !canEdit, onSelect: () => navigate(`/posts/edit?id=${post.postId}`) },
              { label: "Видалити (модерація)", icon: <IconTrash size={16} />, danger: true, hidden: !isModerator, onSelect: () => void moderateDelete() },
            ]}
          />
        </div>
      </header>

      {post.mediaUrls.length > 0 && (
        <div className={s.media} onClick={open}>
          <PostMedia urls={post.mediaUrls} title={post.title} seed={post.postId} variant="feed" placeholder={false} />
        </div>
      )}

      <div className={s.actions}>
        <button
          type="button"
          className={cx(s.action, liked && s.liked)}
          onClick={toggleLike}
          aria-pressed={liked}
          aria-label={liked ? "Прибрати вподобання" : "Вподобати"}
        >
          {liked ? <IconHeartFilled size={23} /> : <IconHeart size={23} />}
        </button>
        <Link to={`/posts/${post.postId}#comments`} className={s.action} aria-label={`Коментарі: ${commentsCount}`}>
          <IconComment size={23} />
          {commentsCount > 0 && <span>{commentsCount}</span>}
        </Link>
        <button type="button" className={s.action} onClick={e => void share(e)} aria-label="Поділитися">
          <IconShare size={22} />
        </button>
        <button
          type="button"
          className={cx(s.action, s.save, saved && s.saved)}
          onClick={toggleSave}
          aria-pressed={saved}
          aria-label={saved ? "Прибрати зі збереженого" : "Зберегти пост"}
          title={saves > 0 ? `Збережено ${saves} ${plural(saves, ["раз", "рази", "разів"])}` : undefined}
        >
          {saved ? <IconBookmarkFilled size={22} /> : <IconBookmark size={22} />}
        </button>
      </div>

      <div className={s.body}>
        <span className={s.likes}>
          {likes} {plural(likes, ["вподобання", "вподобання", "вподобань"])}
        </span>
        <h3 className={s.title}>
          <Link to={`/posts/${post.postId}`}>{post.title}</Link>
        </h3>
        {post.description && (
          <p className={cx(s.desc, !expanded && s.descClamp)}>
            {post.description}
          </p>
        )}
        {longDesc && !expanded && (
          <button type="button" className={s.more} onClick={() => setExpanded(true)}>
            ще
          </button>
        )}
        {post.tags.length > 0 && (
          <ul className={s.tags} aria-label="Теги">
            {post.tags.map(t => (
              <li key={t}>
                <Link to={`/search?tag=${encodeURIComponent(t)}`}>#{t.toLowerCase()}</Link>
              </li>
            ))}
          </ul>
        )}
        {commentsCount > 0 && (
          <Link to={`/posts/${post.postId}#comments`} className={s.allComments}>
            Переглянути {commentsCount === 1 ? "коментар" : `усі ${commentsCount} ${plural(commentsCount, ["коментар", "коментарі", "коментарів"])}`}
          </Link>
        )}
      </div>
    </article>
  );
}
