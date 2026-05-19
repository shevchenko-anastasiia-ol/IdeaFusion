import { FunctionComponent, useEffect, useState } from "react";
import { useNavigate } from "react-router-dom";
import { Typography, Box } from "@mui/material";
import FrameComponent2 from "../components/FrameComponent2";
import Moderation from "../components/Moderation";
import { teamsApi } from "../api/teams";
import { collaborationRequestsApi } from "../api/teams";
import type { TeamEntity } from "../api/types";
import { teamStatusLabel } from "../api/types";
import { useAuth } from "../context/AuthContext";
import styles from "./AllTeamsPage.module.css";

const AVATAR_PALETTE = [
  "#4002aa", "#ff6b6b", "#ffb347", "#00c9a7",
  "#e040fb", "#ef5350", "#26c6da", "#ec407a", "#29b6f6", "#66bb6a",
];

const CAT_COLORS: Record<string, { bg: string; border: string; color: string }> = {
  "Дизайн":   { bg: "rgba(100,100,120,0.25)", border: "#555",     color: "#aaa" },
  "Музика":   { bg: "rgba(0,201,167,0.12)",   border: "#00c9a7",  color: "#00c9a7" },
  "Арт":      { bg: "rgba(255,107,107,0.12)", border: "#ff6b6b",  color: "#ff9898" },
  "Фото":     { bg: "rgba(255,179,71,0.12)",  border: "#ffb347",  color: "#ffb347" },
  "Анімація": { bg: "rgba(41,182,246,0.12)",  border: "#29b6f6",  color: "#29b6f6" },
};

function avatarColor(seed: string): string {
  let h = 0;
  for (let i = 0; i < seed.length; i++) h = (h * 31 + seed.charCodeAt(i)) >>> 0;
  return AVATAR_PALETTE[h % AVATAR_PALETTE.length];
}

const CATEGORIES = ["Всі", "Дизайн", "Музика", "Арт", "Фото", "Анімація"];
const STATUSES   = ["Всі", "У пошуку", "Активна", "Завершена"];

const TeamCard: FunctionComponent<{ team: TeamEntity; isAdmin?: boolean; onDelete?: (id: string) => void }> = ({ team, isAdmin, onDelete }) => {
  const navigate = useNavigate();
  const { user } = useAuth();
  const [applying, setApplying] = useState(false);
  const [applied,  setApplied]  = useState(false);
  const [err,      setErr]      = useState<string | null>(null);

  const cat    = CAT_COLORS[team.category] ?? CAT_COLORS["Дизайн"];
  const status = teamStatusLabel(team.status);

  const openRole  = team.requiredRoles[0]?.role ?? null;
  const extraCount = Math.max(0, team.members.length - 3);

  async function handleApply(e: React.MouseEvent) {
    e.stopPropagation();
    if (!user) { navigate('/login'); return; }
    setApplying(true);
    setErr(null);
    try {
      await collaborationRequestsApi.create({
        teamId: team.id,
        fromUserId: user.id,
        role: openRole ?? 'Учасник',
      });
      setApplied(true);
    } catch (ex: any) {
      setErr(ex.message);
    } finally {
      setApplying(false);
    }
  }

  return (
    <div className={styles.teamCard} onClick={() => navigate(`/teams/${team.id}`)} style={{ position: "relative" }}>
      {isAdmin && (
        <button
          style={{ position: "absolute", top: 10, right: 10, background: "none", border: "none", cursor: "pointer", color: "#ff6b6b", padding: 4, zIndex: 2 }}
          title="Видалити команду"
          onClick={e => { e.stopPropagation(); onDelete?.(team.id); }}
        >
          <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
            <polyline points="3 6 5 6 21 6"/><path d="M19 6l-1 14a2 2 0 0 1-2 2H8a2 2 0 0 1-2-2L5 6"/><path d="M10 11v6"/><path d="M14 11v6"/><path d="M9 6V4a1 1 0 0 1 1-1h4a1 1 0 0 1 1 1v2"/>
          </svg>
        </button>
      )}
      <div className={styles.cardInfo}>
        <div className={styles.nameRow}>
          <span className={styles.teamName}>{team.name}</span>
          <span
            className={styles.statusBadge}
            data-status={status === "У пошуку" ? "search" : status === "Активна" ? "active" : "closed"}
          >
            {status}
          </span>
        </div>
        <span className={styles.teamTags}>{team.tags.join(' · ')}</span>
        <span className={styles.categoryBadge} style={{ background: cat.bg, border: `1px solid ${cat.border}`, color: cat.color }}>
          {team.category}
        </span>
      </div>

      <div className={styles.separator} />

      <div className={styles.cardBottom}>
        <div className={styles.membersRow}>
          <span className={styles.membersLabel}>Учасники</span>
          <div className={styles.avatars}>
            {team.members.slice(0, 3).map((m, i) => (
              <div key={i} className={styles.avatar} style={{ backgroundColor: avatarColor(m.user.userId) }}>
                {m.user.username.slice(0, 2).toUpperCase()}
              </div>
            ))}
            {extraCount > 0 && <div className={styles.avatarExtra}>+{extraCount}</div>}
          </div>
          {openRole && <span className={styles.openRole}>+ Потрібен {openRole}</span>}
        </div>

        {err && <span style={{ color: '#ff6b6b', fontSize: 12 }}>{err}</span>}

        <div className={styles.actionsRow}>
          <Box component="span" className={styles.postsCount}>
            <Typography variant="body2">Ролей відкрито: {team.requiredRoles.length}</Typography>
          </Box>
          {status !== "Завершена" && !applied && (
            <button
              className={styles.applyBtn}
              disabled={applying}
              onClick={handleApply}
            >
              {applying ? '...' : 'Подати заявку'}
            </button>
          )}
          {applied && <span className={styles.appliedBtn}>Заявку подано ✓</span>}
        </div>
      </div>
    </div>
  );
};

const PAGE_SIZE = 10;

const AllTeamsPage: FunctionComponent = () => {
  const navigate = useNavigate();
  const { user: me } = useAuth();
  const [activeCategory, setActiveCategory] = useState("Всі");
  const [activeStatus,   setActiveStatus]   = useState("Всі");
  const [teams,   setTeams]   = useState<TeamEntity[]>([]);
  const [loading, setLoading] = useState(true);
  const [error,   setError]   = useState<string | null>(null);
  const [page,    setPage]    = useState(1);

  useEffect(() => {
    teamsApi.getPaged(1, 50)
      .then(res => setTeams(res.items ?? (res as any)))
      .catch((e: Error) => setError(e.message))
      .finally(() => setLoading(false));
  }, []);

  const isAdmin = me?.roles?.includes('Admin');

  async function handleDeleteTeam(id: string) {
    if (!confirm('Видалити цю команду назавжди?')) return;
    try {
      await teamsApi.deleteTeam(id);
      setTeams(prev => prev.filter(t => t.id !== id));
    } catch (e: any) {
      alert(e.message ?? 'Помилка видалення');
    }
  }

  const filtered = teams.filter(t => {
    const catOk    = activeCategory === "Всі" || t.category === activeCategory;
    const statusOk = activeStatus   === "Всі" || teamStatusLabel(t.status) === activeStatus;
    return catOk && statusOk;
  });

  const totalPages = Math.max(1, Math.ceil(filtered.length / PAGE_SIZE));
  const pageTeams  = filtered.slice((page - 1) * PAGE_SIZE, page * PAGE_SIZE);

  function setCategory(cat: string) { setActiveCategory(cat); setPage(1); }
  function setStatus(s: string)     { setActiveStatus(s);     setPage(1); }

  return (
    <div className={styles.page}>
      <FrameComponent2 />

      <div className={styles.rightCol}>
        <header className={styles.header}>
          <div className={styles.headerLeft}>
            <button className={styles.backBtn} onClick={() => navigate(-1)}>← Назад</button>
            <h1 className={styles.pageTitle}>Всі команди</h1>
          </div>
          <div className={styles.headerControls}>
            <div className={styles.filterGroup}>
              {CATEGORIES.map(cat => (
                <button key={cat} className={activeCategory === cat ? styles.filterBtnActive : styles.filterBtn} onClick={() => setCategory(cat)}>{cat}</button>
              ))}
            </div>
            <div className={styles.hDivider} />
            <div className={styles.filterGroup}>
              {STATUSES.map(s => (
                <button key={s} className={activeStatus === s ? styles.filterBtnActive : styles.filterBtn} onClick={() => setStatus(s)}>{s}</button>
              ))}
            </div>
          </div>
        </header>

        <main className={styles.grid}>
          {loading && <p style={{ color: "#aaa", padding: 24 }}>Завантаження...</p>}
          {error   && <p style={{ color: "#ff6b6b", padding: 24 }}>Помилка: {error}</p>}
          {!loading && !error && filtered.length === 0 && (
            <p style={{ color: "#aaa", padding: 24 }}>Команд не знайдено</p>
          )}
          {pageTeams.map(team => <TeamCard key={team.id} team={team} isAdmin={isAdmin} onDelete={handleDeleteTeam} />)}
        </main>

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

        <div className={styles.footerWrap}>
          <Moderation />
        </div>
      </div>
    </div>
  );
};

export default AllTeamsPage;