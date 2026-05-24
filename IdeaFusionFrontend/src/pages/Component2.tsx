import { FunctionComponent, useEffect, useState } from "react";
import { Box, Typography } from "@mui/material";
import Moderation from "../components/Moderation";
import SearchBar from "../components/SearchBar";
import GroupComponent11 from "../components/GroupComponent11";
import FrameComponent2 from "../components/FrameComponent2";
import styles from "./Component2.module.css";
import { useNavigate } from "react-router-dom";
import { aggregatorApi } from "../api/aggregator";
import { teamsApi } from "../api/teams";
import { postsApi } from "../api/posts";
import { api } from "../api/client";
import type { PostWithEngagementDto, TeamEntity, UserDto } from "../api/types";
import { teamStatusLabel, timeAgo } from "../api/types";
import { useAuth } from "../context/AuthContext";

export type Component2Type = {};

const PREVIEW_COLORS = ["#2d2d4a","#1a3a2a","#3a1a2a","#1a1a3a","#1a2a3a","#2a1a3a"];
const AVATAR_PALETTE = ["#4002aa","#00c9a7","#ff6b6b","#ffb347","#e040fb","#29b6f6","#7c5cfc"];

function colorFor(id: number, palette: string[]) { return palette[id % palette.length]; }
function getMediaType(url: string): 'image' | 'video' | 'audio' | 'pdf' {
  const ext = url.split('?')[0].split('.').pop()?.toLowerCase() ?? '';
  if (['mp4', 'webm', 'mov', 'avi', 'mkv'].includes(ext)) return 'video';
  if (['mp3', 'wav', 'ogg', 'aac', 'm4a', 'flac'].includes(ext)) return 'audio';
  if (ext === 'pdf') return 'pdf';
  return 'image';
}
function avatarColor(seed: string): string {
  let h = 0;
  for (let i = 0; i < seed.length; i++) h = (h * 31 + seed.charCodeAt(i)) >>> 0;
  return AVATAR_PALETTE[h % AVATAR_PALETTE.length];
}

const match = (query: string, ...fields: string[]) => {
  if (!query.trim()) return true;
  const q = query.toLowerCase();
  return fields.some(f => f.toLowerCase().includes(q));
};

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

const TAG_LABEL: Record<string, string> = {};
for (const [label, slugs] of Object.entries(TAG_MAP)) {
  for (const slug of slugs) TAG_LABEL[slug] = label;
}
function tagLabel(raw: string): string {
  return TAG_LABEL[raw.toLowerCase()] ?? (raw.charAt(0).toUpperCase() + raw.slice(1));
}

const TrashIcon = () => (
  <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
    <polyline points="3 6 5 6 21 6"/><path d="M19 6l-1 14a2 2 0 0 1-2 2H8a2 2 0 0 1-2-2L5 6"/><path d="M10 11v6"/><path d="M14 11v6"/><path d="M9 6V4a1 1 0 0 1 1-1h4a1 1 0 0 1 1 1v2"/>
  </svg>
);

const PostCard: FunctionComponent<{ item: PostWithEngagementDto; onDelete?: (id: number) => void }> = ({ item, onDelete }) => {
  const navigate = useNavigate();
  const { user } = useAuth();
  const isAdmin = user?.roles?.includes('Admin');
  const { post, likesCount, commentsCount } = item;
  const [liked,   setLiked]   = useState(item.isLikedByCurrentUser);
  const [starred, setStarred] = useState(item.isSavedByCurrentUser);

  const isCollab   = !!post.collaboration;
  const authorName = post.collaboration?.name ?? post.author?.userName ?? "Невідомий";
  const tag        = tagLabel(post.tags[0] ?? "Інше");
  const avatarUrl  = post.collaboration?.avatarUrl ?? post.author?.avatarUrl;

  const heartFilter = "brightness(0) saturate(100%) invert(27%) sepia(96%) saturate(1600%) hue-rotate(320deg)";
  const starFilter  = "brightness(0) saturate(100%) invert(75%) sepia(60%) saturate(600%) hue-rotate(20deg)";

  return (
    <Box onClick={() => navigate(`/posts/${post.postId}`)} sx={{
      background: "#181818", borderRadius: "24px", overflow: "hidden",
      border: "1px solid #222", cursor: "pointer", display: "flex",
      flexDirection: "column", transition: "transform .15s",
      "&:hover": { transform: "translateY(-3px)" },
    }}>
      {post.mediaUrls.length > 0 ? (() => {
        const imageUrl = post.mediaUrls.find(u => getMediaType(u) === 'image');
        if (imageUrl) return (
          <Box component="img" src={imageUrl} alt={post.title}
            sx={{ width: "100%", aspectRatio: "4/5", maxHeight: 300, objectFit: "cover", display: "block", flexShrink: 0 }} />
        );
        const url = post.mediaUrls[0];
        const type = getMediaType(url);
        if (type === 'video') return (
          <Box component="video" src={url} muted preload="metadata"
            sx={{ width: "100%", aspectRatio: "4/5", maxHeight: 300, objectFit: "cover", display: "block", flexShrink: 0 }} />
        );
        if (type === 'audio') return (
          <Box sx={{ aspectRatio: "4/5", maxHeight: 300, backgroundColor: colorFor(post.postId, PREVIEW_COLORS),
            display: "flex", alignItems: "center", justifyContent: "center", flexShrink: 0 }}>
            <Box sx={{ fontSize: 32 }}>🎵</Box>
          </Box>
        );
        if (type === 'pdf') return (
          <Box sx={{ aspectRatio: "4/5", maxHeight: 300, backgroundColor: colorFor(post.postId, PREVIEW_COLORS),
            display: "flex", alignItems: "center", justifyContent: "center", flexShrink: 0 }}>
            <Box sx={{ fontSize: 32 }}>📄</Box>
          </Box>
        );
        return (
          <Box sx={{ aspectRatio: "4/5", maxHeight: 300, backgroundColor: colorFor(post.postId, PREVIEW_COLORS), flexShrink: 0 }} />
        );
      })() : (
        <Box sx={{ aspectRatio: "4/5", maxHeight: 300, backgroundColor: colorFor(post.postId, PREVIEW_COLORS), flexShrink: 0 }} />
      )}
      <Box sx={{ p: "14px 18px 12px", display: "flex", flexDirection: "column", gap: "10px" }}>
        <Box sx={{ display: "flex", alignItems: "center", justifyContent: "space-between", gap: "10px" }}>
          <Box sx={{ display: "flex", alignItems: "center", gap: "10px" }}>
            {avatarUrl ? (
              <Box component="img" src={avatarUrl} alt={authorName}
                sx={{ width: 36, height: 36, borderRadius: "50%", objectFit: "cover", flexShrink: 0 }} />
            ) : (
              <Box sx={{ width: 36, height: 36, borderRadius: "50%",
                backgroundColor: colorFor(post.author?.userId ?? post.postId, AVATAR_PALETTE),
                color: "#fff", fontSize: 12, fontWeight: 700,
                display: "flex", alignItems: "center", justifyContent: "center", flexShrink: 0 }}>
                {authorName.slice(0, 2).toUpperCase()}
              </Box>
            )}
            <Box>
              <Box sx={{ fontSize: 13, fontWeight: 600, color: "#fff", fontFamily: "var(--font-inter)" }}>{authorName}</Box>
              <Box sx={{ fontSize: 11, color: "#666", fontFamily: "var(--font-inter)" }}>{timeAgo(post.createdAt)}</Box>
            </Box>
          </Box>
          <Box sx={{ display: "flex", alignItems: "center", gap: "6px", flexShrink: 0 }}>
            {isCollab && (
              <Box sx={{ fontSize: 11, px: "10px", py: "2px", borderRadius: "30px",
                background: "rgba(124,92,252,.1)", border: "1px solid #7c5cfc", color: "#a48fff",
                fontFamily: "var(--font-inter)", whiteSpace: "nowrap" }}>
                Команда
              </Box>
            )}
            <Box sx={{ fontSize: 11, px: "10px", py: "2px", borderRadius: "30px",
              background: "rgba(124,92,252,.15)", border: "1px solid #7c5cfc", color: "#a48fff",
              fontFamily: "var(--font-inter)", whiteSpace: "nowrap" }}>
              {tag}
            </Box>
          </Box>
        </Box>
        <Box sx={{ fontSize: 15, fontWeight: 700, color: "#fff", fontFamily: "var(--font-inter)", lineHeight: 1.3 }}>{post.title}</Box>
        <Box sx={{ display: "flex", alignItems: "center", gap: "16px" }}>
          {[
            { src: "/icon-heart-1.svg",   val: likesCount,    toggle: () => setLiked(!liked),     active: liked,    filter: heartFilter },
            { src: "/icon-comment-1.svg", val: commentsCount, toggle: null,                        active: false,    filter: undefined   },
            { src: "/Star-Icon.svg",       val: item.viewsCount, toggle: () => setStarred(!starred), active: starred, filter: starFilter  },
          ].map(({ src, val, toggle, active, filter }) => (
            <Box key={src} sx={{ display: "flex", alignItems: "center", gap: "5px", fontSize: 14, color: "#666", fontWeight: 300 }}>
              <img src={src} alt="" style={{ width: 20, height: 20, cursor: toggle ? "pointer" : "default",
                filter: active ? filter : undefined }}
                onClick={(e) => { if (toggle) { e.stopPropagation(); toggle(); } }} />
              {val}
            </Box>
          ))}
          {isAdmin && (
            <Box component="button"
              title="Видалити пост"
              onClick={async e => {
                e.stopPropagation();
                if (!confirm('Видалити цей пост назавжди?')) return;
                try { await postsApi.delete(post.postId); onDelete?.(post.postId); }
                catch (ex: any) { alert(ex.message ?? 'Помилка видалення'); }
              }}
              sx={{ ml: "auto", background: "none", border: "none", cursor: "pointer",
                color: "#ff6b6b", p: "2px 6px", display: "flex", alignItems: "center" }}>
              <TrashIcon />
            </Box>
          )}
        </Box>
      </Box>
    </Box>
  );
};

const UserCard: FunctionComponent<{ user: UserDto; meId?: string; isAdmin?: boolean; onDelete?: (id: string) => void }> = ({ user, meId, isAdmin, onDelete }) => {
  const navigate    = useNavigate();
  const displayName = user.fullName ?? user.userName ?? user.email.split('@')[0];
  const initials    = displayName.slice(0, 2).toUpperCase();
  const role        = user.specialization ?? user.roles.join(', ') ?? 'Користувач';
  const isMe        = user.id === meId;

  return (
    <Box onClick={() => navigate(`/users/${user.id}`)} sx={{
      background: "#181818", borderRadius: "14px", border: "1px solid #222",
      p: "14px 18px", display: "flex", alignItems: "center", gap: "12px",
      cursor: "pointer", transition: "background .15s",
      "&:hover": { background: "#1e1e1e" },
    }}>
      {user.avatarUrl ? (
        <Box component="img" src={user.avatarUrl} alt="" sx={{ width: 44, height: 44, borderRadius: "50%", objectFit: "cover", flexShrink: 0 }} />
      ) : (
        <Box sx={{ width: 44, height: 44, borderRadius: "50%", backgroundColor: avatarColor(user.id),
          color: "#fff", fontSize: 14, fontWeight: 700, display: "flex",
          alignItems: "center", justifyContent: "center", flexShrink: 0 }}>
          {initials}
        </Box>
      )}
      <Box sx={{ flex: 1, minWidth: 0 }}>
        <Box sx={{ fontSize: 14, fontWeight: 700, color: "#f0f0f0", fontFamily: "var(--font-inter)",
          overflow: "hidden", textOverflow: "ellipsis", whiteSpace: "nowrap" }}>{displayName}</Box>
        <Box sx={{ fontSize: 12, color: "#777", fontWeight: 300, fontFamily: "var(--font-inter)" }}>{role}</Box>
      </Box>
      {isAdmin && !isMe && !user.roles?.includes('Admin') && (
        <Box component="button"
          title="Видалити користувача"
          onClick={e => { e.stopPropagation(); onDelete?.(user.id); }}
          sx={{ background: "none", border: "none", cursor: "pointer", color: "#ff6b6b", p: "4px 8px", flexShrink: 0 }}>
          <TrashIcon />
        </Box>
      )}
    </Box>
  );
};

const TeamCard: FunctionComponent<{ team: TeamEntity; isAdmin?: boolean; onDelete?: (id: string) => void }> = ({ team, isAdmin, onDelete }) => {
  const navigate = useNavigate();
  const status   = teamStatusLabel(team.status);
  const isActive = status === "Активна";

  return (
    <Box onClick={() => navigate(`/teams/${team.id}`)} sx={{
      position: "relative", background: "#181818", borderRadius: "20px", border: "1px solid #222",
      p: "18px 22px", display: "flex", flexDirection: "column", gap: "10px",
      cursor: "pointer", transition: "background .15s",
      "&:hover": { background: "#1e1e1e" },
    }}>
      {isAdmin && (
        <Box component="button"
          title="Видалити команду"
          onClick={e => { e.stopPropagation(); onDelete?.(team.id); }}
          sx={{ position: "absolute", top: 10, right: 10, background: "none", border: "none",
            cursor: "pointer", color: "#ff6b6b", p: "4px", zIndex: 2 }}>
          <TrashIcon />
        </Box>
      )}
      <Box sx={{ display: "flex", alignItems: "center", justifyContent: "space-between", gap: "10px", pr: isAdmin ? "28px" : 0 }}>
        <Box sx={{ fontSize: 17, fontWeight: 700, color: "#fff", fontFamily: "var(--font-inter)" }}>{team.name}</Box>
        <Box sx={{ fontSize: 11, px: "10px", py: "2px", borderRadius: "30px",
          background: isActive ? "rgba(0,201,167,.15)" : "rgba(124,92,252,.15)",
          border: `1px solid ${isActive ? "#00c9a7" : "#7c5cfc"}`,
          color: isActive ? "#00c9a7" : "#a48fff",
          whiteSpace: "nowrap", flexShrink: 0 }}>
          {status}
        </Box>
      </Box>
      <Box sx={{ fontSize: 12, color: "#666", fontFamily: "var(--font-inter)" }}>{team.tags.join(' · ')}</Box>
      <Box sx={{ display: "flex", alignItems: "center", justifyContent: "space-between" }}>
        <Box sx={{ display: "flex", gap: "6px" }}>
          {team.members.slice(0, 5).map((m, i) => (
            <Box key={i} sx={{ width: 28, height: 28, borderRadius: "50%", background: "#4002aa",
              color: "#fff", fontSize: 10, fontWeight: 700, display: "flex",
              alignItems: "center", justifyContent: "center" }}>
              {m.user.username.slice(0, 2).toUpperCase()}
            </Box>
          ))}
        </Box>
        <Box sx={{ fontSize: 12, color: "#555" }}>{team.members.length} учасників</Box>
      </Box>
    </Box>
  );
};

const SectionHeader: FunctionComponent<{ title: string; linkLabel: string; onLink: () => void }> = ({ title, linkLabel, onLink }) => (
  <Box sx={{ display: "flex", alignItems: "center", justifyContent: "space-between", mb: "4px" }}>
    <Typography variant="inherit" variantMapping={{ inherit: "h3" }}
      sx={{ fontWeight: 400, fontSize: "var(--fs-24)", color: "var(--color-darkslategray-100)", fontFamily: "var(--font-inter)" }}>
      {title}
    </Typography>
    <Typography variant="inherit" variantMapping={{ inherit: "h3" }}
      onClick={onLink}
      sx={{ fontWeight: 400, fontSize: "var(--fs-24)", color: "var(--color-mediumslateblue-200)",
        fontFamily: "var(--font-inter)", cursor: "pointer" }}>
      {linkLabel}
    </Typography>
  </Box>
);

const Component2: FunctionComponent<Component2Type> = () => {
  const navigate = useNavigate();
  const { user: me } = useAuth();
  const isAdmin = me?.roles?.includes('Admin');

  const [query,          setQuery]          = useState("");
  const [activeType,     setActiveType]     = useState("Всі");
  const [activeCategory, setActiveCategory] = useState("Всі");

  const [posts,   setPosts]   = useState<PostWithEngagementDto[]>([]);
  const [teams,   setTeams]   = useState<TeamEntity[]>([]);
  const [users,   setUsers]   = useState<UserDto[]>([]);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    const PAGE = 200;
    async function fetchAllPosts(): Promise<PostWithEngagementDto[]> {
      const all: PostWithEngagementDto[] = [];
      let skip = 0;
      while (true) {
        const batch = await aggregatorApi.getFeed(undefined, skip, PAGE);
        all.push(...batch);
        if (batch.length < PAGE) break;
        skip += PAGE;
      }
      return all;
    }

    Promise.all([
      fetchAllPosts(),
      teamsApi.getPaged().then(r => Array.isArray(r) ? r : (r.items ?? [])),
      api.get<UserDto[]>('/api/users').catch(() => [] as UserDto[]),
    ]).then(([p, t, u]) => {
      setPosts(p);
      setTeams(t);
      setUsers(u);
    }).catch(() => {}).finally(() => setLoading(false));
  }, []);

  async function handleDeleteUser(userId: string) {
    if (!confirm('Видалити цього користувача назавжди?')) return;
    try {
      await api.delete<void>(`/api/users/${userId}`);
      setUsers(prev => prev.filter(u => u.id !== userId));
    } catch (e: any) {
      alert(e.message ?? 'Помилка видалення');
    }
  }

  async function handleDeleteTeam(id: string) {
    if (!confirm('Видалити цю команду назавжди?')) return;
    try {
      await teamsApi.deleteTeam(id);
      setTeams(prev => prev.filter(t => t.id !== id));
    } catch (e: any) {
      alert(e.message ?? 'Помилка видалення');
    }
  }

  const showPosts = activeType === "Всі" || activeType === "Пости";
  const showUsers = activeType === "Всі" || activeType === "Користувачі";
  const showTeams = activeType === "Всі" || activeType === "Команди";

  const filteredPosts = posts.filter(p => {
    const mapped = activeCategory === "Всі" ? null : (TAG_MAP[activeCategory] ?? [activeCategory.toLowerCase()]);
    const categoryOk = !mapped || p.post.tags.some(t => mapped.includes(t.toLowerCase()));
    return categoryOk && match(query, p.post.title, p.post.description ?? "", p.post.tags.join(' '), p.post.author?.userName ?? "", p.post.collaboration?.name ?? "");
  });
  const filteredUsers = users.filter(u => match(query, u.userName ?? "", u.fullName ?? "", u.email ?? "", u.specialization ?? ""));
  const filteredTeams = teams.filter(t => match(query, t.name, t.description ?? "", t.category ?? "", t.tags.join(' ')));

  return (
    <Box className={styles.div}>
      <Box className={styles.child} />
      <FrameComponent2 />
      <main className={styles.postsArea}>
        <section className={styles.filterRegionParent}>
          <SearchBar value={query} onChange={setQuery} />
          <GroupComponent11
            onTypeChange={setActiveType}
            onCategoryChange={setActiveCategory}
          />
        </section>

        <section className={styles.postsList}>
          {loading && <Box sx={{ color: "#aaa", fontSize: 15, p: "24px" }}>Завантаження...</Box>}

          {!loading && showPosts && (
            <Box sx={{ width: "100%" }}>
              <SectionHeader title="Пости" linkLabel="Всі пости →" onLink={() => navigate("/posts")} />
              <Box className={styles.searchCategorySeparatorInline} />
              {filteredPosts.length > 0 ? (
                <Box sx={{ display: "grid", gridTemplateColumns: { xs: "1fr", sm: "repeat(2, 1fr)", md: "repeat(3, 1fr)" }, gap: "16px", mt: "16px" }}>
                  {filteredPosts.slice(0, 6).map(p => <PostCard key={p.post.postId} item={p} onDelete={id => setPosts(prev => prev.filter(x => x.post.postId !== id))} />)}
                </Box>
              ) : (
                <Box sx={{ color: "#555", fontSize: 15, fontFamily: "var(--font-inter)", mt: "16px", mb: "8px" }}>
                  Нічого не знайдено
                </Box>
              )}
            </Box>
          )}

          {!loading && showUsers && (
            <Box sx={{ width: "100%", mt: "32px" }}>
              <SectionHeader title="Користувачі" linkLabel="Всі користувачі →" onLink={() => navigate("/users")} />
              <Box className={styles.searchCategorySeparatorInline} />
              {filteredUsers.length > 0 ? (
                <Box sx={{ display: "grid", gridTemplateColumns: { xs: "1fr", sm: "repeat(2, 1fr)", md: "repeat(3, 1fr)" }, gap: "12px", mt: "16px" }}>
                  {filteredUsers.slice(0, 6).map(u => <UserCard key={u.id} user={u} meId={me?.id} isAdmin={isAdmin} onDelete={handleDeleteUser} />)}
                </Box>
              ) : (
                <Box sx={{ color: "#555", fontSize: 15, fontFamily: "var(--font-inter)", mt: "16px", mb: "8px" }}>
                  Нічого не знайдено
                </Box>
              )}
            </Box>
          )}

          {!loading && showTeams && (
            <Box sx={{ width: "100%", mt: "32px" }}>
              <SectionHeader title="Команди" linkLabel="Всі команди →" onLink={() => navigate("/teams")} />
              <Box className={styles.searchCategorySeparatorInline} />
              {filteredTeams.length > 0 ? (
                <Box sx={{ display: "grid", gridTemplateColumns: { xs: "1fr", sm: "repeat(2, 1fr)" }, gap: "16px", mt: "16px" }}>
                  {filteredTeams.slice(0, 6).map(t => <TeamCard key={t.id} team={t} isAdmin={isAdmin} onDelete={handleDeleteTeam} />)}
                </Box>
              ) : (
                <Box sx={{ color: "#555", fontSize: 15, fontFamily: "var(--font-inter)", mt: "16px", mb: "8px" }}>
                  Нічого не знайдено
                </Box>
              )}
            </Box>
          )}
        </section>
        <Moderation />
      </main>
    </Box>
  );
};

export default Component2;