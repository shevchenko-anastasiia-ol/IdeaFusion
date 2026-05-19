import { FunctionComponent, useEffect, useState } from "react";
import { useNavigate, useSearchParams } from "react-router-dom";
import FrameComponent2 from "../components/FrameComponent2";
import Moderation from "../components/Moderation";
import { teamsApi, collaborationRequestsApi } from "../api/teams";
import type { TeamEntity } from "../api/types";
import { teamStatusLabel } from "../api/types";
import { useAuth } from "../context/AuthContext";
import styles from "./JoinRequestPage.module.css";

const JoinRequestPage: FunctionComponent = () => {
  const navigate    = useNavigate();
  const [params]    = useSearchParams();
  const { user }    = useAuth();
  const preTeamId   = params.get('teamId') ?? '';

  const [team,      setTeam]      = useState<TeamEntity | null>(null);
  const [role,      setRole]      = useState('');
  const [message,   setMessage]   = useState('Доброго дня, прошу розглянути мою кандидатуру на учасника вашої команди.');
  const [submitting, setSubmitting] = useState(false);
  const [done,       setDone]       = useState(false);
  const [error,      setError]      = useState<string | null>(null);

  useEffect(() => {
    if (!preTeamId) return;
    teamsApi.getById(preTeamId)
      .then(setTeam)
      .catch(() => {});
  }, [preTeamId]);

  async function handleSubmit() {
    if (!user || !team || !role.trim()) { setError('Оберіть роль'); return; }
    setSubmitting(true);
    setError(null);
    try {
      await collaborationRequestsApi.create({
        teamId: team.id,
        fromUserId: user.id,
        fromUsername: user.userName ?? user.fullName ?? user.email?.split('@')[0],
        fromAvatarUrl: user.avatarUrl,
        role: role.trim(),
        message: message.trim() || undefined,
      });
      setDone(true);
    } catch (ex: unknown) {
      setError(ex instanceof Error ? ex.message : 'Помилка надсилання');
    } finally {
      setSubmitting(false);
    }
  }

  const roles = team?.requiredRoles.map(r => r.role) ?? [
    'Розробник React', 'Графічний дизайнер', 'Композитор', '3D артист',
  ];

  const status = team ? teamStatusLabel(team.status) : '';

  return (
    <div className={styles.page}>
      <FrameComponent2 />

      <div className={styles.content}>
        <header className={styles.header}>
          <h2 className={styles.headerTitle}>Запит на вступ до команди</h2>
          <button className={styles.cancelBtn} onClick={() => navigate(-1)}>Скасувати</button>
        </header>

        <main className={styles.main}>
          {done ? (
            <div className={styles.card}>
              <h3 className={styles.cardTitle} style={{ color: "#3fcca0" }}>✓ Заявку надіслано!</h3>
              <button className={styles.submitBtn} onClick={() => navigate(-1)}>Назад</button>
            </div>
          ) : (
            <div className={styles.card}>
              <h3 className={styles.cardTitle}>Надішли запит на вступ до команди</h3>

              <div className={styles.section}>
                <div className={styles.label}>Від кого</div>
                <div className={styles.recipient}>
                  {user?.avatarUrl ? (
                    <img src={user.avatarUrl} alt="" className={styles.recipientAvatar} style={{ objectFit: "cover" }} />
                  ) : (
                    <div className={styles.recipientAvatar} style={{ backgroundColor: "#7c5cfc" }}>
                      {(user?.userName ?? user?.email ?? '?').slice(0, 2).toUpperCase()}
                    </div>
                  )}
                  <div>
                    <div className={styles.recipientName}>
                      {user?.fullName ?? user?.userName ?? user?.email ?? 'Ви'}
                    </div>
                    {user?.specialization && (
                      <div className={styles.recipientRole}>{user.specialization}</div>
                    )}
                  </div>
                </div>
              </div>

              {team && (
                <div className={styles.section}>
                  <div className={styles.label}>В команду</div>
                  <div className={styles.teamRow}>
                    <div className={styles.teamAccent} style={{ backgroundColor: "#7c5cfc" }} />
                    <div className={styles.teamInfo}>
                      <div className={styles.teamTop}>
                        <span className={styles.teamName}>{team.name}</span>
                        <span className={styles.teamBadge} style={{ background: "rgba(124,92,252,0.2)", border: "1px solid #7c5cfc", color: "#a48fff" }}>
                          {status}
                        </span>
                      </div>
                      <div className={styles.teamSub}>{team.tags.join(' · ')}</div>
                    </div>
                  </div>
                </div>
              )}

              <div className={styles.section}>
                <div className={styles.label}>На роль</div>
                <select className={styles.select} value={role} onChange={e => setRole(e.target.value)}>
                  <option value="">Оберіть з наявних...</option>
                  {roles.map(r => <option key={r} value={r}>{r}</option>)}
                </select>
              </div>

              <div className={styles.section}>
                <div className={styles.label}>Текст повідомлення</div>
                <textarea
                  className={styles.textarea}
                  rows={6}
                  value={message}
                  onChange={e => setMessage(e.target.value)}
                />
              </div>

              {error && <p style={{ color: "#ff6b6b", marginBottom: 8 }}>{error}</p>}

              <button className={styles.submitBtn} disabled={submitting} onClick={handleSubmit}>
                {submitting ? '...' : 'Надіслати'}
              </button>
            </div>
          )}
        </main>

        <Moderation moderationPosition="relative" moderationMarginTop="auto" moderationTop="unset" moderationLeft="124px" />
      </div>
    </div>
  );
};

export default JoinRequestPage;