import { FunctionComponent, useEffect, useState } from "react";
import { useNavigate } from "react-router-dom";
import FrameComponent2 from "../components/FrameComponent2";
import Moderation from "../components/Moderation";
import { useAuth } from "../context/AuthContext";
import { aggregatorApi } from "../api/aggregator";
import type { UserDashboardDto } from "../api/types";
import { teamStatusLabel } from "../api/types";
import styles from "./DashboardPage.module.css";

const ACCENT_PALETTE = ["#7c5cfc","#00C9A7","#e05555","#ffb347","#29b6f6"];
function accentFor(name: string): string {
  let h = 0;
  for (let i = 0; i < name.length; i++) h = (h * 31 + name.charCodeAt(i)) >>> 0;
  return ACCENT_PALETTE[h % ACCENT_PALETTE.length];
}

const CAT_COLORS: Record<string, { color: string; bg: string; border: string }> = {
  "Дизайн":   { color: "#a48fff", bg: "rgba(124,92,252,0.15)", border: "1px solid #7c5cfc" },
  "Музика":   { color: "#3fcca0", bg: "rgba(63,204,160,0.15)", border: "1px solid #3fcca0" },
  "Арт":      { color: "#ff9898", bg: "rgba(255,107,107,0.12)", border: "1px solid #ff6b6b" },
  "Фото":     { color: "#ffb347", bg: "rgba(255,179,71,0.12)", border: "1px solid #ffb347" },
  "Анімація": { color: "#29b6f6", bg: "rgba(41,182,246,0.12)", border: "1px solid #29b6f6" },
};

const DashboardPage: FunctionComponent = () => {
  const navigate = useNavigate();
  const { user, contentUserId } = useAuth();
  const [dash, setDash]     = useState<UserDashboardDto | null>(null);
  const [loading, setLoading] = useState(true);

  const displayName = user?.fullName || user?.userName || user?.email?.split('@')[0] || 'Користувач';
  const today = new Date().toLocaleDateString('uk-UA', { day: 'numeric', month: 'long', year: 'numeric' });

  useEffect(() => {
    if (!contentUserId) { setLoading(false); return; }
    aggregatorApi.getUserDashboard(contentUserId, user?.id)
      .then(setDash)
      .catch(() => {})
      .finally(() => setLoading(false));
  }, [contentUserId]);

  return (
    <div className={styles.page}>
      <FrameComponent2 />

      <div className={styles.rightCol}>
        <div className={styles.content}>
        {/* ── Header ── */}
        <div className={styles.header}>
          <div className={styles.profileRow}>
            {user?.avatarUrl ? (
              <img src={user.avatarUrl} alt="" className={styles.avatar} style={{ objectFit: 'cover' }} />
            ) : (
              <div className={styles.avatar} style={{ backgroundColor: accentFor(displayName) }}>
                {displayName.slice(0, 2).toUpperCase()}
              </div>
            )}
            <div>
              <p className={styles.greeting}>Привіт, {displayName}!</p>
              {user?.specialization && <p className={styles.subLine}>{user.specialization}</p>}
              <p className={styles.subLine}>{user?.email}</p>
            </div>
          </div>
          <span className={styles.date}>{today}</span>
        </div>

        {/* ── Stats ── */}
        <p className={styles.sectionTitle}>Моя активність</p>
        <div className={styles.statsRow}>
          {[
            { num: loading ? "…" : String(dash?.totalPosts ?? 0),      label: "моїх постів",    color: "#7c5cfc" },
            { num: loading ? "…" : String(dash?.totalSavedPosts ?? 0), label: "збережених",     color: "#3fcca0" },
            { num: loading ? "…" : String(dash?.totalTeams ?? 0),      label: "колаборацій",    color: "#ff6b6b" },
          ].map(s => (
            <div key={s.label} className={styles.statCard} style={{ borderTopColor: s.color }}>
              <span className={styles.statNum}>{s.num}</span>
              <span className={styles.statLabel}>{s.label}</span>
            </div>
          ))}
        </div>

        {/* ── Teams ── */}
        <div className={styles.sectionHeader} style={{ marginTop: 32 }}>
          <p className={styles.sectionTitle} style={{ margin: 0 }}>Мої команди</p>
          <button className={styles.seeAllBtn} onClick={() => navigate('/teams')}>Усі команди →</button>
        </div>
        <div className={styles.teamsBox}>
          {loading && <p style={{ color: "#aaa", padding: 12 }}>Завантаження...</p>}
          {!loading && (dash?.myTeams ?? []).length === 0 && (
            <p style={{ color: "#777", padding: 12 }}>Ви ще не в жодній команді</p>
          )}
          {(dash?.myTeams ?? []).map((t, i, arr) => {
            const accent = accentFor(t.name);
            const status = teamStatusLabel(t.status);
            const cat = CAT_COLORS[t.category] ?? CAT_COLORS["Дизайн"];
            const statusBg =
              status === "Активна"  ? "rgba(124,92,252,0.55)" :
              status === "У пошуку" ? "rgba(0,201,167,0.43)"  :
              "rgba(210,50,50,0.65)";
            const statusBorder =
              status === "Активна"  ? "1px solid #7c5cfc" :
              status === "У пошуку" ? "1px solid #00C9A7"  :
              "1px solid #e05555";
            return (
              <div key={t.teamId}>
                <div className={styles.teamRow} onClick={() => navigate(`/teams/${t.teamId}`)} style={{ cursor: "pointer" }}>
                  <div className={styles.teamLeft}>
                    <div className={styles.teamAccent} style={{ backgroundColor: accent }} />
                    <div>
                      <p className={styles.teamName}>{t.name}</p>
                      <p className={styles.teamSub}>{t.category} · {t.membersCount} учасників</p>
                    </div>
                  </div>
                  <div className={styles.teamRight}>
                    <span className={styles.tag} style={{ color: cat.color, background: cat.bg, border: cat.border }}>
                      {t.category}
                    </span>
                    <span className={styles.statusBadge} style={{ color: "#fff", background: statusBg, border: statusBorder }}>
                      {status}
                    </span>
                  </div>
                </div>
                {i < arr.length - 1 && <div className={styles.divider} />}
              </div>
            );
          })}
          <button className={styles.createBtn} onClick={() => navigate("/team/new")}>
            + Створити нову команду
          </button>
        </div>
        </div>
        <Moderation moderationPosition="relative" moderationTop="unset" moderationLeft="unset" />
      </div>
    </div>
  );
};

export default DashboardPage;