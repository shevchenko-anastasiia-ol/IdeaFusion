import { FunctionComponent, useEffect, useState } from "react";
import { useNavigate, useParams } from "react-router-dom";
import FrameComponent2 from "../components/FrameComponent2";
import { teamsApi } from "../api/teams";
import { useAuth } from "../context/AuthContext";
import { aggregatorApi } from "../api/aggregator";
import { postauthorApi } from "../api/posts";
import type { TeamEntity, UserDto, PostWithEngagementDto } from "../api/types";
import { teamStatusLabel } from "../api/types";
import { api } from "../api/client";
import styles from "./UserPublicPage.module.css";

const PALETTE = ["#7c5cfc","#ff6b6b","#3fcca0","#ffb347","#29b6f6","#e040fb","#ef5350","#66bb6a","#26c6da","#ec407a"];
const BG_PALETTE = ["#3fcca0","#f87c6b","#7c5cfc","#4a4a6a","#ffb347","#5bc8f5","#c756ff"];

function accentFor(seed: string): string {
  let h = 0;
  for (let i = 0; i < seed.length; i++) h = (h * 31 + seed.charCodeAt(i)) >>> 0;
  return PALETTE[h % PALETTE.length];
}
function bgFor(id: number) { return BG_PALETTE[id % BG_PALETTE.length]; }

function getMediaType(url: string): 'image' | 'video' | 'audio' | 'pdf' {
  const ext = url.split('?')[0].split('.').pop()?.toLowerCase() ?? '';
  if (['mp4', 'webm', 'mov', 'avi', 'mkv'].includes(ext)) return 'video';
  if (['mp3', 'wav', 'ogg', 'aac', 'm4a', 'flac'].includes(ext)) return 'audio';
  if (ext === 'pdf') return 'pdf';
  return 'image';
}
function cardMedia(urls: string[], fallbackBg: string) {
  const img = urls.find(u => getMediaType(u) === 'image');
  if (img) return { style: { backgroundImage: `url("${img}")`, backgroundSize: 'cover', backgroundPosition: 'center' }, icon: null };
  const t = urls[0] ? getMediaType(urls[0]) : null;
  return { style: { backgroundColor: fallbackBg, display: 'flex', alignItems: 'center', justifyContent: 'center' },
           icon: t === 'video' ? '🎬' : t === 'audio' ? '🎵' : t === 'pdf' ? '📄' : null };
}

const FILTERS = ["Всі", "Дизайн", "Музика", "Арт", "Анімація"];
const TAG_MAP: Record<string, string[]> = {
  "Дизайн":   ["design", "branding", "ui-ux"],
  "Музика":   ["music"],
  "Арт":      ["art", "illustration", "street-art"],
  "Анімація": ["animation"],
};

const UserPublicPage: FunctionComponent = () => {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const { user: me } = useAuth();

  const [profile,  setProfile]  = useState<UserDto | null>(null);
  const [teams,    setTeams]    = useState<TeamEntity[]>([]);
  const [posts,    setPosts]    = useState<PostWithEngagementDto[]>([]);
  const [loading,  setLoading]  = useState(true);
  const [notFound, setNotFound] = useState(false);
  const [activeFilter, setActiveFilter] = useState("Всі");

  useEffect(() => {
    if (!id) return;
    setLoading(true);
    setNotFound(false);
    setProfile(null);
    setTeams([]);
    setPosts([]);

    Promise.all([
      api.get<UserDto>(`/api/users/${id}`).catch(() => null),
      teamsApi.getByMember(id).catch(() => []),
    ]).then(async ([u, t]) => {
      if (!u) {
        setNotFound(true);
        setTeams(t as TeamEntity[]);
        return;
      }
      setProfile(u);
      setTeams(t as TeamEntity[]);

      // Fetch portfolio: personal posts + team posts, using the portfolio endpoint
      const userName = u.userName ?? u.email.split('@')[0];
      try {
        const userContentId = await postauthorApi.getByUserName(userName, u.avatarUrl ?? undefined);
        const userPosts = await aggregatorApi.getPortfolio(userContentId, u.id);
        setPosts(userPosts);
      } catch {
        // No posts available — not an error
      }
    }).finally(() => setLoading(false));
  }, [id]);

  if (loading) {
    return (
      <div className={styles.page}>
        <FrameComponent2 />
        <div className={styles.content}>
          <p className={styles.msg}>Завантаження...</p>
        </div>
      </div>
    );
  }

  if (notFound || !profile) {
    return (
      <div className={styles.page}>
        <FrameComponent2 />
        <div className={styles.content}>
          <p className={styles.msg}>Користувача не знайдено</p>
        </div>
      </div>
    );
  }

  const isMe = me?.id === id;
  const displayName = profile.fullName ?? profile.userName ?? profile.email.split("@")[0];
  const initials    = displayName.slice(0, 2).toUpperCase();
  const color       = accentFor(profile.id);

  const filtered = activeFilter === "Всі"
    ? posts
    : posts.filter(p => {
        const mapped = TAG_MAP[activeFilter] ?? [activeFilter.toLowerCase()];
        return p.post.tags.some(t => mapped.includes(t.toLowerCase()));
      });

  return (
    <div className={styles.page}>
      <FrameComponent2 />

      <div className={styles.content}>
        {/* Header */}
        <header className={styles.header}>
          <button className={styles.backBtn} onClick={() => navigate(-1)}>← Назад</button>
          <h2 className={styles.headerTitle}>Профіль користувача</h2>
        </header>

        {/* Profile card */}
        <div className={styles.profileCard}>
          {profile.avatarUrl ? (
            <img src={profile.avatarUrl} alt="" className={styles.profileAvatar} style={{ objectFit: "cover" }} />
          ) : (
            <div className={styles.profileAvatar} style={{ backgroundColor: color }}>{initials}</div>
          )}
          <div className={styles.profileInfo}>
            <h1 className={styles.profileName}>{displayName}</h1>
            {profile.specialization && (
              <p className={styles.profileSub}>{profile.specialization}</p>
            )}
            <p className={styles.profileSub}>{profile.email}</p>
          </div>
          {isMe ? (
            <button className={styles.actionBtn} onClick={() => navigate("/personal")}>
              Редагувати профіль
            </button>
          ) : (
            <button
              className={styles.actionBtn}
              onClick={() => navigate(`/team/invite-to-team?userId=${profile.id}`)}
            >
              Запросити до команди
            </button>
          )}
        </div>

        <div className={styles.body}>
          {/* Info section */}
          <div className={styles.infoSection}>
            <div className={styles.infoCard}>
              <h3 className={styles.cardTitle}>Інформація</h3>
              <div className={styles.infoRow}>
                <span className={styles.infoLabel}>Логін</span>
                <span className={styles.infoValue}>{profile.userName ?? "—"}</span>
              </div>
              <div className={styles.infoRow}>
                <span className={styles.infoLabel}>Email</span>
                <span className={styles.infoValue}>{profile.email}</span>
              </div>
              {profile.specialization && (
                <div className={styles.infoRow}>
                  <span className={styles.infoLabel}>Спеціалізація</span>
                  <span className={styles.infoValue}>{profile.specialization}</span>
                </div>
              )}
              <div className={styles.infoRow}>
                <span className={styles.infoLabel}>Ролі</span>
                <span className={styles.infoValue}>{profile.roles.join(", ") || "Користувач"}</span>
              </div>
              <div className={styles.infoRow}>
                <span className={styles.infoLabel}>З нами з</span>
                <span className={styles.infoValue}>
                  {new Date(profile.createdAt).toLocaleDateString("uk-UA", { day: "numeric", month: "long", year: "numeric" })}
                </span>
              </div>
            </div>

            <div className={styles.statsCard}>
              <h3 className={styles.cardTitle}>Статистика</h3>
              <div className={styles.statsGrid}>
                <div className={styles.statItem}>
                  <span className={styles.statNumber}>{posts.length}</span>
                  <span className={styles.statLabel}>Постів</span>
                </div>
                <div className={styles.statItem}>
                  <span className={styles.statNumber}>{teams.length}</span>
                  <span className={styles.statLabel}>Команд</span>
                </div>
                <div className={styles.statItem}>
                  <span className={styles.statNumber}>{posts.reduce((s, p) => s + p.likesCount, 0)}</span>
                  <span className={styles.statLabel}>Лайків</span>
                </div>
                <div className={styles.statItem}>
                  <span className={styles.statNumber}>{posts.reduce((s, p) => s + p.commentsCount, 0)}</span>
                  <span className={styles.statLabel}>Коментарів</span>
                </div>
              </div>
            </div>
          </div>

          {/* Teams section */}
          <div className={styles.section}>
            <h2 className={styles.sectionTitle}>Команди ({teams.length})</h2>
            {teams.length === 0 ? (
              <p className={styles.emptyMsg}>Поки не в жодній команді</p>
            ) : (
              <div className={styles.teamsList}>
                {teams.map(t => {
                  const status = teamStatusLabel(t.status);
                  const accent = accentFor(t.id);
                  const statusStyle =
                    status === "Активна"
                      ? { bg: "rgba(63,204,160,0.15)", border: "1px solid #3fcca0", color: "#3fcca0" }
                      : status === "У пошуку"
                      ? { bg: "rgba(124,92,252,0.15)", border: "1px solid #7c5cfc", color: "#a48fff" }
                      : { bg: "rgba(210,50,50,0.15)", border: "1px solid #e05555", color: "#ff6b6b" };
                  const role = t.members.find(m => m.user.userId === profile.id)?.role;
                  return (
                    <div key={t.id} className={styles.teamCard} onClick={() => navigate(`/teams/${t.id}`)}>
                      {t.avatarUrl ? (
                        <img src={t.avatarUrl} alt="" className={styles.teamAvatar} style={{ objectFit: "cover" }} />
                      ) : (
                        <div className={styles.teamAvatar} style={{ backgroundColor: accent }}>
                          {t.name.slice(0, 2).toUpperCase()}
                        </div>
                      )}
                      <div className={styles.teamInfo}>
                        <div className={styles.teamNameRow}>
                          <span className={styles.teamName}>{t.name}</span>
                          <span className={styles.statusBadge} style={{ background: statusStyle.bg, border: statusStyle.border, color: statusStyle.color }}>
                            {status}
                          </span>
                        </div>
                        <div className={styles.teamMeta}>
                          <span>{t.category}</span>
                          {role && <span style={{ color: "#a48fff" }}>· {role}</span>}
                          <span>· {t.members.length} учасників</span>
                        </div>
                      </div>
                    </div>
                  );
                })}
              </div>
            )}
          </div>

          {/* Portfolio section */}
          <div className={styles.portfolioSection}>
            <div className={styles.portfolioHeader}>
              <h2 className={styles.sectionTitle} style={{ margin: 0 }}>Портфоліо ({posts.length})</h2>
              <div className={styles.filters}>
                {FILTERS.map(f => (
                  <button
                    key={f}
                    className={`${styles.filterBtn} ${activeFilter === f ? styles.filterActive : ""}`}
                    onClick={() => setActiveFilter(f)}
                  >
                    {f}
                  </button>
                ))}
              </div>
            </div>

            <div className={styles.portfolioGrid}>
              {filtered.length === 0 && (
                <p style={{ color: "#777", fontFamily: "var(--font-inter)", fontSize: 14, margin: 0 }}>
                  Постів немає
                </p>
              )}
              {filtered.map(item => {
                const urls = item.post.mediaUrls ?? [];
                const videoUrl = urls.find(u => getMediaType(u) === 'video');
                const imageUrl = urls.find(u => getMediaType(u) === 'image');
                const fallbackBg = bgFor(item.post.postId);
                return (
                <div
                  key={item.post.postId}
                  className={styles.portfolioCard}
                  onClick={() => navigate(`/posts/${item.post.postId}`)}
                >
                  <div
                    className={styles.cardPlate}
                    style={{
                      position: 'relative', overflow: 'hidden',
                      ...(imageUrl && !videoUrl ? { backgroundImage: `url("${imageUrl}")`, backgroundSize: 'cover', backgroundPosition: 'center' } : {}),
                      ...(!videoUrl && !imageUrl ? { backgroundColor: fallbackBg, display: 'flex', alignItems: 'center', justifyContent: 'center' } : {}),
                    }}
                  >
                    {videoUrl && (
                      <video src={videoUrl} muted preload="metadata" style={{ width: '100%', height: '100%', objectFit: 'cover', display: 'block', pointerEvents: 'none' }} />
                    )}
                    {!videoUrl && !imageUrl && urls[0] && (
                      <span style={{ fontSize: 32, opacity: 0.7 }}>
                        {getMediaType(urls[0]) === 'audio' ? '🎵' : '📄'}
                      </span>
                    )}
                  </div>
                  <div className={styles.cardBody}>
                    <div className={styles.cardRow}>
                      <span className={styles.cardTitle2}>{item.post.title}</span>
                      <div style={{ display: "flex", gap: 4, flexShrink: 0 }}>
                        {item.post.collaboration != null && (
                          <span className={styles.cardTag} style={{ color: "#3fcca0", border: "1px solid #3fcca0", background: "rgba(63,204,160,0.15)" }}>
                            Команда
                          </span>
                        )}
                        {item.post.tags[0] && (
                          <span className={styles.cardTag} style={{ color: "#a48fff", border: "1px solid #7c5cfc", background: "rgba(124,92,252,0.15)" }}>
                            {item.post.tags[0]}
                          </span>
                        )}
                      </div>
                    </div>
                    <div className={styles.cardStats}>
                      <span>♥ {item.likesCount}</span>
                      <span>💬 {item.commentsCount}</span>
                      <span>👁 {item.viewsCount}</span>
                    </div>
                  </div>
                </div>
                );
              })}
            </div>
          </div>
        </div>
      </div>
    </div>
  );
};

export default UserPublicPage;