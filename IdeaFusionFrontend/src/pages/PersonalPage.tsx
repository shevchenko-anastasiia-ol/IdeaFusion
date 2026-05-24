import { FunctionComponent, useEffect, useRef, useState } from "react";
import { useNavigate } from "react-router-dom";
import FrameComponent2 from "../components/FrameComponent2";
import Moderation from "../components/Moderation";
import { useAuth } from "../context/AuthContext";
import { authApi } from "../api/auth";
import { aggregatorApi } from "../api/aggregator";
import type { PostWithEngagementDto } from "../api/types";
import styles from "./PersonalPage.module.css";

const FILTERS = ["Всі", "Дизайн", "Музика", "Арт", "Анімація"];

const TAG_MAP: Record<string, string[]> = {
  "Дизайн":   ["design", "branding", "ui-ux"],
  "Музика":   ["music"],
  "Арт":      ["art", "illustration", "street-art"],
  "Анімація": ["animation"],
};

const BG_PALETTE = ["#3fcca0","#f87c6b","#7c5cfc","#4a4a6a","#ffb347","#5bc8f5","#c756ff"];
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

const PersonalPage: FunctionComponent = () => {
  const navigate = useNavigate();
  const { user, logout, updateAvatar, updateProfile, contentUserId } = useAuth();

  // Portfolio
  const [activeFilter, setActiveFilter] = useState("Всі");
  const [posts,    setPosts]    = useState<PostWithEngagementDto[]>([]);
  const [loading,  setLoading]  = useState(false);

  // Avatar
  const fileInputRef  = useRef<HTMLInputElement>(null);
  const [uploading, setUploading] = useState(false);

  // Profile form
  const [fullName,       setFullName]       = useState(user?.fullName ?? '');
  const [specialization, setSpecialization] = useState(user?.specialization ?? '');
  const [profileSaving,  setProfileSaving]  = useState(false);
  const [profileMsg,     setProfileMsg]     = useState<{ ok: boolean; text: string } | null>(null);

  // Password form
  const [currentPwd,  setCurrentPwd]  = useState('');
  const [newPwd,      setNewPwd]      = useState('');
  const [confirmPwd,  setConfirmPwd]  = useState('');
  const [pwdSaving,   setPwdSaving]   = useState(false);
  const [pwdMsg,      setPwdMsg]      = useState<{ ok: boolean; text: string } | null>(null);

  const userName = user?.userName ?? user?.email?.split('@')[0] ?? '';
  const initials = (user?.fullName ?? userName).slice(0, 2).toUpperCase();

  // Sync form defaults when user loads
  useEffect(() => {
    setFullName(user?.fullName ?? '');
    setSpecialization(user?.specialization ?? '');
  }, [user?.fullName, user?.specialization]);

  useEffect(() => {
    if (!user || contentUserId == null) return;
    setLoading(true);
    aggregatorApi.getPortfolio(contentUserId, user.id, contentUserId)
      .then(setPosts)
      .catch(() => {})
      .finally(() => setLoading(false));
  }, [user, contentUserId]);

  const filtered = activeFilter === "Всі"
    ? posts
    : posts.filter(p => {
        const mapped = TAG_MAP[activeFilter] ?? [activeFilter.toLowerCase()];
        return p.post.tags.some(t => mapped.includes(t.toLowerCase()));
      });

  async function handleLogout() {
    await logout();
    navigate('/login');
  }

  async function handleAvatarFile(e: React.ChangeEvent<HTMLInputElement>) {
    const file = e.target.files?.[0];
    if (!file) return;
    setUploading(true);
    try {
      await updateAvatar(file);
    } catch (err) {
      alert((err as Error).message ?? 'Не вдалось завантажити фото');
    } finally {
      setUploading(false);
      if (fileInputRef.current) fileInputRef.current.value = '';
    }
  }

  async function handleSaveProfile() {
    setProfileSaving(true);
    setProfileMsg(null);
    try {
      await updateProfile({ fullName: fullName.trim() || undefined, specialization: specialization.trim() || undefined });
      setProfileMsg({ ok: true, text: 'Зміни збережено' });
    } catch (err) {
      setProfileMsg({ ok: false, text: (err as Error).message ?? 'Помилка збереження' });
    } finally {
      setProfileSaving(false);
    }
  }

  async function handleChangePassword() {
    if (newPwd !== confirmPwd) {
      setPwdMsg({ ok: false, text: 'Паролі не збігаються' });
      return;
    }
    if (newPwd.length < 8) {
      setPwdMsg({ ok: false, text: 'Пароль має містити мінімум 8 символів' });
      return;
    }
    setPwdSaving(true);
    setPwdMsg(null);
    try {
      await authApi.changePassword({ currentPassword: currentPwd, newPassword: newPwd });
      setPwdMsg({ ok: true, text: 'Пароль успішно змінено' });
      setCurrentPwd(''); setNewPwd(''); setConfirmPwd('');
    } catch (err) {
      setPwdMsg({ ok: false, text: (err as Error).message ?? 'Помилка зміни паролю' });
    } finally {
      setPwdSaving(false);
    }
  }

  const displayName = user?.fullName || userName;
  const displaySub  = user?.specialization || user?.email || '';

  return (
    <div className={styles.page}>
      <FrameComponent2 />

      <div className={styles.content}>
        <header className={styles.header}>
          <h2 className={styles.headerTitle}>Особиста сторінка</h2>
          <button className={styles.exitBtn} onClick={handleLogout}>Вийти</button>
        </header>

        <div className={styles.profileCard}>
          <div
            className={styles.profileAvatar}
            style={user?.avatarUrl
              ? { backgroundImage: `url(${user.avatarUrl})`, backgroundSize: 'cover', backgroundPosition: 'center' }
              : {}}
          >
            {!user?.avatarUrl && initials}
          </div>
          <div className={styles.profileInfo}>
            <h1 className={styles.profileName}>{displayName}</h1>
            <p className={styles.profileSub}>{displaySub}</p>
          </div>
          <button
            className={styles.editPhotoBtn}
            onClick={() => fileInputRef.current?.click()}
            disabled={uploading}
          >
            {uploading ? 'Завантаження...' : 'Редагувати фото профілю'}
          </button>
          <input
            ref={fileInputRef}
            type="file"
            accept="image/jpeg,image/png,image/webp,image/gif"
            style={{ display: 'none' }}
            onChange={handleAvatarFile}
          />
        </div>

        <div className={styles.formsRow}>
          {/* Personal info */}
          <div className={styles.card}>
            <h3 className={styles.cardTitle}>Особиста інформація</h3>
            <div className={styles.field}>
              <label className={styles.fieldLabel}>Email</label>
              <input className={styles.input} type="email" value={user?.email ?? ''} readOnly />
            </div>
            <div className={styles.field}>
              <label className={styles.fieldLabel}>Повне ім'я</label>
              <input
                className={styles.input}
                type="text"
                placeholder="Введіть повне ім'я"
                value={fullName}
                onChange={e => setFullName(e.target.value)}
              />
            </div>
            <div className={styles.field}>
              <label className={styles.fieldLabel}>Спеціалізація</label>
              <input
                className={styles.input}
                type="text"
                placeholder="Напр.: UI/UX Design, Figma, Animation"
                value={specialization}
                onChange={e => setSpecialization(e.target.value)}
              />
            </div>
            <div className={styles.field}>
              <label className={styles.fieldLabel}>Ролі</label>
              <input className={styles.input} type="text" value={user?.roles?.join(', ') ?? 'Користувач'} readOnly />
            </div>
            {profileMsg && (
              <p style={{ margin: 0, fontSize: 13, color: profileMsg.ok ? '#3fcca0' : '#ff6b6b' }}>
                {profileMsg.text}
              </p>
            )}
            <button
              className={styles.saveBtn}
              onClick={handleSaveProfile}
              disabled={profileSaving}
            >
              {profileSaving ? 'Збереження...' : 'Зберегти зміни'}
            </button>
          </div>

          {/* Change password */}
          <div className={styles.card}>
            <h3 className={styles.cardTitle}>Зміна паролю</h3>
            <div className={styles.field}>
              <label className={styles.fieldLabel}>Поточний пароль</label>
              <input
                className={styles.input}
                type="password"
                placeholder="••••••••"
                value={currentPwd}
                onChange={e => setCurrentPwd(e.target.value)}
              />
            </div>
            <div className={styles.field}>
              <label className={styles.fieldLabel}>Новий пароль</label>
              <input
                className={styles.input}
                type="password"
                placeholder="Мін. 8 символів"
                value={newPwd}
                onChange={e => setNewPwd(e.target.value)}
              />
            </div>
            <div className={styles.field}>
              <label className={styles.fieldLabel}>Підтвердіть пароль</label>
              <input
                className={styles.input}
                type="password"
                placeholder="••••••••"
                value={confirmPwd}
                onChange={e => setConfirmPwd(e.target.value)}
              />
            </div>
            {pwdMsg && (
              <p style={{ margin: 0, fontSize: 13, color: pwdMsg.ok ? '#3fcca0' : '#ff6b6b' }}>
                {pwdMsg.text}
              </p>
            )}
            <button
              className={styles.saveBtn}
              onClick={handleChangePassword}
              disabled={pwdSaving || !currentPwd || !newPwd || !confirmPwd}
            >
              {pwdSaving ? 'Збереження...' : 'Оновити пароль'}
            </button>
          </div>
        </div>

        <div className={styles.portfolioSection}>
          <div className={styles.portfolioHeader}>
            <h2 className={styles.portfolioTitle}>Портфоліо</h2>
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
            {loading && <p style={{ color: "#aaa" }}>Завантаження...</p>}
            {!loading && filtered.length === 0 && <p style={{ color: "#777" }}>Постів немає</p>}
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
                  <button
                    className={styles.cardEditBtn}
                    title="Редагувати пост"
                    onClick={(e) => { e.stopPropagation(); navigate(`/posts/edit?id=${item.post.postId}`); }}
                  >
                    <svg width="13" height="13" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2.2" strokeLinecap="round" strokeLinejoin="round">
                      <path d="M11 4H4a2 2 0 0 0-2 2v14a2 2 0 0 0 2 2h14a2 2 0 0 0 2-2v-7"/>
                      <path d="M18.5 2.5a2.121 2.121 0 0 1 3 3L12 15l-4 1 1-4 9.5-9.5z"/>
                    </svg>
                  </button>
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
                        <span
                          className={styles.cardTag}
                          style={{ color: "#a48fff", border: "1px solid #7c5cfc", background: "rgba(124,92,252,0.15)" }}
                        >
                          {item.post.tags[0]}
                        </span>
                      )}
                    </div>
                  </div>
                  <span className={styles.cardAuthor}>{item.post.collaboration?.name ?? item.post.author?.userName ?? ''}</span>
                </div>
              </div>
              );
            })}
          </div>
        </div>
        <Moderation moderationPosition="relative" moderationTop="unset" moderationLeft="unset" />
      </div>
    </div>
  );
};

export default PersonalPage;