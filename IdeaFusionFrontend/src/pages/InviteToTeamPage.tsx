import { FunctionComponent, useEffect, useState } from "react";
import { useNavigate, useSearchParams } from "react-router-dom";
import FrameComponent2 from "../components/FrameComponent2";
import { teamsApi, groupInvitationsApi } from "../api/teams";
import { useAuth } from "../context/AuthContext";
import type { TeamEntity, UserDto } from "../api/types";
import { teamStatusLabel } from "../api/types";
import { api } from "../api/client";
import styles from "./InviteToTeamPage.module.css";

const PALETTE = ["#7c5cfc","#ff6b6b","#3fcca0","#ffb347","#29b6f6","#e040fb"];
function accentFor(seed: string): string {
  let h = 0;
  for (let i = 0; i < seed.length; i++) h = (h * 31 + seed.charCodeAt(i)) >>> 0;
  return PALETTE[h % PALETTE.length];
}

const InviteToTeamPage: FunctionComponent = () => {
  const navigate = useNavigate();
  const [params] = useSearchParams();
  const { user } = useAuth();

  const inviteeId = params.get("userId") ?? "";
  const preTeamId = params.get("teamId") ?? "";

  const [invitee, setInvitee] = useState<UserDto | null>(null);
  const [myTeams, setMyTeams] = useState<TeamEntity[]>([]);
  const [selectedTeamId, setSelectedTeamId] = useState(preTeamId);
  const [role, setRole] = useState("");
  const [message, setMessage] = useState("");
  const [submitting, setSubmitting] = useState(false);
  const [done, setDone] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    if (!user || !inviteeId) { setLoading(false); return; }
    Promise.all([
      api.get<UserDto>(`/api/users/${inviteeId}`).catch(() => null),
      teamsApi.getByMember(user.id),
    ]).then(([inv, teams]) => {
      setInvitee(inv);
      const owned = (teams as TeamEntity[]).filter(t =>
        t.members.some(m => m.user.userId === user.id && m.role === "Owner")
      );
      setMyTeams(owned);
      if (!selectedTeamId && owned.length > 0) setSelectedTeamId(owned[0].id);
    }).catch(() => {}).finally(() => setLoading(false));
  }, [user, inviteeId]);

  const selectedTeam = myTeams.find(t => t.id === selectedTeamId) ?? null;

  const availableRoles = selectedTeam?.requiredRoles.map(r => r.role) ?? [];

  async function handleSubmit() {
    if (!user) { navigate("/login"); return; }
    if (!selectedTeamId) { setError("Оберіть команду"); return; }
    if (!role.trim())    { setError("Введіть роль");    return; }
    if (!inviteeId)      { setError("Немає отримувача"); return; }

    setSubmitting(true);
    setError(null);
    try {
      await groupInvitationsApi.create({
        teamId: selectedTeamId,
        invitedUserId: inviteeId,
        invitedByUserId: user.id,
        role: role.trim(),
        message: message.trim() || undefined,
        expirationDays: 14,
      });
      setDone(true);
    } catch (ex: unknown) {
      setError(ex instanceof Error ? ex.message : "Помилка надсилання");
    } finally {
      setSubmitting(false);
    }
  }

  const inviteeName = invitee?.fullName || invitee?.userName || invitee?.email?.split("@")[0] || inviteeId.slice(0, 8);
  const inviteeInitials = inviteeName.slice(0, 2).toUpperCase();
  const inviteeColor = accentFor(inviteeId);

  return (
    <div className={styles.page}>
      <FrameComponent2 />

      <header className={styles.header}>
        <h2 className={styles.headerTitle}>Запросити до команди</h2>
        <button className={styles.cancelBtn} onClick={() => navigate(-1)}>Скасувати</button>
      </header>

      <main className={styles.main}>
        {done ? (
          <div className={styles.card}>
            <h3 className={styles.cardTitle} style={{ color: "#3fcca0" }}>✓ Запрошення надіслано!</h3>
            <p style={{ color: "#888", fontSize: 14 }}>
              {inviteeName} отримає запрошення до команди «{selectedTeam?.name}».
            </p>
            <button className={styles.submitBtn} onClick={() => navigate(-1)}>Назад</button>
          </div>
        ) : loading ? (
          <div className={styles.card}>
            <p style={{ color: "#aaa" }}>Завантаження...</p>
          </div>
        ) : (
          <div className={styles.card}>
            <h3 className={styles.cardTitle}>Надішли запрошення користувачу</h3>

            {/* Кому */}
            <div className={styles.section}>
              <div className={styles.label}>Кому</div>
              <div className={styles.recipient}>
                {invitee?.avatarUrl ? (
                  <img src={invitee.avatarUrl} alt="" className={styles.recipientAvatar} style={{ objectFit: "cover" }} />
                ) : (
                  <div className={styles.recipientAvatar} style={{ backgroundColor: inviteeColor }}>
                    {inviteeInitials}
                  </div>
                )}
                <div>
                  <div className={styles.recipientName}>{inviteeName}</div>
                  {invitee?.specialization && (
                    <div className={styles.recipientRole}>{invitee.specialization}</div>
                  )}
                </div>
              </div>
            </div>

            {/* Оберіть команду */}
            <div className={styles.section}>
              <div className={styles.label}>Оберіть команду</div>
              {myTeams.length === 0 ? (
                <p style={{ color: "#666", fontSize: 13 }}>
                  У вас немає команд, де ви власник.{" "}
                  <span style={{ color: "#7c5cfc", cursor: "pointer" }} onClick={() => navigate("/team/new")}>
                    Створити команду
                  </span>
                </p>
              ) : (
                <div className={styles.teamList}>
                  {myTeams.map(t => {
                    const status = teamStatusLabel(t.status);
                    const active = selectedTeamId === t.id;
                    const statusStyle =
                      status === "Активна"
                        ? { bg: "rgba(63,204,160,0.2)", border: "1px solid #3fcca0", color: "#3fcca0" }
                        : status === "У пошуку"
                        ? { bg: "rgba(124,92,252,0.2)", border: "1px solid #7c5cfc", color: "#a48fff" }
                        : { bg: "rgba(210,50,50,0.2)", border: "1px solid #e05555", color: "#ff6b6b" };
                    return (
                      <label
                        key={t.id}
                        className={`${styles.teamRow} ${active ? styles.teamRowActive : ""}`}
                        onClick={() => { setSelectedTeamId(t.id); setRole(""); }}
                      >
                        <div className={styles.teamAccent} style={{ backgroundColor: accentFor(t.id) }} />
                        <div className={styles.teamInfo}>
                          <div className={styles.teamTop}>
                            <span className={styles.teamName}>{t.name}</span>
                            <span className={styles.teamBadge} style={{ background: statusStyle.bg, border: statusStyle.border, color: statusStyle.color }}>
                              {status}
                            </span>
                            <span className={styles.teamCount}>{t.members.length} учасників</span>
                          </div>
                          <div className={styles.teamSub}>{t.tags.join(" · ") || t.category}</div>
                        </div>
                        <input type="radio" name="team" className={styles.radio} readOnly checked={active} />
                      </label>
                    );
                  })}
                </div>
              )}
            </div>

            {/* Роль */}
            <div className={styles.section}>
              <div className={styles.label}>На роль</div>
              {availableRoles.length > 0 ? (
                <select className={styles.select} value={role} onChange={e => setRole(e.target.value)}>
                  <option value="">Оберіть з наявних...</option>
                  {availableRoles.map(r => <option key={r} value={r}>{r}</option>)}
                </select>
              ) : (
                <input
                  className={styles.input}
                  placeholder="Введіть роль (напр. UI Designer)"
                  value={role}
                  onChange={e => setRole(e.target.value)}
                />
              )}
            </div>

            {/* Повідомлення */}
            <div className={styles.section}>
              <div className={styles.label}>Текст повідомлення</div>
              <textarea
                className={styles.textarea}
                rows={5}
                placeholder="Привіт! Ми хотіли б запросити вас до нашої команди..."
                value={message}
                onChange={e => setMessage(e.target.value)}
              />
            </div>

            {error && <p style={{ color: "#ff6b6b", fontSize: 13 }}>{error}</p>}

            <button
              className={styles.submitBtn}
              onClick={handleSubmit}
              disabled={submitting || myTeams.length === 0}
            >
              {submitting ? "Надсилання..." : "Надіслати"}
            </button>
          </div>
        )}
      </main>
    </div>
  );
};

export default InviteToTeamPage;