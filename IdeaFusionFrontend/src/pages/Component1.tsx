import { FunctionComponent, useEffect, useState } from "react";
import { Box } from "@mui/material";
import FrameComponent2 from "../components/FrameComponent2";
import FrameComponent1 from "../components/FrameComponent1";
import FrameComponent111 from "../components/FrameComponent111";
import Moderation from "../components/Moderation";
import styles from "./Component1.module.css";
import { useNavigate } from "react-router-dom";
import { aggregatorApi } from "../api/aggregator";
import { likesApi, postsApi, savedPostsApi } from "../api/posts";
import type { PostWithEngagementDto, UserDto } from "../api/types";
import { timeAgo } from "../api/types";
import { useAuth } from "../context/AuthContext";
import { api } from "../api/client";

export type Component1Type = {};

const PREVIEW_COLORS = ["#2d2d4a","#1a3a2a","#3a1a2a","#1a1a3a","#1a2a3a","#2a1a3a","#0a2a3a","#2a0a0a"];
const AVATAR_COLORS  = ["#4002aa","#00c9a7","#ff6b6b","#ffb347","#e040fb","#29b6f6","#ef5350","#7c5cfc"];

function colorFor(id: number, palette: string[]) { return palette[id % palette.length]; }

function getMediaType(url: string): 'image' | 'video' | 'audio' | 'pdf' {
  const ext = url.split('?')[0].split('.').pop()?.toLowerCase() ?? '';
  if (['mp4', 'webm', 'mov', 'avi', 'mkv'].includes(ext)) return 'video';
  if (['mp3', 'wav', 'ogg', 'aac', 'm4a', 'flac'].includes(ext)) return 'audio';
  if (ext === 'pdf') return 'pdf';
  return 'image';
}

const TAG_MAP: Record<string, string[]> = {
  "Дизайн":      ["design", "branding"],
  "Музика":      ["music"],
  "Фото":        ["photography"],
  "Анімація":    ["animation"],
  "Арт":         ["art", "illustration", "street-art"],
  "Цифровий":    ["digital"],
  "Gamedev":     ["gamedev"],
  "3D":          ["3d"],
  "UI/UX":       ["ui-ux"],
  "Архітектура": ["architecture"],
  "Колаб":       ["collab"],
  "Мода":        ["fashion"],
  "Фільм":       ["film"],
  "Поезія":      ["poetry"],
  "Традиційне":  ["traditional"],
  "В процесі":   ["wip"],
  "Письменство": ["writing"],
};

// Inverted: English slug → Ukrainian display label
const TAG_LABEL: Record<string, string> = {};
for (const [label, slugs] of Object.entries(TAG_MAP)) {
  for (const slug of slugs) TAG_LABEL[slug] = label;
}
function tagLabel(raw: string): string {
  return TAG_LABEL[raw.toLowerCase()] ?? (raw.charAt(0).toUpperCase() + raw.slice(1));
}

const PostCard: FunctionComponent<{ item: PostWithEngagementDto; userAvatarMap: Map<string, string>; onDelete?: (id: number) => void }> = ({ item, userAvatarMap, onDelete }) => {
  const navigate = useNavigate();
  const { user, contentUserId } = useAuth();
  const isAdmin = user?.roles?.includes('Admin');
  const { post, commentsCount } = item;
  const [liked,      setLiked]      = useState(item.isLikedByCurrentUser);
  const [starred,    setStarred]    = useState(item.isSavedByCurrentUser);
  const [localLikes, setLocalLikes] = useState(item.likesCount);
  const [localSaved, setLocalSaved] = useState(item.savedCount ?? 0);

  const isCollab   = !!post.collaboration;
  const authorName = post.collaboration?.name ?? post.author?.userName ?? "Невідомий";
  const initials   = authorName.slice(0, 2).toUpperCase();
  const tag        = tagLabel(post.tags[0] ?? "Інше");

  const currentUserName = user?.userName ?? user?.email?.split('@')[0];
  const resolvedAvatarUrl = post.collaboration?.avatarUrl
    ?? post.author?.avatarUrl
    ?? (post.author?.userName ? userAvatarMap.get(post.author.userName) : undefined)
    ?? (post.author?.userName === currentUserName ? user?.avatarUrl : undefined);

  function handleAuthorClick(e: React.MouseEvent) {
    e.stopPropagation();
    if (isCollab && post.collaboration?.externalId) {
      navigate(`/teams/${post.collaboration.externalId}`);
    } else if (!isCollab && post.author?.userId) {
      navigate(`/users/${post.createdBy ?? ''}`);
    }
  }

  async function handleLike(e: React.MouseEvent) {
    e.stopPropagation();
    const next = !liked;
    setLiked(next);
    setLocalLikes(c => next ? c + 1 : c - 1);
    if (!contentUserId) return;
    try {
      if (next) await likesApi.add({ postId: post.postId, userId: contentUserId });
      else      await likesApi.remove(post.postId, contentUserId);
    } catch {
      setLiked(!next);
      setLocalLikes(c => next ? c - 1 : c + 1);
    }
  }

  async function handleSave(e: React.MouseEvent) {
    e.stopPropagation();
    const next = !starred;
    setStarred(next);
    setLocalSaved(c => next ? c + 1 : c - 1);
    if (!contentUserId) return;
    try {
      if (next) await savedPostsApi.save(post.postId, contentUserId);
      else      await savedPostsApi.unsave(post.postId, contentUserId);
    } catch {
      setStarred(!next);
      setLocalSaved(c => next ? c - 1 : c + 1);
    }
  }

  const heartFilter = "brightness(0) saturate(100%) invert(27%) sepia(96%) saturate(1600%) hue-rotate(320deg)";
  const starFilter  = "brightness(0) saturate(100%) invert(75%) sepia(60%) saturate(600%) hue-rotate(20deg)";

  return (
    <div className={styles.postCard} onClick={() => navigate(`/posts/${post.postId}`)}>
      <div className={styles.postHeader}>
        <div
          className={styles.authorRow}
          onClick={handleAuthorClick}
          style={{ cursor: (isCollab && post.collaboration?.externalId) || (!isCollab && post.createdBy) ? "pointer" : "default" }}
        >
          {resolvedAvatarUrl ? (
            <img src={resolvedAvatarUrl} alt={authorName} className={styles.avatar} style={{ objectFit: 'cover' }} />
          ) : (
            <div className={styles.avatar} style={{ backgroundColor: colorFor(post.author?.userId ?? post.postId, AVATAR_COLORS) }}>
              {initials}
            </div>
          )}
          <div>
            <div className={styles.authorName}>{authorName}</div>
            <div className={styles.authorTime}>{timeAgo(post.createdAt)}</div>
          </div>
        </div>
        <div className={styles.cardBadges}>
          {isCollab && (
            <span className={styles.teamBadge}>Команда</span>
          )}
          <span className={styles.categoryBadge}>{tag}</span>
        </div>
      </div>

      {post.mediaUrls.length > 0 ? (() => {
        // Prefer an image for the thumbnail; fall back to first item
        const imageUrl = post.mediaUrls.find(u => getMediaType(u) === 'image');
        if (imageUrl) return <img src={imageUrl} alt={post.title} className={styles.previewImg} />;
        const url = post.mediaUrls[0];
        const type = getMediaType(url);
        if (type === 'video') return (
          <video src={url} muted preload="metadata" className={styles.previewImg}
            style={{ objectFit: 'cover' }} />
        );
        if (type === 'audio') return (
          <div className={styles.preview} style={{ backgroundColor: colorFor(post.postId, PREVIEW_COLORS) }}>
            <span className={styles.previewLabel}>🎵 {post.title}</span>
          </div>
        );
        if (type === 'pdf') return (
          <div className={styles.preview} style={{ backgroundColor: colorFor(post.postId, PREVIEW_COLORS) }}>
            <span className={styles.previewLabel}>📄 {post.title}</span>
          </div>
        );
        return (
          <div className={styles.preview} style={{ backgroundColor: colorFor(post.postId, PREVIEW_COLORS) }}>
            <span className={styles.previewLabel}>Прев'ю · {post.title}</span>
          </div>
        );
      })() : (
        <div className={styles.preview} style={{ backgroundColor: colorFor(post.postId, PREVIEW_COLORS) }}>
          <span className={styles.previewLabel}>Прев'ю · {post.title}</span>
        </div>
      )}

      <div className={styles.postBody}>
        <h3 className={styles.postTitle}>{post.title}</h3>
        <div className={styles.divider} />
        {post.description && <p className={styles.postDesc}>{post.description}</p>}
      </div>

      <div className={styles.postStats}>
        <span className={styles.stat}>
          <img
            src="/icon-heart-1.svg" alt="" className={styles.statIcon}
            onClick={handleLike}
            style={{ cursor: "pointer", filter: liked ? heartFilter : "none" }}
          />
          {localLikes}
        </span>
        <span className={styles.stat}>
          <img src="/icon-comment-1.svg" alt="" className={styles.statIcon} />
          {commentsCount}
        </span>
        <span className={styles.stat}>
          <img
            src="/Star-Icon.svg" alt="" className={styles.statIcon}
            onClick={handleSave}
            style={{ cursor: "pointer", filter: starred ? starFilter : "none" }}
          />
          {localSaved}
        </span>
        {isAdmin && (
          <button
            style={{ marginLeft: "auto", background: "none", border: "none", cursor: "pointer", color: "#ff6b6b", padding: "2px 6px", display: "flex", alignItems: "center" }}
            title="Видалити пост"
            onClick={async e => {
              e.stopPropagation();
              if (!confirm('Видалити цей пост назавжди?')) return;
              try { await postsApi.delete(post.postId); onDelete?.(post.postId); }
              catch (ex: any) { alert(ex.message ?? 'Помилка видалення'); }
            }}
          >
            <svg width="15" height="15" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
              <polyline points="3 6 5 6 21 6"/><path d="M19 6l-1 14a2 2 0 0 1-2 2H8a2 2 0 0 1-2-2L5 6"/><path d="M10 11v6"/><path d="M14 11v6"/><path d="M9 6V4a1 1 0 0 1 1-1h4a1 1 0 0 1 1 1v2"/>
            </svg>
          </button>
        )}
      </div>
    </div>
  );
};

const PAGE_SIZE = 10;

const Component1: FunctionComponent<Component1Type> = ({}) => {
  const navigate = useNavigate();
  const { contentUserId } = useAuth();
  const [activeFilter, setActiveFilter] = useState("Всі");
  const [sortBy, setSortBy] = useState<'time' | 'popular'>('time');
  const [allPosts,   setAllPosts]   = useState<PostWithEngagementDto[]>([]);
  const [loading,    setLoading]    = useState(true);
  const [error,      setError]      = useState<string | null>(null);
  const [page,       setPage]       = useState(1);
  const [userAvatarMap, setUserAvatarMap] = useState<Map<string, string>>(new Map());

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
    setLoading(true);
    setError(null);
    setPage(1);
    aggregatorApi.getFeed(contentUserId ?? undefined, 0, 1000, sortBy)
      .then(batch => setAllPosts(batch))
      .catch((e: Error) => setError(e.message))
      .finally(() => setLoading(false));
  }, [contentUserId, sortBy]);

  const filtered = activeFilter === "Всі"
    ? allPosts
    : allPosts.filter(p => {
        const mapped = TAG_MAP[activeFilter] ?? [activeFilter.toLowerCase()];
        return p.post.tags.some(t => mapped.includes(t.toLowerCase()));
      });

  const totalPages = Math.max(1, Math.ceil(filtered.length / PAGE_SIZE));
  const pagePosts  = filtered.slice((page - 1) * PAGE_SIZE, page * PAGE_SIZE);

  return (
    <Box className={styles.div}>
      <Box className={styles.child} />
      <main className={styles.frameParent}>
        <FrameComponent2 />
        <FrameComponent111
          activeFilter={activeFilter}
          onFilterChange={(f) => { setActiveFilter(f); setPage(1); }}
        />
        <div className={styles.mainLayout}>
          <div className={styles.postsCol}>
            <div style={{ display: 'flex', gap: 8, padding: '8px 0 4px', alignItems: 'center' }}>
              <span style={{ fontSize: 12, color: '#666', fontFamily: 'var(--font-inter)', marginRight: 4 }}>Сортування:</span>
              <button
                onClick={() => { setSortBy('time'); setPage(1); }}
                style={{
                  background: sortBy === 'time' ? 'rgba(124,92,252,0.18)' : 'none',
                  border: `1px solid ${sortBy === 'time' ? '#7c5cfc' : '#333'}`,
                  borderRadius: 20, padding: '4px 14px', fontSize: 12,
                  color: sortBy === 'time' ? '#a48fff' : '#888',
                  cursor: 'pointer', fontFamily: 'var(--font-inter)', transition: 'all .15s',
                }}
              >За часом</button>
              <button
                onClick={() => { setSortBy('popular'); setPage(1); }}
                style={{
                  background: sortBy === 'popular' ? 'rgba(124,92,252,0.18)' : 'none',
                  border: `1px solid ${sortBy === 'popular' ? '#7c5cfc' : '#333'}`,
                  borderRadius: 20, padding: '4px 14px', fontSize: 12,
                  color: sortBy === 'popular' ? '#a48fff' : '#888',
                  cursor: 'pointer', fontFamily: 'var(--font-inter)', transition: 'all .15s',
                }}
              >За популярністю</button>
            </div>
            {loading && <p style={{ color: "#aaa", padding: 24 }}>Завантаження...</p>}
            {error   && <p style={{ color: "#ff6b6b", padding: 24 }}>Помилка: {error}</p>}
            {!loading && !error && filtered.length === 0 && (
              <p style={{ color: "#777", padding: 24 }}>Постів не знайдено</p>
            )}
            {pagePosts.map(item => (
              <PostCard
                key={item.post.postId}
                item={item}
                userAvatarMap={userAvatarMap}
                onDelete={id => setAllPosts(prev => prev.filter(p => p.post.postId !== id))}
              />
            ))}
            {!loading && !error && filtered.length > PAGE_SIZE && (
              <div className={styles.pagination}>
                <button
                  className={styles.pageBtn}
                  disabled={page === 1}
                  onClick={() => setPage(p => p - 1)}
                >← Назад</button>
                <span className={styles.pageNum}>Сторінка {page} з {totalPages}</span>
                <button
                  className={styles.pageBtn}
                  disabled={page >= totalPages}
                  onClick={() => setPage(p => p + 1)}
                >Далі →</button>
              </div>
            )}
          </div>
          <div className={styles.sideCol}>
            <div className={styles.sideTop}>
              <button className={styles.newPostBtn} onClick={() => navigate("/posts/new")}>
                + Новий проєкт
              </button>
              <FrameComponent1 />
            </div>
          </div>
        </div>
      </main>
      <div className={styles.moderationWrapper}>
        <Moderation />
      </div>
    </Box>
  );
};

export default Component1;