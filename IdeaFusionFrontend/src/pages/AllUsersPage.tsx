import { FunctionComponent, useEffect, useState } from "react";
import { useNavigate } from "react-router-dom";
import FrameComponent2 from "../components/FrameComponent2";
import Moderation from "../components/Moderation";
import { useAuth } from "../context/AuthContext";
import type { UserDto } from "../api/types";
import { api } from "../api/client";
import styles from "./AllUsersPage.module.css";

const AVATAR_PALETTE = ["#4002aa","#00c9a7","#ff6b6b","#ffb347","#e040fb","#29b6f6","#ef5350","#66bb6a","#7c5cfc","#26c6da","#ec407a"];
function avatarColor(seed: string): string {
  let h = 0;
  for (let i = 0; i < seed.length; i++) h = (h * 31 + seed.charCodeAt(i)) >>> 0;
  return AVATAR_PALETTE[h % AVATAR_PALETTE.length];
}

const UserCard: FunctionComponent<{ user: UserDto; meId?: string; isAdmin?: boolean; onDelete?: (id: string) => void }> = ({ user, meId, isAdmin, onDelete }) => {
  const navigate = useNavigate();
  const displayName = user.fullName ?? user.userName ?? user.email.split('@')[0];
  const initials    = displayName.slice(0, 2).toUpperCase();
  const isMe        = user.id === meId;

  return (
    <div className={styles.userCard} onClick={() => navigate(`/users/${user.id}`)} style={{ cursor: "pointer" }}>
      {user.avatarUrl ? (
        <img src={user.avatarUrl} alt="" className={styles.avatar} style={{ objectFit: "cover", borderRadius: "50%" }} />
      ) : (
        <div className={styles.avatar} style={{ backgroundColor: avatarColor(user.id) }}>{initials}</div>
      )}
      <div className={styles.cardInfo}>
        <span className={styles.userName}>{displayName}</span>
        <span className={styles.userRole}>{user.specialization ?? user.roles.join(', ') ?? 'Користувач'}</span>
      </div>
      {!isMe && (
        <button
          className={styles.inviteBtn}
          onClick={e => { e.stopPropagation(); navigate(`/team/invite-to-team?userId=${user.id}`); }}
        >
          Запросити
        </button>
      )}
      {isAdmin && !isMe && !user.roles?.includes('Admin') && (
        <button
          style={{ background: "none", border: "none", cursor: "pointer", color: "#ff6b6b", padding: "4px 8px", marginLeft: 4, flexShrink: 0 }}
          title="Видалити користувача"
          onClick={e => { e.stopPropagation(); onDelete?.(user.id); }}
        >
          <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
            <polyline points="3 6 5 6 21 6"/><path d="M19 6l-1 14a2 2 0 0 1-2 2H8a2 2 0 0 1-2-2L5 6"/><path d="M10 11v6"/><path d="M14 11v6"/><path d="M9 6V4a1 1 0 0 1 1-1h4a1 1 0 0 1 1 1v2"/>
          </svg>
        </button>
      )}
    </div>
  );
};

const PAGE_SIZE = 15;

const AllUsersPage: FunctionComponent = () => {
  const navigate = useNavigate();
  const { user: me } = useAuth();
  const [users,   setUsers]   = useState<UserDto[]>([]);
  const [loading, setLoading] = useState(true);
  const [error,   setError]   = useState<string | null>(null);
  const [page,    setPage]    = useState(1);

  useEffect(() => {
    api.get<UserDto[]>('/api/users')
      .then(setUsers)
      .catch((e: Error) => setError(e.message))
      .finally(() => setLoading(false));
  }, []);

  const isAdmin = me?.roles?.includes('Admin');

  async function handleDeleteUser(userId: string) {
    if (!confirm('Видалити цього користувача назавжди?')) return;
    try {
      await api.delete<void>(`/api/users/${userId}`);
      setUsers(prev => prev.filter(u => u.id !== userId));
    } catch (e: any) {
      alert(e.message ?? 'Помилка видалення');
    }
  }

  const allVisible = users.filter(u => u.id !== me?.id);
  const totalPages = Math.max(1, Math.ceil(allVisible.length / PAGE_SIZE));
  const pageUsers  = allVisible.slice((page - 1) * PAGE_SIZE, page * PAGE_SIZE);

  return (
    <div className={styles.page}>
      <FrameComponent2 />

      <div className={styles.rightCol}>
        <header className={styles.header}>
          <button className={styles.backBtn} onClick={() => navigate(-1)}>← Назад</button>
          <h1 className={styles.pageTitle}>Всі користувачі</h1>
        </header>

        <main className={styles.grid}>
          {loading && <p style={{ color: "#aaa", padding: 24 }}>Завантаження...</p>}
          {!loading && error && !isAdmin && (
            <p style={{ color: "#777", padding: 24 }}>Список користувачів доступний лише адміністраторам.</p>
          )}
          {!loading && error && isAdmin && (
            <p style={{ color: "#ff6b6b", padding: 24 }}>Помилка: {error}</p>
          )}
          {!loading && !error && pageUsers.map(u => (
            <UserCard key={u.id} user={u} meId={me?.id} isAdmin={isAdmin} onDelete={handleDeleteUser} />
          ))}
        </main>

        {!loading && !error && allVisible.length > PAGE_SIZE && (
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

export default AllUsersPage;