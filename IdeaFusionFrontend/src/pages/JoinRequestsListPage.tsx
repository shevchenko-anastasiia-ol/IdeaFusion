import { FunctionComponent, useEffect, useState } from "react";
import { useNavigate, useSearchParams } from "react-router-dom";
import FrameComponent2 from "../components/FrameComponent2";
import Moderation from "../components/Moderation";
import { collaborationRequestsApi } from "../api/teams";
import type { CollaborationRequestEntity } from "../api/types";
import { requestStatusKey, timeAgo } from "../api/types";
import { useAuth } from "../context/AuthContext";
import styles from "./JoinRequestsListPage.module.css";

const AVATAR_PALETTE = ["#7c5cfc","#ff6b6b","#3fcca0","#888","#ffb347","#29b6f6"];
function avatarColor(seed: string): string {
  let h = 0;
  for (let i = 0; i < seed.length; i++) h = (h * 31 + seed.charCodeAt(i)) >>> 0;
  return AVATAR_PALETTE[h % AVATAR_PALETTE.length];
}

const JoinRequestsListPage: FunctionComponent = () => {
  const navigate      = useNavigate();
  const [params]      = useSearchParams();
  const { user }      = useAuth();
  const teamId        = params.get('teamId') ?? '';

  const [activeFilter, setActiveFilter] = useState("Всі");
  const [requests,  setRequests]  = useState<CollaborationRequestEntity[]>([]);
  const [loading,   setLoading]   = useState(true);
  const [error,     setError]     = useState<string | null>(null);

  const filters = ["Всі", "Нові", "Прийняті", "Відхилені"];

  useEffect(() => {
    if (!teamId) { setLoading(false); return; }
    collaborationRequestsApi.getByTeam(teamId)
      .then(setRequests)
      .catch((e) => setError(e?.message ?? 'Помилка завантаження запитів'))
      .finally(() => setLoading(false));
  }, [teamId]);

  async function handleAccept(r: CollaborationRequestEntity) {
    if (!user) return;
    await collaborationRequestsApi.accept(r.id, { requestId: r.id, userId: user.id });
    setRequests(prev => prev.map(x => x.id === r.id ? { ...x, status: 1 } : x));
  }

  async function handleReject(r: CollaborationRequestEntity) {
    if (!user) return;
    await collaborationRequestsApi.reject(r.id, { requestId: r.id, userId: user.id });
    setRequests(prev => prev.map(x => x.id === r.id ? { ...x, status: 2 } : x));
  }

  const filtered = requests.filter(r => {
    const k = requestStatusKey(r.status);
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
          <h2 className={styles.headerTitle}>Запити на вступ до команди</h2>
          <button className={styles.backBtn} onClick={() => navigate(teamId ? `/teams/${teamId}` : "/teams")}>Назад</button>
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
            {!loading && error && <p style={{ color: "#ff6b6b" }}>Помилка: {error}</p>}
            {!loading && !error && filtered.length === 0 && <p style={{ color: "#666" }}>Запитів немає</p>}
            {filtered.map(r => {
              const key = requestStatusKey(r.status);
              const displayName = r.fromUsername ?? r.fromUserId;
              return (
                <div key={r.id} className={`${styles.card} ${key === "new" ? styles.cardNew : key === "rejected" ? styles.cardRejected : styles.cardAccepted}`}>
                  <div
                    className={styles.cardTop}
                    style={{ cursor: "pointer" }}
                    onClick={() => navigate(`/users/${r.fromUserId}`)}
                  >
                    {r.fromAvatarUrl ? (
                      <img src={r.fromAvatarUrl} alt="" className={styles.avatar} style={{ objectFit: "cover" }} />
                    ) : (
                      <div className={styles.avatar} style={{ backgroundColor: avatarColor(r.fromUserId) }}>
                        {displayName.slice(0, 2).toUpperCase()}
                      </div>
                    )}
                    <div className={styles.meta}>
                      <span className={styles.name}>{displayName}</span>
                      <span className={styles.time}>{timeAgo(r.createdAt)}</span>
                    </div>
                    {key === "new"      && <span className={styles.newBadge}>Нове</span>}
                    {key === "rejected" && <span className={styles.rejectedBadge}>Відхилено</span>}
                    {key === "accepted" && <span className={styles.acceptedBadge}>Прийнято</span>}
                  </div>

                  {r.message && <p className={styles.text}>{r.message}</p>}
                  <p className={styles.text} style={{ fontSize: 12, color: "#888" }}>Роль: {r.role}</p>

                  {key === "new" && (
                    <div className={styles.actions}>
                      <button className={styles.acceptBtn} onClick={() => handleAccept(r)}>Прийняти</button>
                      <button className={styles.rejectBtn} onClick={() => handleReject(r)}>Відхилити</button>
                    </div>
                  )}
                  {key === "rejected" && <span className={styles.actionLabel} style={{ color: "#ff6b6b" }}>Відхилено</span>}
                  {key === "accepted" && <span className={styles.actionLabel} style={{ color: "#3fcca0" }}>Прийнято до команди</span>}
                </div>
              );
            })}
          </div>
        </main>

        <Moderation moderationPosition="relative" moderationMarginTop="auto" moderationTop="unset" moderationLeft="124px" />
      </div>
    </div>
  );
};

export default JoinRequestsListPage;