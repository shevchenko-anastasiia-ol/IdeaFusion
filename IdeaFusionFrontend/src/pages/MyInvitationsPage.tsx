import { FunctionComponent, useEffect, useState } from "react";
import { useNavigate } from "react-router-dom";
import FrameComponent2 from "../components/FrameComponent2";
import Moderation from "../components/Moderation";
import { groupInvitationsApi, teamsApi } from "../api/teams";
import type { GroupInvitationEntity, UserDto } from "../api/types";
import { invitationStatusKey, timeAgo } from "../api/types";
import { useAuth } from "../context/AuthContext";
import { api } from "../api/client";
import styles from "./MyInvitationsPage.module.css";

const AVATAR_PALETTE = ["#7c5cfc","#ff6b6b","#3fcca0","#888","#ffb347","#29b6f6"];
function avatarColor(seed: string): string {
  let h = 0;
  for (let i = 0; i < seed.length; i++) h = (h * 31 + seed.charCodeAt(i)) >>> 0;
  return AVATAR_PALETTE[h % AVATAR_PALETTE.length];
}

const MyInvitationsPage: FunctionComponent = () => {
  const navigate = useNavigate();
  const { user } = useAuth();
  const [activeFilter, setActiveFilter] = useState("Всі");
  const [invitations, setInvitations] = useState<GroupInvitationEntity[]>([]);
  const [loading, setLoading] = useState(true);
  const [userMap, setUserMap]   = useState<Record<string, UserDto>>({});
  const [teamMap, setTeamMap]   = useState<Record<string, string>>({});

  const filters = ["Всі", "Нові", "Прийняті", "Відхилені"];

  useEffect(() => {
    if (!user) { setLoading(false); return; }
    Promise.all([
      groupInvitationsApi.getByUser(user.id),
      api.get<UserDto[]>('/api/users').catch(() => [] as UserDto[]),
      teamsApi.getPaged(1, 200).then((r: any) => Array.isArray(r) ? r : (r.items ?? [])).catch(() => []),
    ]).then(([inv, users, teams]) => {
      setInvitations(inv as GroupInvitationEntity[]);
      const um: Record<string, UserDto> = {};
      (users as UserDto[]).forEach(u => { um[u.id] = u; });
      setUserMap(um);
      const tm: Record<string, string> = {};
      (teams as any[]).forEach(t => { tm[t.id] = t.name; });
      setTeamMap(tm);
    }).catch(() => {}).finally(() => setLoading(false));
  }, [user]);

  async function handleAccept(inv: GroupInvitationEntity) {
    if (!user) return;
    const username = user.userName ?? user.email?.split('@')[0] ?? user.id;
    await groupInvitationsApi.accept(inv.id, {
      invitationId: inv.id,
      userId: user.id,
      username,
      avatarUrl: user.avatarUrl ?? undefined,
    });
    setInvitations(prev => prev.map(x => x.id === inv.id ? { ...x, status: 1 } : x));
  }

  async function handleDecline(inv: GroupInvitationEntity) {
    if (!user) return;
    await groupInvitationsApi.decline(inv.id, { invitationId: inv.id, userId: user.id });
    setInvitations(prev => prev.map(x => x.id === inv.id ? { ...x, status: 2 } : x));
  }

  const filtered = invitations.filter(inv => {
    const k = invitationStatusKey(inv.status);
    if (activeFilter === "Всі")      return true;
    if (activeFilter === "Нові")     return k === "new";
    if (activeFilter === "Прийняті") return k === "accepted";
    if (activeFilter === "Відхилені") return k === "rejected";
    return true;
  });

  return (
    <div className={styles.page}>
      <FrameComponent2 />

      <div className={styles.content}>
        <header className={styles.header}>
          <h2 className={styles.headerTitle}>Мої запрошення до команд</h2>
          <button className={styles.backBtn} onClick={() => navigate(-1)}>Назад</button>
        </header>

        <main className={styles.main}>
          <div className={styles.filters}>
            {filters.map(f => (
              <button key={f} className={`${styles.filterBtn} ${activeFilter === f ? styles.filterActive : ""}`} onClick={() => setActiveFilter(f)}>
                {f}
              </button>
            ))}
          </div>

          <div className={styles.list}>
            {loading && <p style={{ color: "#aaa" }}>Завантаження...</p>}
            {!loading && filtered.length === 0 && <p style={{ color: "#666" }}>Запрошень немає</p>}
            {filtered.map(inv => {
              const key = invitationStatusKey(inv.status);
              const inviter = userMap[inv.invitedByUserId];
              const inviterName = inviter
                ? (inviter.fullName ?? inviter.userName ?? inviter.email.split('@')[0])
                : inv.invitedByUserId.slice(0, 8) + "…";
              const inviterInitials = inviterName.slice(0, 2).toUpperCase();
              const resolvedTeamName = teamMap[inv.teamId] ?? inv.teamId.slice(0, 8) + "…";
              return (
                <div key={inv.id} className={`${styles.card} ${key === "new" ? styles.cardNew : key === "rejected" ? styles.cardRejected : styles.cardAccepted}`}>
                  <div className={styles.cardTop}
                    style={{ cursor: "pointer" }}
                    onClick={() => navigate(`/users/${inv.invitedByUserId}`)}>
                    {inviter?.avatarUrl ? (
                      <img src={inviter.avatarUrl} alt="" className={styles.avatar} style={{ objectFit: "cover", borderRadius: "50%" }} />
                    ) : (
                      <div className={styles.avatar} style={{ backgroundColor: avatarColor(inv.invitedByUserId) }}>
                        {inviterInitials}
                      </div>
                    )}
                    <div className={styles.meta}>
                      <div className={styles.nameRow}>
                        <span className={styles.name}>{inviterName}</span>
                        {key === "new"      && <span className={styles.newBadge}>Нове</span>}
                        {key === "rejected" && <span className={styles.rejectedBadge}>Відхилено</span>}
                        {key === "accepted" && <span className={styles.acceptedBadge}>Прийнято</span>}
                      </div>
                      <span className={styles.time}>{timeAgo(inv.createdAt)}</span>
                    </div>
                  </div>

                  {inv.message && <p className={styles.text}>{inv.message}</p>}

                  <div className={styles.teamRow}>
                    <span className={styles.teamLabel}>До команди:</span>
                    <span
                      className={styles.teamName}
                      style={{ cursor: "pointer", color: "#a48fff" }}
                      onClick={() => navigate(`/teams/${inv.teamId}`)}
                    >«{resolvedTeamName}»</span>
                    <span style={{ color: "#888", fontSize: 12 }}> · роль: {inv.role}</span>
                  </div>

                  {key === "new" && (
                    <div className={styles.actions}>
                      <button className={styles.acceptBtn} onClick={() => handleAccept(inv)}>Прийняти</button>
                      <button className={styles.rejectBtn} onClick={() => handleDecline(inv)}>Відхилити</button>
                    </div>
                  )}
                  {key === "rejected" && <span className={styles.actionLabel} style={{ color: "#ff6b6b" }}>Відхилено</span>}
                  {key === "accepted" && <span className={styles.actionLabel} style={{ color: "#3fcca0" }}>Прийнято до команди</span>}
                </div>
              );
            })}
          </div>
        </main>

        <Moderation moderationPosition="relative" moderationMarginTop="auto" moderationTop="unset" moderationLeft="0" />
      </div>
    </div>
  );
};

export default MyInvitationsPage;