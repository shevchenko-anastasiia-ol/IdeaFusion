import { FunctionComponent, useEffect, useRef, useState } from "react";
import { useNavigate, useParams } from "react-router-dom";
import FrameComponent2 from "../components/FrameComponent2";
import Moderation from "../components/Moderation";
import { teamsApi, collaborationRequestsApi } from "../api/teams";
import { aggregatorApi } from "../api/aggregator";
import { authApi } from "../api/auth";
import { useAuth } from "../context/AuthContext";
import { api } from "../api/client";
import type {
  AggregatorTeamPostDto,
  CollaborationRequestEntity,
  TeamEntity,
  UserDto,
} from "../api/types";
import { initials, teamStatusLabel, timeAgo } from "../api/types";
import styles from "./TeamDetailPage.module.css";

const PALETTE = ["#7c5cfc", "#ff6b6b", "#3fcca0", "#ffb347", "#29b6f6", "#e040fb"];
function accentFor(seed: string): string {
  let h = 0;
  for (let i = 0; i < seed.length; i++) h = (h * 31 + seed.charCodeAt(i)) >>> 0;
  return PALETTE[h % PALETTE.length];
}

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

const STATUS_OPTIONS = [
  { value: 0, label: "Активна" },
  { value: 1, label: "У пошуку" },
  { value: 2, label: "Завершена" },
];

function statusNum(status: number | string): number {
  if (typeof status === "number") return status;
  if (status === "Active") return 0;
  if (status === "Searching") return 1;
  return 2;
}

const TeamDetailPage: FunctionComponent = () => {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const { user } = useAuth();

  const [team, setTeam] = useState<TeamEntity | null>(null);
  const [posts, setPosts] = useState<AggregatorTeamPostDto[]>([]);
  const [requests, setRequests] = useState<CollaborationRequestEntity[]>([]);
  const [myRequest, setMyRequest] = useState<CollaborationRequestEntity | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [requestsError, setRequestsError] = useState<string | null>(null);

  // Edit state
  const [editName, setEditName] = useState("");
  const [editDesc, setEditDesc] = useState("");
  const [editCategory, setEditCategory] = useState("");
  const [editTags, setEditTags] = useState("");
  const [editStatus, setEditStatus] = useState(1);
  const [saving, setSaving] = useState(false);
  const [saveMsg, setSaveMsg] = useState<string | null>(null);

  // Avatar upload
  const avatarInputRef = useRef<HTMLInputElement>(null);
  const [avatarUploading, setAvatarUploading] = useState(false);

  // Add required role state
  const [newRole, setNewRole] = useState("");
  const [showAddRole, setShowAddRole] = useState(false);
  const [userAvatarMap, setUserAvatarMap] = useState<Map<string, string>>(new Map());
  const [userEmailMap,  setUserEmailMap]  = useState<Map<string, string>>(new Map());

  useEffect(() => {
    api.get<UserDto[]>('/api/users')
      .then(users => {
        const avatarMap = new Map<string, string>();
        const emailMap  = new Map<string, string>();
        for (const u of users) {
          if (u.userName && u.avatarUrl) avatarMap.set(u.userName, u.avatarUrl);
          emailMap.set(u.id, u.email);
        }
        setUserAvatarMap(avatarMap);
        setUserEmailMap(emailMap);
      })
      .catch(() => {});
  }, []);

  const isOwner = !!(
    team &&
    user &&
    team.members.some((m) => m.user.userId === user.id && m.role === "Owner")
  );
  const isMember = !!(
    team && user && team.members.some((m) => m.user.userId === user.id)
  );

  // Reset all state and reload when team ID changes
  useEffect(() => {
    if (!id) return;
    setLoading(true);
    setError(null);
    setTeam(null);
    setPosts([]);
    setRequests([]);
    setMyRequest(null);

    Promise.all([
      teamsApi.getById(id),
      aggregatorApi.getTeamFull(id).catch(() => null),
    ])
      .then(([t, full]) => {
        setTeam(t);
        setEditName(t.name);
        setEditDesc(t.description);
        setEditCategory(t.category);
        setEditTags(t.tags.join(", "));
        setEditStatus(statusNum(t.status));
        if (full) setPosts(full.posts);

      })
      .catch((e) => setError(e.message))
      .finally(() => setLoading(false));
  }, [id]);

  // Check if current visitor has a pending request for this team
  useEffect(() => {
    if (!id || !user || !team || isMember) { setMyRequest(null); return; }
    collaborationRequestsApi.getByUser(user.id)
      .then((rs) => {
        const pending = rs.find(
          (r) => r.teamId === id && (r.status === 0 || r.status === "Pending")
        );
        setMyRequest(pending ?? null);
      })
      .catch(() => setMyRequest(null));
  }, [id, user, team, isMember]);

  function loadRequests() {
    if (!id || !isOwner) return;
    setRequestsError(null);
    collaborationRequestsApi
      .getByTeam(id)
      .then((rs) =>
        setRequests(rs.filter((r) => r.status === 0 || r.status === "Pending"))
      )
      .catch((e) => setRequestsError(e?.message ?? 'Помилка завантаження запитів'));
  }

  // Load pending requests for owner
  useEffect(() => {
    loadRequests();
  // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [id, isOwner]);

  async function handleSave() {
    if (!team || !user) return;
    setSaving(true);
    setSaveMsg(null);
    try {
      const tags = editTags
        .split(",")
        .map((t) => t.trim())
        .filter(Boolean);
      const updated = await teamsApi.update(team.id, {
        teamId: team.id,
        name: editName,
        description: editDesc,
        category: editCategory,
        tags,
        userId: user.id,
      });
      if (editStatus !== statusNum(team.status)) {
        await teamsApi.setStatus(team.id, {
          teamId: team.id,
          status: editStatus,
          userId: user.id,
        });
      }
      setTeam({ ...updated, status: editStatus });
      setSaveMsg("Збережено!");
    } catch (e: unknown) {
      setSaveMsg(e instanceof Error ? e.message : "Помилка збереження");
    } finally {
      setSaving(false);
      setTimeout(() => setSaveMsg(null), 3000);
    }
  }

  async function handleAvatarChange(e: React.ChangeEvent<HTMLInputElement>) {
    const file = e.target.files?.[0];
    if (!file || !team || !user) return;
    setAvatarUploading(true);
    try {
      const { url } = await authApi.uploadFile(file);
      const updated = await teamsApi.setAvatarUrl(team.id, {
        teamId: team.id,
        avatarUrl: url,
        userId: user.id,
      });
      setTeam(updated);
    } catch (e: unknown) {
      alert(e instanceof Error ? e.message : "Помилка завантаження фото");
    } finally {
      setAvatarUploading(false);
      if (avatarInputRef.current) avatarInputRef.current.value = "";
    }
  }

  async function handleRemoveMember(memberId: string) {
    if (!team || !user) return;
    if (!confirm("Видалити учасника з команди?")) return;
    try {
      const updated = await teamsApi.removeMember(team.id, memberId, user.id);
      setTeam(updated);
    } catch (e: unknown) {
      alert(e instanceof Error ? e.message : "Помилка");
    }
  }

  async function handleAddRole() {
    if (!team || !user || !newRole.trim()) return;
    try {
      const updated = await teamsApi.addRequiredRole(team.id, {
        teamId: team.id,
        role: newRole.trim(),
        userId: user.id,
      });
      setTeam(updated);
      setNewRole("");
      setShowAddRole(false);
    } catch (e: unknown) {
      alert(e instanceof Error ? e.message : "Помилка");
    }
  }

  async function handleAcceptRequest(reqId: string) {
    if (!user || !id) return;
    try {
      await collaborationRequestsApi.accept(reqId, { requestId: reqId, userId: user.id });
      setRequests((rs) => rs.filter((r) => r.id !== reqId));
      // Reload team so new member appears in the list
      const updated = await teamsApi.getById(id);
      setTeam(updated);
    } catch (e: unknown) {
      alert(e instanceof Error ? e.message : "Помилка");
    }
  }

  async function handleRejectRequest(reqId: string) {
    if (!user) return;
    try {
      await collaborationRequestsApi.reject(reqId, { requestId: reqId, userId: user.id });
      setRequests((rs) => rs.filter((r) => r.id !== reqId));
    } catch (e: unknown) {
      alert(e instanceof Error ? e.message : "Помилка");
    }
  }

  if (loading) {
    return (
      <div className={styles.page}>
        <FrameComponent2 />
        <p className={styles.loading}>Завантаження...</p>
      </div>
    );
  }
  if (error || !team) {
    return (
      <div className={styles.page}>
        <FrameComponent2 />
        <p className={styles.loading}>Команду не знайдено</p>
      </div>
    );
  }

  const leader = team.members.find((m) => m.role === "Owner");
  const teamInits = initials(team.name);
  const accent = accentFor(team.id);

  // ── OWNER VIEW ─────────────────────────────────────────────────────────────
  if (isOwner) {
    return (
      <div className={styles.page}>
        <FrameComponent2 />
        <div className={styles.content}>

          <div className={styles.pageHeader}>
            <h2 className={styles.pageTitle}>Особиста сторінка команди</h2>
          </div>

          {/* Profile card with avatar upload */}
          <div className={styles.profileCard}>
            <div className={styles.avatarWrapper}>
              {team.avatarUrl ? (
                <img src={team.avatarUrl} alt="" className={styles.profileAvatar} style={{ objectFit: "cover" }} />
              ) : (
                <div className={styles.profileAvatar} style={{ backgroundColor: accent }}>{teamInits}</div>
              )}
              <button
                className={styles.avatarEditBtn}
                onClick={() => avatarInputRef.current?.click()}
                disabled={avatarUploading}
                title="Змінити фото команди"
              >
                {avatarUploading ? "…" : "✎"}
              </button>
              <input
                ref={avatarInputRef}
                type="file"
                accept="image/jpeg,image/png,image/webp,image/gif"
                style={{ display: "none" }}
                onChange={handleAvatarChange}
              />
            </div>
            <div className={styles.profileInfo}>
              <div className={styles.profileRow}>
                <h1 className={styles.profileName}>{team.name}</h1>
                <span className={styles.profileTag}>{team.category}</span>
              </div>
              <p className={styles.profileSub}>{team.tags.join(" · ") || team.description.slice(0, 80)}</p>
            </div>
          </div>

          <div className={styles.main}>
            {/* Left column */}
            <div className={styles.leftCol}>
              <div className={styles.card}>
                <h3 className={styles.cardTitle}>Інформація про команду</h3>
                <div className={styles.field}>
                  <label className={styles.fieldLabel}>Назва</label>
                  <input className={styles.input} value={editName} onChange={(e) => setEditName(e.target.value)} />
                </div>
                <div className={styles.field}>
                  <label className={styles.fieldLabel}>Опис</label>
                  <textarea className={styles.textarea} rows={4} value={editDesc} onChange={(e) => setEditDesc(e.target.value)} />
                </div>
                <div className={styles.field}>
                  <label className={styles.fieldLabel}>Теги (через кому)</label>
                  <input className={styles.input} value={editTags} onChange={(e) => setEditTags(e.target.value)} />
                </div>
                <div className={styles.field}>
                  <label className={styles.fieldLabel}>Статус</label>
                  <select className={styles.input} value={editStatus} onChange={(e) => setEditStatus(Number(e.target.value))}>
                    {STATUS_OPTIONS.map((o) => (
                      <option key={o.value} value={o.value}>{o.label}</option>
                    ))}
                  </select>
                </div>
                <div className={styles.field}>
                  <label className={styles.fieldLabel}>Категорія</label>
                  <input className={styles.input} value={editCategory} onChange={(e) => setEditCategory(e.target.value)} />
                </div>
                <button className={styles.saveBtn} onClick={handleSave} disabled={saving}>
                  {saving ? "Збереження..." : "Зберегти зміни"}
                </button>
                {saveMsg && <span className={styles.saveMsg}>{saveMsg}</span>}
              </div>

              {/* Pending requests — always visible for owner */}
              <div className={styles.card}>
                <div className={styles.requestsHeader}>
                  <span style={{ color: "#ccc", fontWeight: 600 }}>
                    Запити на вступ
                    {requests.length > 0 && <span style={{ color: "#7c5cfc", marginLeft: 8 }}>({requests.length})</span>}
                  </span>
                  <div style={{ display: "flex", gap: 12, alignItems: "center" }}>
                    <button
                      onClick={loadRequests}
                      style={{ background: "none", border: "none", color: "#666", fontSize: 13, cursor: "pointer", padding: 0 }}
                      title="Оновити список"
                    >
                      ↻
                    </button>
                    <span
                      onClick={() => navigate(`/team/requests?teamId=${team.id}`)}
                      style={{ cursor: "pointer", color: "#7c5cfc", fontSize: 13 }}
                    >
                      Переглянути всі →
                    </span>
                  </div>
                </div>
                {requestsError && (
                  <p style={{ color: "#ff6b6b", fontSize: 13 }}>Помилка: {requestsError}</p>
                )}
                {!requestsError && requests.length === 0 && (
                  <p style={{ color: "#555", fontSize: 13 }}>Нових запитів немає</p>
                )}
                {requests.slice(0, 3).map((r) => {
                  const reqName = r.fromUsername ?? r.fromUserId;
                  return (
                  <div key={r.id} className={`${styles.requestCard} ${styles.requestNew}`}>
                    <div className={styles.requestTop}>
                      {r.fromAvatarUrl ? (
                        <img src={r.fromAvatarUrl} alt="" className={styles.requestAvatar} style={{ objectFit: "cover" }} />
                      ) : (
                        <div className={styles.requestAvatar} style={{ backgroundColor: accentFor(r.fromUserId) }}>
                          {reqName.slice(0, 2).toUpperCase()}
                        </div>
                      )}
                      <div>
                        <div className={styles.requestName}>{reqName}</div>
                        <div className={styles.requestTime}>{timeAgo(r.createdAt)}</div>
                      </div>
                      <span className={styles.newBadge}>Нове</span>
                    </div>
                    <p className={styles.requestText}>Роль: {r.role}</p>
                    {r.message && <p className={styles.requestText}>{r.message}</p>}
                    <div className={styles.requestActions}>
                      <button className={styles.acceptBtn} onClick={() => handleAcceptRequest(r.id)}>Прийняти</button>
                      <button className={styles.rejectBtn} onClick={() => handleRejectRequest(r.id)}>Відхилити</button>
                    </div>
                  </div>
                  );
                })}
              </div>
            </div>

            {/* Right column */}
            <div className={styles.rightCol}>
              <div className={styles.card}>
                <h3 className={styles.cardTitle}>Учасники команди</h3>
                <div className={styles.membersList}>
                  {team.members.map((m, i) => (
                    <div key={i} className={styles.memberRow}>
                      <div
                        style={{ display: "flex", alignItems: "center", gap: 12, flex: 1, cursor: "pointer" }}
                        onClick={() => navigate(`/users/${m.user.userId}`)}
                      >
                        {(m.user.avatarUrl ?? userAvatarMap.get(m.user.username)) ? (
                          <img src={m.user.avatarUrl ?? userAvatarMap.get(m.user.username)} alt="" className={styles.memberAvatar} style={{ objectFit: "cover" }} />
                        ) : (
                          <div className={styles.memberAvatar} style={{ backgroundColor: accentFor(m.user.userId) }}>
                            {initials(m.user.username)}
                          </div>
                        )}
                        <div className={styles.memberInfo}>
                          <div className={styles.memberName}>{m.user.username}</div>
                          <div className={styles.memberRole}>{m.role}</div>
                        </div>
                      </div>
                      {m.role === "Owner" ? (
                        <span className={styles.leaderBadge}>Лідер</span>
                      ) : (
                        <div className={styles.memberBtns}>
                          <button className={styles.removeBtn} onClick={() => handleRemoveMember(m.user.userId)}>
                            Видалити
                          </button>
                        </div>
                      )}
                    </div>
                  ))}
                </div>
              </div>

              <div className={styles.card}>
                <h3 className={styles.cardTitle}>Необхідні ролі</h3>
                <div className={styles.rolesList}>
                  {team.requiredRoles.length === 0 && (
                    <p className={styles.emptyText}>Немає відкритих ролей</p>
                  )}
                  {team.requiredRoles.map((r, i) => (
                    <div key={i} className={styles.roleItem}>{r.role}</div>
                  ))}
                  {showAddRole ? (
                    <div className={styles.addRoleRow}>
                      <input
                        className={styles.input}
                        placeholder="Назва ролі"
                        value={newRole}
                        onChange={(e) => setNewRole(e.target.value)}
                        onKeyDown={(e) => e.key === "Enter" && handleAddRole()}
                      />
                      <button className={styles.acceptBtn} onClick={handleAddRole}>Додати</button>
                      <button className={styles.rejectBtn} onClick={() => setShowAddRole(false)}>✕</button>
                    </div>
                  ) : (
                    <button className={styles.addRoleBtn} onClick={() => setShowAddRole(true)}>+ Додати нову роль</button>
                  )}
                </div>
              </div>
            </div>
          </div>

          {/* Portfolio */}
          <div className={styles.portfolioSection}>
            <div className={styles.portfolioHeader}>
              <h2 className={styles.portfolioTitle}>Портфоліо команди</h2>
            </div>
            {posts.length === 0 ? (
              <p className={styles.emptyText}>Публікацій поки немає</p>
            ) : (
              <div className={styles.portfolioGrid}>
                {posts.map((p, i) => {
                  const urls = p.mediaUrls ?? [];
                  const videoUrl = urls.find(u => getMediaType(u) === 'video');
                  const imageUrl = urls.find(u => getMediaType(u) === 'image');
                  const fallbackBg = PALETTE[i % PALETTE.length];
                  return (
                  <div key={i} className={styles.portfolioCard} onClick={() => navigate(`/posts/${p.postId}`)}>
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
                        <span className={styles.cardTitle2}>{p.title}</span>
                        <div style={{ display: 'flex', gap: 4, flexShrink: 0 }}>
                          {p.tags?.[0] && (
                            <span className={styles.cardTag}>{p.tags[0]}</span>
                          )}
                          <span className={styles.cardTeamBadge}>Команда</span>
                        </div>
                      </div>
                      <span className={styles.cardAuthor}>{team.name}</span>
                    </div>
                  </div>
                  );
                })}
              </div>
            )}
          </div>
          <Moderation moderationPosition="relative" moderationTop="unset" moderationLeft="unset" />
        </div>
      </div>
    );
  }

  // ── VISITOR / MEMBER VIEW ──────────────────────────────────────────────────
  return (
    <div className={styles.page}>
      <FrameComponent2 />
      <div className={styles.content}>

        <div className={styles.pubProfileHeader}>
          {team.avatarUrl ? (
            <img src={team.avatarUrl} alt="" className={styles.profileAvatar} style={{ objectFit: "cover" }} />
          ) : (
            <div className={styles.profileAvatar} style={{ backgroundColor: accent }}>{teamInits}</div>
          )}
          <div className={styles.profileInfo}>
            <h1 className={styles.profileName}>{team.name}</h1>
            <p className={styles.profileSub}>{team.tags.join(" · ") || team.category}</p>
          </div>
          {!isMember && (
            myRequest ? (
              <span style={{ color: "#3fcca0", fontSize: 14, fontWeight: 600, padding: "10px 20px", border: "1px solid #3fcca0", borderRadius: 30, whiteSpace: "nowrap" }}>
                ✓ Запит надіслано
              </span>
            ) : (
              <button
                className={styles.applyBtn}
                onClick={() => navigate(`/team/join-request?teamId=${team.id}`)}
              >
                Подати заявку
              </button>
            )
          )}
        </div>

        <div className={styles.pubMain}>
          {/* Left: Portfolio */}
          <div className={styles.pubLeftCol}>
            <div className={styles.sectionHeader}>
              <h2 className={styles.sectionTitle}>Портфоліо</h2>
            </div>
            {posts.length === 0 ? (
              <p className={styles.emptyText}>Публікацій поки немає</p>
            ) : (
              <div className={styles.pubPortfolioGrid}>
                {posts.map((p, i) => {
                  const urls = p.mediaUrls ?? [];
                  const videoUrl = urls.find(u => getMediaType(u) === 'video');
                  const imageUrl = urls.find(u => getMediaType(u) === 'image');
                  const fallbackBg = PALETTE[i % PALETTE.length];
                  return (
                  <div key={i} className={styles.portfolioCard} onClick={() => navigate(`/posts/${p.postId}`)}>
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
                        <span className={styles.cardTitle2}>{p.title}</span>
                        <div style={{ display: 'flex', gap: 4, flexShrink: 0 }}>
                          {p.tags?.[0] && (
                            <span className={styles.cardTag}>{p.tags[0]}</span>
                          )}
                          <span className={styles.cardTeamBadge}>Команда</span>
                        </div>
                      </div>
                      <span className={styles.cardAuthor}>{team.name}</span>
                    </div>
                  </div>
                  );
                })}
              </div>
            )}
          </div>

          {/* Right: About + Members */}
          <div className={styles.pubRightCol}>
            <div className={styles.card}>
              <h3 className={styles.cardSectionTitle}>Про команду</h3>
              <p className={styles.aboutText}>{team.description}</p>
              <div className={styles.infoRow}>
                <span className={styles.infoLabel}>Статус</span>
                <span className={styles.statusBadge}>{teamStatusLabel(team.status)}</span>
              </div>
              <div className={styles.infoRow}>
                <span className={styles.infoLabel}>Категорія</span>
                <span className={styles.infoValue}>{team.category}</span>
              </div>
              {team.tags.length > 0 && (
                <div className={styles.infoRow}>
                  <span className={styles.infoLabel}>Теги</span>
                  <span className={styles.infoValue}>{team.tags.join(" · ")}</span>
                </div>
              )}
            </div>

            {leader && (
              <div className={styles.card}>
                <h3 className={styles.cardSectionTitle}>Контакти лідера</h3>
                <div className={styles.infoRow}>
                  <span className={styles.infoLabel}>Ім'я</span>
                  <span className={styles.infoValue}>{leader.user.username}</span>
                </div>
                <div className={styles.infoRow}>
                  <span className={styles.infoLabel}>Email</span>
                  <span className={styles.infoValue}>
                    {userEmailMap.get(leader.user.userId) ?? '—'}
                  </span>
                </div>
              </div>
            )}

            <div className={styles.card}>
              <h3 className={styles.cardSectionTitle}>Учасники команди</h3>
              <div className={styles.membersList}>
                {team.members.map((m, i) => (
                  <div
                    key={i}
                    className={styles.memberRow}
                    style={{ cursor: "pointer" }}
                    onClick={() => navigate(`/users/${m.user.userId}`)}
                  >
                    {(m.user.avatarUrl ?? userAvatarMap.get(m.user.username)) ? (
                      <img src={m.user.avatarUrl ?? userAvatarMap.get(m.user.username)} alt="" className={styles.memberAvatar}
                        style={{ width: 40, height: 40, objectFit: "cover" }} />
                    ) : (
                      <div className={styles.memberAvatar}
                        style={{ width: 40, height: 40, fontSize: 13, backgroundColor: accentFor(m.user.userId) }}>
                        {initials(m.user.username)}
                      </div>
                    )}
                    <span className={styles.memberName}>{m.user.username}</span>
                    {m.role === "Owner" && <span className={styles.leaderBadge}>Лідер</span>}
                  </div>
                ))}
                {team.requiredRoles.length > 0 && !isMember && (
                  <button
                    className={styles.addMemberBtn}
                    onClick={() => navigate(`/team/join-request?teamId=${team.id}`)}
                  >
                    + Потрібен {team.requiredRoles[0].role}
                  </button>
                )}
              </div>
            </div>
          </div>
        </div>
        <Moderation moderationPosition="relative" moderationTop="unset" moderationLeft="unset" />
      </div>
    </div>
  );
};

export default TeamDetailPage;