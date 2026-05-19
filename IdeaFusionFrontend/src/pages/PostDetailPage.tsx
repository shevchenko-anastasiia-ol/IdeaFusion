import { FunctionComponent, useEffect, useRef, useState } from "react";
import { useNavigate, useParams } from "react-router-dom";
import FrameComponent2 from "../components/FrameComponent2";
import Moderation from "../components/Moderation";
import { aggregatorApi } from "../api/aggregator";
import { commentsApi, likesApi, savedPostsApi } from "../api/posts";
import type { AggregatorCommentDto, PostFullDetailsDto, UserDto } from "../api/types";
import { timeAgo } from "../api/types";
import { useAuth } from "../context/AuthContext";
import { api } from "../api/client";
import styles from "./PostDetailPage.module.css";

const AVATAR_PALETTE = ["#4002aa","#00c9a7","#ff6b6b","#ffb347","#e040fb","#29b6f6","#ef5350","#7c5cfc"];
function avatarColor(seed: string | number): string {
  const s = String(seed);
  let h = 0;
  for (let i = 0; i < s.length; i++) h = (h * 31 + s.charCodeAt(i)) >>> 0;
  return AVATAR_PALETTE[h % AVATAR_PALETTE.length];
}

const CommentThread: FunctionComponent<{ comment: AggregatorCommentDto; depth?: number; userAvatarMap: Map<string, string> }> = ({ comment, depth = 0, userAvatarMap }) => {
  const { user } = useAuth();
  const currentUserName = user?.userName ?? user?.email?.split('@')[0];
  const avatarUrl = comment.author.avatarUrl
    ?? userAvatarMap.get(comment.author.userName)
    ?? (comment.author.userName === currentUserName ? user?.avatarUrl : undefined);

  return (
    <div className={`${styles.comment} ${depth > 0 ? styles.nested : ""}`}>
      {avatarUrl ? (
        <img src={avatarUrl} alt={comment.author.userName} className={styles.commentAvatar} style={{ objectFit: 'cover' }} />
      ) : (
        <div className={styles.commentAvatar} style={{ backgroundColor: avatarColor(comment.author.userId) }}>
          {comment.author.userName.slice(0, 2).toUpperCase()}
        </div>
      )}
      <div className={styles.commentBody}>
        <div className={styles.commentHeader}>
          <span className={styles.commentName}>{comment.author.userName}</span>
          <span className={styles.commentTime}>{timeAgo(comment.createdAt)}</span>
        </div>
        <p className={styles.commentText}>{comment.body}</p>
      </div>
      {comment.replies?.map(r => (
        <CommentThread key={r.commentId} comment={r} depth={depth + 1} userAvatarMap={userAvatarMap} />
      ))}
    </div>
  );
};

const heartFilter = "brightness(0) saturate(100%) invert(27%) sepia(96%) saturate(1600%) hue-rotate(320deg)";
const starFilter  = "brightness(0) saturate(100%) invert(75%) sepia(60%) saturate(600%) hue-rotate(20deg)";

function getMediaType(url: string): 'image' | 'video' | 'audio' | 'pdf' {
  const ext = url.split('?')[0].split('.').pop()?.toLowerCase() ?? '';
  if (['mp4', 'webm', 'mov', 'avi', 'mkv'].includes(ext)) return 'video';
  if (['mp3', 'wav', 'ogg', 'aac', 'm4a', 'flac'].includes(ext)) return 'audio';
  if (ext === 'pdf') return 'pdf';
  return 'image';
}

const MediaCarousel: FunctionComponent<{ urls: string[]; title: string }> = ({ urls, title }) => {
  const [index, setIndex] = useState(0);
  if (urls.length === 0) return <div className={styles.cover} />;

  const current = urls[index];
  const type = getMediaType(current);

  return (
    <div className={styles.carousel}>
      <div className={styles.carouselTrack}>
        {type === 'video' && <video key={current} src={current} controls className={styles.carouselMedia} />}
        {type === 'audio' && <audio key={current} src={current} controls className={styles.audio} />}
        {type === 'pdf' && (
          <a href={current} target="_blank" rel="noreferrer" className={styles.pdfLink}>
            <span className={styles.pdfIcon}>📄</span>
            Відкрити PDF
          </a>
        )}
        {type === 'image' && <img key={current} src={current} alt={title} className={styles.carouselMedia} />}
      </div>

      {urls.length > 1 && (
        <>
          <button
            className={`${styles.carouselBtn} ${styles.carouselBtnPrev}`}
            onClick={() => setIndex(i => (i - 1 + urls.length) % urls.length)}
            aria-label="Попереднє"
          >&#8249;</button>
          <button
            className={`${styles.carouselBtn} ${styles.carouselBtnNext}`}
            onClick={() => setIndex(i => (i + 1) % urls.length)}
            aria-label="Наступне"
          >&#8250;</button>

          <div className={styles.carouselDots}>
            {urls.map((_, i) => (
              <button
                key={i}
                className={`${styles.carouselDot} ${i === index ? styles.carouselDotActive : ''}`}
                onClick={() => setIndex(i)}
                aria-label={`Медіа ${i + 1}`}
              />
            ))}
          </div>
        </>
      )}
    </div>
  );
};

const PostDetailPage: FunctionComponent = () => {
  const navigate        = useNavigate();
  const { id }          = useParams<{ id: string }>();
  const { user, contentUserId } = useAuth();
  const [data,    setData]    = useState<PostFullDetailsDto | null>(null);
  const [loading, setLoading] = useState(true);
  const [error,   setError]   = useState<string | null>(null);
  const [body,    setBody]    = useState('');
  const [sending, setSending] = useState(false);
  const textRef = useRef<HTMLTextAreaElement>(null);
  const [userAvatarMap, setUserAvatarMap] = useState<Map<string, string>>(new Map());

  const [liked,           setLiked]           = useState(false);
  const [saved,           setSaved]           = useState(false);
  const [localLikesCount, setLocalLikesCount] = useState(0);
  const [localSavedCount, setLocalSavedCount] = useState(0);

  useEffect(() => {
    api.get<UserDto[]>('/api/users')
      .then(users => {
        const map = new Map<string, string>();
        for (const u of users) {
          if (u.userName && u.avatarUrl) map.set(u.userName, u.avatarUrl);
        }
        setUserAvatarMap(map);
      })
      .catch(() => {});
  }, []);

  useEffect(() => {
    if (!id) return;
    aggregatorApi.getPostFull(Number(id), contentUserId ?? undefined)
      .then(d => {
        setData(d);
        setLiked(d.isLikedByCurrentUser);
        setSaved(d.isSavedByCurrentUser);
        setLocalLikesCount(d.likesCount);
        setLocalSavedCount(d.savedCount ?? 0);
      })
      .catch((e: Error) => setError(e.message))
      .finally(() => setLoading(false));
  }, [id, contentUserId]);

  async function handleLike() {
    if (!data) return;
    const next = !liked;
    setLiked(next);
    setLocalLikesCount(c => next ? c + 1 : c - 1);
    if (!contentUserId) return;
    try {
      if (next) await likesApi.add({ postId: data.post.postId, userId: contentUserId });
      else      await likesApi.remove(data.post.postId, contentUserId);
    } catch {
      setLiked(!next);
      setLocalLikesCount(c => next ? c - 1 : c + 1);
    }
  }

  async function handleSave() {
    if (!data) return;
    const next = !saved;
    setSaved(next);
    setLocalSavedCount(c => next ? c + 1 : c - 1);
    if (!contentUserId) return;
    try {
      if (next) await savedPostsApi.save(data.post.postId, contentUserId);
      else      await savedPostsApi.unsave(data.post.postId, contentUserId);
    } catch {
      setSaved(!next);
      setLocalSavedCount(c => next ? c - 1 : c + 1);
    }
  }

  async function handleSend() {
    if (!body.trim() || !data || !user) return;
    setSending(true);
    try {
      await commentsApi.create({
        postId: data.post.postId,
        postAuthorId: data.post.author?.postAuthorId ?? 0,
        body: body.trim(),
        createdBy: user.id,
      });
      setBody('');
      // re-fetch to get updated comments
      const refreshed = await aggregatorApi.getPostFull(Number(id));
      setData(refreshed);
    } catch {}
    setSending(false);
  }

  if (loading) return (
    <div className={styles.page}>
      <FrameComponent2 />
      <div className={styles.content}><p style={{ color: "#aaa", padding: 32 }}>Завантаження...</p></div>
    </div>
  );

  if (error || !data) return (
    <div className={styles.page}>
      <FrameComponent2 />
      <div className={styles.content}><p style={{ color: "#ff6b6b", padding: 32 }}>{error ?? "Пост не знайдено"}</p></div>
    </div>
  );

  const { post, comments, commentsCount, viewsCount } = data;
  const authorName = post.collaboration?.name ?? post.author?.userName ?? "Невідомий";
  const isAuthor = !!user && !!post.author && post.author.userName === (user.userName ?? user.email.split('@')[0]);
  const isAdmin  = user?.roles?.includes('Admin');

  return (
    <div className={styles.page}>
      <FrameComponent2 />

      <div className={styles.content}>
        <button className={styles.back} onClick={() => navigate("/home")}>
          ← Назад до стрічки
        </button>

        <div className={styles.main}>
          {/* Left column — post */}
          <div className={styles.postCol}>
            <div className={styles.authorRow}>
              <div className={styles.author}>
                {(() => {
                  const currentUserName = user?.userName ?? user?.email?.split('@')[0];
                  const url = post.collaboration?.avatarUrl
                    ?? post.author?.avatarUrl
                    ?? (post.author?.userName ? userAvatarMap.get(post.author.userName) : undefined)
                    ?? (post.author?.userName === currentUserName ? user?.avatarUrl : undefined);
                  return url ? (
                    <img src={url} alt={authorName} className={styles.authorAvatar} style={{ objectFit: 'cover' }} />
                  ) : (
                    <div className={styles.authorAvatar} style={{ backgroundColor: avatarColor(post.author?.userId ?? post.collaboration?.name ?? 0) }}>
                      {authorName.slice(0, 2).toUpperCase()}
                    </div>
                  );
                })()}
                <div>
                  <div className={styles.authorName}>{authorName}</div>
                  {post.collaboration && data.team?.membersCount != null && (
                    <div className={styles.authorMeta}>Команда · {data.team.membersCount} учасників</div>
                  )}
                </div>
              </div>
              {isAuthor && (
                <button
                  className={styles.editBtn}
                  title="Редагувати пост"
                  onClick={() => navigate(`/posts/edit?id=${post.postId}`)}
                >
                  <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
                    <path d="M11 4H4a2 2 0 0 0-2 2v14a2 2 0 0 0 2 2h14a2 2 0 0 0 2-2v-7"/>
                    <path d="M18.5 2.5a2.121 2.121 0 0 1 3 3L12 15l-4 1 1-4 9.5-9.5z"/>
                  </svg>
                </button>
              )}
              {isAdmin && !isAuthor && (
                <button
                  className={styles.editBtn}
                  title="Видалити пост (адмін)"
                  style={{ color: "#ff6b6b", borderColor: "#ff3333" }}
                  onClick={async () => {
                    if (!confirm('Видалити цей пост назавжди?')) return;
                    try {
                      await api.delete(`/api/post/${post.postId}`);
                      navigate('/home');
                    } catch (ex: any) { alert(ex.message ?? 'Помилка видалення'); }
                  }}
                >
                  <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
                    <polyline points="3 6 5 6 21 6"/><path d="M19 6l-1 14a2 2 0 0 1-2 2H8a2 2 0 0 1-2-2L5 6"/><path d="M10 11v6"/><path d="M14 11v6"/><path d="M9 6V4a1 1 0 0 1 1-1h4a1 1 0 0 1 1 1v2"/>
                  </svg>
                </button>
              )}
            </div>

            <h1 className={styles.title}>{post.title}</h1>
            {post.tags.length > 0 && <p className={styles.tags}>{post.tags.join(' · ')}</p>}

            <MediaCarousel urls={post.mediaUrls} title={post.title} />

            {post.description && <p className={styles.desc}>{post.description}</p>}

            {post.externalLink && (
              <>
                <p className={styles.linksTitle}>Зовнішні посилання</p>
                <div className={styles.links}>
                  <a className={styles.link} href={post.externalLink} target="_blank" rel="noreferrer">
                    {post.externalLink}
                  </a>
                </div>
              </>
            )}

            <div className={styles.stats}>
              <button className={styles.statBtn} onClick={handleLike}>
                <img
                  src="/icon-heart-1.svg" alt="" className={styles.statIcon}
                  style={{ filter: liked ? heartFilter : "none" }}
                />
                {localLikesCount}
              </button>
              <span className={styles.stat}>
                <img src="/icon-comment-1.svg" alt="" className={styles.statIcon} />
                {commentsCount}
              </span>
              <button className={styles.statBtn} onClick={handleSave}>
                <img
                  src="/Star-Icon.svg" alt="" className={styles.statIcon}
                  style={{ filter: saved ? starFilter : "none" }}
                />
                {localSavedCount}
              </button>
            </div>
          </div>

          {/* Right column — comments */}
          <div className={styles.commentsCol}>
            <p className={styles.commentsTitle}>Коментарі</p>

            <div className={styles.commentsList}>
              {comments.length === 0 && (
                <p style={{ color: "#666", fontSize: 14 }}>Коментарів ще немає</p>
              )}
              {comments.map(c => (
                <CommentThread key={c.commentId} comment={c} userAvatarMap={userAvatarMap} />
              ))}
            </div>

            <div className={styles.inputRow}>
              {user?.avatarUrl ? (
                <img src={user.avatarUrl} alt="" className={styles.myAvatar} style={{ objectFit: 'cover' }} />
              ) : (
                <div className={styles.myAvatar} style={{ backgroundColor: avatarColor(user?.id ?? '') }}>
                  {user ? user.email.slice(0, 2).toUpperCase() : '?'}
                </div>
              )}
              <div className={styles.inputWrap}>
                <textarea
                  ref={textRef}
                  className={styles.input}
                  placeholder="Напишіть коментар..."
                  rows={3}
                  value={body}
                  onChange={e => setBody(e.target.value)}
                />
                <button className={styles.sendBtn} disabled={sending || !body.trim()} onClick={handleSend}>
                  {sending ? '...' : 'Надіслати'}
                </button>
              </div>
            </div>
          </div>
        </div>
      </div>

      <Moderation
        moderationPosition="relative"
        moderationMarginTop="auto"
        moderationTop="unset"
        moderationLeft="124px"
      />
    </div>
  );
};

export default PostDetailPage;