import { FunctionComponent, useEffect, useState } from "react";
import { useNavigate } from "react-router-dom";
import FrameComponent2 from "../components/FrameComponent2";
import styles from "./Component3.module.css";
import { useAuth } from "../context/AuthContext";
import { teamsApi, groupInvitationsApi } from "../api/teams";
import type { TeamEntity, GroupInvitationEntity } from "../api/types";
import { teamStatusLabel, invitationStatusKey, timeAgo } from "../api/types";

export type Component3Type = {};

const ACCENT_PALETTE = ["#7c5cfc","#00C9A7","#e05555","#ffb347","#29b6f6"];
function accentFor(name: string): string {
  let h = 0;
  for (let i = 0; i < name.length; i++) h = (h * 31 + name.charCodeAt(i)) >>> 0;
  return ACCENT_PALETTE[h % ACCENT_PALETTE.length];
}

const AVATAR_PALETTE = ["#7c5cfc","#ff6b6b","#3fcca0","#ffb347","#29b6f6","#e040fb"];
function avatarColor(seed: string): string {
  let h = 0;
  for (let i = 0; i < seed.length; i++) h = (h * 31 + seed.charCodeAt(i)) >>> 0;
  return AVATAR_PALETTE[h % AVATAR_PALETTE.length];
}

const FILTERS = ["Усі", "Активні", "У пошуку", "Завершені"];

// ── Team card ──────────────────────────────────────────────
const TeamCard: FunctionComponent<{ team: TeamEntity; onClick: () => void }> = ({ team, onClick }) => {
  const status = teamStatusLabel(team.status);
  const accent = accentFor(team.name);
  const openRole = team.requiredRoles[0]?.role;

  const statusStyle =
    status === "Активна"
      ? { bg: "rgba(63,204,160,0.15)", border: "1px solid #3fcca0", color: "#3fcca0" }
      : status === "У пошуку"
      ? { bg: "rgba(124,92,252,0.15)", border: "1px solid #7c5cfc", color: "#a48fff" }
      : { bg: "rgba(210,50,50,0.15)", border: "1px solid #e05555", color: "#ff6b6b" };

  return (
    <div className={styles.teamCard} onClick={onClick}>
      {team.avatarUrl ? (
        <img src={team.avatarUrl} alt="" className={styles.teamAccent} style={{ objectFit: "cover" }} />
      ) : (
        <div className={styles.teamAccent} style={{ background: accent }}>
          {team.name.slice(0, 2).toUpperCase()}
        </div>
      )}
      <div className={styles.teamInfo}>
        <div className={styles.teamNameRow}>
          <span className={styles.teamName}>{team.name}</span>
          <span
            className={styles.statusBadge}
            style={{ background: statusStyle.bg, border: statusStyle.border, color: statusStyle.color }}
          >
            {status}
          </span>
        </div>
        <div className={styles.teamTags}>{(team.tags ?? []).join(' · ')}</div>
        <div className={styles.teamMeta}>
          <span>{(team.members ?? []).length} учасників</span>
          {openRole && <span style={{ color: "#a48fff" }}>потрібен {openRole}</span>}
        </div>
      </div>
    </div>
  );
};

// ── Invitation card (sidebar) ──────────────────────────────
const InvCard: FunctionComponent<{
  inv: GroupInvitationEntity;
  teamName: string;
  onAccept: () => void;
  onDecline: () => void;
}> = ({ inv, teamName, onAccept, onDecline }) => {
  const navigate = useNavigate();
  const isPending = invitationStatusKey(inv.status) === "new";
  const color = avatarColor(inv.invitedByUserId);

  return (
    <div className={`${styles.invCard} ${isPending ? styles.invCardNew : styles.invCardOther}`}>
      <div className={styles.invTop}>
        <div
          className={styles.invAvatar}
          style={{ background: color, cursor: "pointer" }}
          onClick={() => navigate(`/users/${inv.invitedByUserId}`)}
          title="Переглянути профіль"
        >
          {inv.invitedByUserId.slice(0, 2).toUpperCase()}
        </div>
        <div className={styles.invMeta}>
          <div className={styles.invTeam}>{teamName}</div>
          <div className={styles.invTime}>{timeAgo(inv.createdAt)}</div>
        </div>
        {isPending && (
          <span style={{
            fontSize: 10, padding: "2px 8px", borderRadius: 20,
            background: "rgba(63,204,160,0.2)", border: "1px solid #3fcca0", color: "#3fcca0",
          }}>Нове</span>
        )}
      </div>
      <div className={styles.invRole}>Роль: {inv.role}</div>
      {isPending && (
        <div className={styles.invActions}>
          <button className={styles.acceptBtn} onClick={onAccept}>Прийняти</button>
          <button className={styles.declineBtn} onClick={onDecline}>Відхилити</button>
        </div>
      )}
    </div>
  );
};

// ── Page ──────────────────────────────────────────────────
const Component3: FunctionComponent<Component3Type> = () => {
  const navigate = useNavigate();
  const { user } = useAuth();
  const [activeFilter, setActiveFilter] = useState("Усі");
  const [myTeams, setMyTeams] = useState<TeamEntity[]>([]);
  const [allTeams, setAllTeams] = useState<TeamEntity[]>([]);
  const [invitations, setInvitations] = useState<GroupInvitationEntity[]>([]);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    if (!user) { setLoading(false); return; }
    Promise.all([
      teamsApi.getByMember(user.id),
      teamsApi.getPaged(1, 100).then((r: any) => Array.isArray(r) ? r : (r.items ?? [])),
      groupInvitationsApi.getByUser(user.id),
    ]).then(([my, all, inv]) => {
      setMyTeams(my as TeamEntity[]);
      setAllTeams(all as TeamEntity[]);
      setInvitations(inv as GroupInvitationEntity[]);
    }).catch(() => {}).finally(() => setLoading(false));
  }, [user]);

  // Build teamId → name map for invitation cards
  const teamNameMap: Record<string, string> = {};
  [...myTeams, ...allTeams].forEach(t => { teamNameMap[t.id] = t.name; });

  const filteredTeams = myTeams.filter(t => {
    if (activeFilter === "Усі") return true;
    const label = teamStatusLabel(t.status);
    if (activeFilter === "Активні")   return label === "Активна";
    if (activeFilter === "У пошуку")  return label === "У пошуку";
    if (activeFilter === "Завершені") return label === "Завершена";
    return true;
  });

  const pendingInvitations = invitations.filter(i => invitationStatusKey(i.status) === "new");
  const sidebarInvitations = pendingInvitations.slice(0, 4);

  async function handleAccept(inv: GroupInvitationEntity) {
    if (!user) return;
    const username = user.userName ?? user.email?.split('@')[0] ?? user.id;
    await groupInvitationsApi.accept(inv.id, {
      invitationId: inv.id,
      userId: user.id,
      username,
      avatarUrl: user.avatarUrl ?? undefined,
    });
    setInvitations(prev => prev.map(i => i.id === inv.id ? { ...i, status: 1 } : i));
  }

  async function handleDecline(inv: GroupInvitationEntity) {
    if (!user) return;
    await groupInvitationsApi.decline(inv.id, { invitationId: inv.id, userId: user.id });
    setInvitations(prev => prev.map(i => i.id === inv.id ? { ...i, status: 2 } : i));
  }

  return (
    <div className={styles.page}>
      <FrameComponent2 />

      <div className={styles.body}>
        {/* ── Main: my teams ── */}
        <div className={styles.main}>
          <div className={styles.mainHeader}>
            <span className={styles.mainTitle}>Мої команди</span>
            <button className={styles.newTeamBtn} onClick={() => navigate("/team/new")}>
              + Нова команда
            </button>
          </div>

          <div className={styles.filters}>
            {FILTERS.map(f => (
              <button
                key={f}
                className={`${styles.filterBtn} ${activeFilter === f ? styles.filterBtnActive : ""}`}
                onClick={() => setActiveFilter(f)}
              >
                {f}
              </button>
            ))}
          </div>

          <div className={styles.teamsList}>
            {loading && <p className={styles.emptyMsg}>Завантаження...</p>}

            {!loading && myTeams.length === 0 && (
              <p className={styles.emptyMsg}>
                Ви ще не в жодній команді.{" "}
                <span
                  onClick={() => navigate("/team/new")}
                  style={{ color: "#7c5cfc", cursor: "pointer" }}
                >
                  Створити команду
                </span>
              </p>
            )}

            {!loading && myTeams.length > 0 && filteredTeams.length === 0 && (
              <p className={styles.emptyMsg}>Немає команд з таким статусом</p>
            )}

            {filteredTeams.map(team => (
              <TeamCard
                key={team.id}
                team={team}
                onClick={() => navigate(`/teams/${team.id}`)}
              />
            ))}
          </div>
        </div>

        {/* ── Right sidebar: invitations ── */}
        <div className={styles.sidebar}>
          <div className={styles.sidebarSection}>
            <div className={styles.sidebarHeader}>
              <span className={styles.sidebarTitle}>
                Запрошення
                {pendingInvitations.length > 0 && (
                  <span className={styles.badge}>{pendingInvitations.length}</span>
                )}
              </span>
              <button className={styles.viewAllBtn} onClick={() => navigate("/team/invitations")}>
                Усі →
              </button>
            </div>

            {loading && <p className={styles.noInvitations}>Завантаження...</p>}

            {!loading && sidebarInvitations.length === 0 && (
              <p className={styles.noInvitations}>Нових запрошень немає</p>
            )}

            {sidebarInvitations.map(inv => (
              <InvCard
                key={inv.id}
                inv={inv}
                teamName={teamNameMap[inv.teamId] ?? "Команда"}
                onAccept={() => handleAccept(inv)}
                onDecline={() => handleDecline(inv)}
              />
            ))}
          </div>
        </div>
      </div>
    </div>
  );
};

export default Component3;