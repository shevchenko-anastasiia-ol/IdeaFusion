import { useCallback, useEffect, useState } from "react";
import { Link, useLocation, useNavigate, useParams } from "react-router-dom";
import { aggregatorApi } from "../api/aggregator";
import { commentsApi, likesApi, postsApi, savedPostsApi } from "../api/posts";
import type { PostFullDetailsDto } from "../api/types";
import { timeAgo } from "../api/types";
import { useAuth } from "../context/AuthContext";
import { Page } from "../layout/AppShell";
import { usePageMeta } from "../layout/PageMeta";
import { Avatar, Button, ButtonLink, EmptyState, ErrorState, IconButton, Loading, Textarea, useDialogs, useToast, cx } from "../ui";
import {
  IconArrowLeft,
  IconBookmark,
  IconBookmarkFilled,
  IconComment,
  IconEdit,
  IconExternal,
  IconEye,
  IconHeart,
  IconHeartFilled,
  IconShare,
  IconTrash,
} from "../ui/icons";
import PostCopyReview from "../components/PostCopyReview";
import PostMedia from "../components/PostMedia";
import CommentThread from "../components/CommentThread";
import { plural } from "../lib/plural";
import { useAuthorDisplay } from "../lib/users";
import sh from "./shared.module.css";
import s from "./PostDetailPage.module.css";

export default function PostDetailPage() {
  const navigate = useNavigate();
  const { hash } = useLocation();
  const { id } = useParams<{ id: string }>();
  const { user, contentUserId, isModerator, isAuthenticated } = useAuth();
  const { confirm } = useDialogs();
  const toast = useToast();

  const [data, setData] = useState<PostFullDetailsDto | null>(null);
  // Before any early return: the author's real name and email come from Identity.
  const person = useAuthorDisplay(data && !data.post.collaboration ? data.post.author : null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [body, setBody] = useState("");
  const [sending, setSending] = useState(false);
  const [commentError, setCommentError] = useState<string | null>(null);
  const [liked, setLiked] = useState(false);
  const [saved, setSaved] = useState(false);
  const [likes, setLikes] = useState(0);
  const [saves, setSaves] = useState(0);

  usePageMeta(data?.post.title ?? "Пост", [{ label: "Стрічка", to: "/home" }]);

  const load = useCallback(() => {
    if (!id) return;
    setLoading(true);
    setError(null);
    aggregatorApi
      .getPostFull(Number(id), contentUserId ?? undefined)
      .then(d => {
        setData(d);
        setLiked(d.isLikedByCurrentUser);
        setSaved(d.isSavedByCurrentUser);
        setLikes(d.likesCount);
        setSaves(d.savedCount ?? 0);
      })
      .catch((e: Error) => setError(e.message))
      .finally(() => setLoading(false));
  }, [id, contentUserId]);

  useEffect(load, [load]);

  // Links like /posts/1#comments land on that part of the page once it has rendered.
  useEffect(() => {
    if (!data || !hash) return;
    const el = document.getElementById(hash.slice(1));
    if (el) window.requestAnimationFrame(() => el.scrollIntoView({ behavior: "smooth", block: "start" }));
  }, [data, hash]);

  async function share() {
    if (!data) return;
    const url = `${window.location.origin}/posts/${data.post.postId}`;
    try {
      if (navigator.share) {
        await navigator.share({ title: data.post.title, url });
        return;
      }
      await navigator.clipboard.writeText(url);
      toast.success("Посилання скопійовано");
    } catch (err) {
      if (err instanceof DOMException && err.name === "AbortError") return;
      toast.error("Не вдалося поділитися посиланням");
    }
  }

  const refreshComments = useCallback(async () => {
    if (!id) return;
    try {
      const refreshed = await aggregatorApi.getPostFull(Number(id), contentUserId ?? undefined);
      setData(refreshed);
    } catch (err) {
      toast.fromError(err, "Не вдалося оновити коментарі");
    }
  }, [id, contentUserId, toast]);

  function requireAuth(): boolean {
    if (isAuthenticated) return true;
    navigate("/login", { state: { from: `/posts/${id}` } });
    return false;
  }

  async function toggleLike() {
    if (!data || !requireAuth()) return;
    const next = !liked;
    setLiked(next);
    setLikes(c => c + (next ? 1 : -1));
    try {
      if (next) await likesApi.add({ postId: data.post.postId });
      else await likesApi.remove(data.post.postId);
    } catch (err) {
      setLiked(!next);
      setLikes(c => c + (next ? -1 : 1));
      toast.fromError(err, "Не вдалося оновити вподобання");
    }
  }

  async function toggleSave() {
    if (!data || !requireAuth()) return;
    const next = !saved;
    setSaved(next);
    setSaves(c => c + (next ? 1 : -1));
    try {
      if (next) await savedPostsApi.save(data.post.postId);
      else await savedPostsApi.unsave(data.post.postId);
    } catch (err) {
      setSaved(!next);
      setSaves(c => c + (next ? -1 : 1));
      toast.fromError(err, "Не вдалося оновити збережене");
    }
  }

  async function sendComment() {
    if (!body.trim() || !data || !user) return;
    setSending(true);
    setCommentError(null);
    try {
      await commentsApi.create({ postId: data.post.postId, body: body.trim() });
      setBody("");
      await refreshComments();
    } catch (e) {
      setCommentError(e instanceof Error ? e.message : "Помилка надсилання коментаря");
    } finally {
      setSending(false);
    }
  }

  async function moderateDelete() {
    if (!data) return;
    const ok = await confirm({
      title: "Видалити пост?",
      message: `«${data.post.title}» буде видалено назавжди. Дію записано в журнал модерації.`,
      confirmLabel: "Видалити",
      danger: true,
    });
    if (!ok) return;
    try {
      await postsApi.moderateDelete(data.post.postId);
      toast.success("Пост видалено");
      navigate("/home");
    } catch (err) {
      toast.fromError(err, "Помилка видалення");
    }
  }

  if (loading) {
    return (
      <Page>
        <Loading />
      </Page>
    );
  }

  if (error || !data) {
    return (
      <Page width="narrow">
        <Link to="/home" className={sh.backLink}>
          <IconArrowLeft size={16} /> До стрічки
        </Link>
        {error ? <ErrorState title="Не вдалося відкрити пост" message={error} onRetry={load} /> : <EmptyState title="Пост не знайдено" />}
      </Page>
    );
  }

  const { post, comments, commentsCount } = data;
  const isCollab = !!post.collaboration;
  const authorName = post.collaboration?.name ?? person.name;
  const ownName = user?.userName ?? user?.email.split("@")[0];
  const isAuthor = !!user && !!post.author && post.author.userName === ownName;
  const authorAvatar = post.collaboration?.avatarUrl ?? post.author?.avatarUrl ?? (isAuthor ? user?.avatarUrl : undefined);
  const authorLink = isCollab
    ? post.collaboration?.externalId
      ? `/teams/${post.collaboration.externalId}`
      : null
    : post.author?.externalId
      ? `/users/${post.author.externalId}`
      : null;

  const hasMedia = post.mediaUrls.length > 0;
  const avatar = <Avatar name={authorName} src={authorAvatar} seed={post.author?.userId ?? post.collaboration?.collaborationId} size={40} square={isCollab} />;
  const authorSub = isCollab
    ? `команда${data.team?.membersCount != null ? ` · ${data.team.membersCount} ${plural(data.team.membersCount, ["учасник", "учасники", "учасників"])}` : ""}`
    : "автор";

  return (
    <Page width="wide">
      <Link to="/home" className={sh.backLink}>
        <IconArrowLeft size={16} /> До стрічки
      </Link>

      <div className={cx(s.sheet, !hasMedia && s.sheetText)}>
        {hasMedia && (
          <div className={s.mediaCol}>
            <PostMedia urls={post.mediaUrls} title={post.title} seed={post.postId} variant="full" placeholder={false} />
          </div>
        )}

        <div className={s.side}>
          <header className={s.author}>
            {authorLink ? (
              <Link to={authorLink} className={s.authorLink}>
                {avatar}
              </Link>
            ) : (
              avatar
            )}
            <span className={s.authorText}>
              {authorLink ? (
                <Link to={authorLink} className={s.authorName}>
                  {authorName}
                </Link>
              ) : (
                <span className={s.authorName}>{authorName}</span>
              )}
              {!isCollab && person.email && <span className={s.authorEmail}>{person.email}</span>}
              <span className={s.authorSub}>
                {authorSub} · {timeAgo(post.createdAt)}
              </span>
            </span>
            <span className={s.authorActions}>
              {isAuthor && (
                <ButtonLink to={`/posts/edit?id=${post.postId}`} size="sm" variant="secondary" icon={<IconEdit size={15} />}>
                  Редагувати
                </ButtonLink>
              )}
              {isModerator && !isAuthor && (
                <IconButton label="Видалити пост (модерація)" onClick={() => void moderateDelete()} className={s.dangerIcon}>
                  <IconTrash size={18} />
                </IconButton>
              )}
            </span>
          </header>

          <div className={s.scroll}>
            {post.tags.length > 0 && (
              <ul className={s.tagList} aria-label="Теги">
                {post.tags.map(t => (
                  <li key={t}>
                    <Link to={`/search?tag=${encodeURIComponent(t)}`}>#{t.toLowerCase()}</Link>
                  </li>
                ))}
              </ul>
            )}
            <h1 className={s.title}>{post.title}</h1>
            {post.description && <p className={s.description}>{post.description}</p>}

            {post.externalLink && (
              <a className={s.external} href={post.externalLink} target="_blank" rel="noreferrer">
                <span>{post.externalLink.replace(/^https?:\/\//, "")}</span>
                <IconExternal size={14} />
              </a>
            )}

            {data.team && (
              <Link to={`/teams/${data.team.teamId}`} className={s.team}>
                <Avatar name={data.team.name} seed={data.team.teamId} size={34} square />
                <span className={s.authorText}>
                  <span className={s.teamName}>{data.team.name}</span>
                  {data.team.description && <span className={s.teamDesc}>{data.team.description}</span>}
                </span>
              </Link>
            )}

            {isModerator && <PostCopyReview postId={post.postId} />}

            <section className={s.comments} id="comments" aria-labelledby="comments-title">
              <h2 id="comments-title" className={s.commentsTitle}>
                Коментарі <span className={sh.mono}>{commentsCount}</span>
              </h2>
              {comments.length === 0 ? (
                <p className={sh.faint}>Коментарів ще немає — будьте першим.</p>
              ) : (
                <ul className={s.commentList}>
                  {comments.map(c => (
                    <CommentThread key={c.commentId} comment={c} postId={post.postId} onRefresh={refreshComments} />
                  ))}
                </ul>
              )}
            </section>
          </div>

          <footer className={s.foot}>
            <div className={s.actions}>
              <button type="button" className={cx(s.action, liked && s.liked)} onClick={() => void toggleLike()} aria-pressed={liked} aria-label={liked ? "Прибрати вподобання" : "Вподобати"}>
                {liked ? <IconHeartFilled size={24} /> : <IconHeart size={24} />}
              </button>
              <button type="button" className={s.action} onClick={() => document.getElementById("post-comment")?.focus()} aria-label="Написати коментар">
                <IconComment size={24} />
              </button>
              <button type="button" className={s.action} onClick={() => void share()} aria-label="Поділитися">
                <IconShare size={23} />
              </button>
              <button
                type="button"
                className={cx(s.action, s.save, saved && s.saved)}
                onClick={() => void toggleSave()}
                aria-pressed={saved}
                aria-label={saved ? "Прибрати зі збереженого" : "Зберегти пост"}
              >
                {saved ? <IconBookmarkFilled size={23} /> : <IconBookmark size={23} />}
              </button>
            </div>
            <div className={s.counts}>
              <strong>
                {likes} {plural(likes, ["вподобання", "вподобання", "вподобань"])}
              </strong>
              <span className={s.countsMuted}>
                <IconEye size={15} /> {data.viewsCount} · збережено {saves}
              </span>
            </div>

            {isAuthenticated ? (
              <form
                className={s.composer}
                onSubmit={e => {
                  e.preventDefault();
                  void sendComment();
                }}
              >
                <Avatar name={ownName} src={user?.avatarUrl} seed={user?.id} size={32} />
                <Textarea
                  id="post-comment"
                  className={s.composerInput}
                  placeholder="Додати коментар…"
                  rows={1}
                  value={body}
                  onChange={e => setBody(e.target.value)}
                  onKeyDown={e => {
                    if (e.key === "Enter" && !e.shiftKey) {
                      e.preventDefault();
                      void sendComment();
                    }
                  }}
                  aria-label="Коментар"
                />
                <Button type="submit" variant="primary" size="sm" loading={sending} disabled={!body.trim()}>
                  Надіслати
                </Button>
              </form>
            ) : (
              <div className={s.loginHint}>
                <span className={sh.muted}>Увійдіть, щоб залишити коментар.</span>
                <ButtonLink to="/login" size="sm" variant="primary">
                  Увійти
                </ButtonLink>
              </div>
            )}
            {commentError && <span className={s.commentError}>{commentError}</span>}
          </footer>
        </div>
      </div>
    </Page>
  );
}
