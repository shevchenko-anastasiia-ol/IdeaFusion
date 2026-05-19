import { FunctionComponent, useEffect, useRef, useState } from "react";
import { useNavigate } from "react-router-dom";
import FrameComponent2 from "../components/FrameComponent2";
import Moderation from "../components/Moderation";
import { teamsApi } from "../api/teams";
import { tagsApi } from "../api/posts";
import type { TeamEntity, TagDto } from "../api/types";
import { useAuth } from "../context/AuthContext";
import styles from "./NewPostPage.module.css";

const GATEWAY = "";

const TAG_LABELS: Record<string, string> = {
  design: "Дизайн", branding: "Дизайн",
  music: "Музика",
  photography: "Фото",
  animation: "Анімація",
  art: "Арт", illustration: "Арт", "street-art": "Арт",
  digital: "Цифровий",
  gamedev: "Gamedev",
  "3d": "3D",
  "ui-ux": "UI/UX",
  architecture: "Архітектура",
  collab: "Колаб",
  fashion: "Мода",
  film: "Фільм",
  poetry: "Поезія",
  traditional: "Традиційне",
  wip: "В процесі",
  writing: "Письменство",
};
function tagLabel(name: string): string {
  return TAG_LABELS[name.toLowerCase()] ?? (name.charAt(0).toUpperCase() + name.slice(1));
}

const NewPostPage: FunctionComponent = () => {
  const navigate = useNavigate();
  const { user, contentUserId } = useAuth();

  const [publisher,    setPublisher]    = useState<"self" | "team">("self");
  const [selectedTeam, setSelectedTeam] = useState<TeamEntity | null>(null);
  const [dropdownOpen, setDropdownOpen] = useState(false);
  const [myTeams,      setMyTeams]      = useState<TeamEntity[]>([]);

  const [title,       setTitle]       = useState('');
  const [description, setDescription] = useState('');
  const [link,        setLink]        = useState('');
  const [mediaFiles,     setMediaFiles]     = useState<File[]>([]);
  const [previews,       setPreviews]       = useState<string[]>([]);
  const [allTags,        setAllTags]        = useState<TagDto[]>([]);
  const [selectedTagIds, setSelectedTagIds] = useState<number[]>([]);
  const [submitting,     setSubmitting]     = useState(false);
  const [error,          setError]          = useState<string | null>(null);

  const dropdownRef = useRef<HTMLDivElement>(null);
  const fileInputRef = useRef<HTMLInputElement>(null);

  useEffect(() => {
    const handler = (e: MouseEvent) => {
      if (dropdownRef.current && !dropdownRef.current.contains(e.target as Node))
        setDropdownOpen(false);
    };
    document.addEventListener("mousedown", handler);
    return () => document.removeEventListener("mousedown", handler);
  }, []);

  useEffect(() => {
    if (!user) return;
    teamsApi.getByMember(user.id).then(setMyTeams).catch(() => {});
  }, [user]);

  useEffect(() => {
    tagsApi.getAll().then(setAllTags).catch(() => {});
  }, []);

  function handleFileChange(e: React.ChangeEvent<HTMLInputElement>) {
    if (!e.target.files) return;
    const newFiles = Array.from(e.target.files);
    setMediaFiles(prev => [...prev, ...newFiles]);
    setPreviews(prev => [...prev, ...newFiles.map(f => f.type.startsWith('image/') ? URL.createObjectURL(f) : '')]);
    e.target.value = '';
  }

  function removeFile(index: number) {
    setPreviews(prev => {
      if (prev[index]) URL.revokeObjectURL(prev[index]);
      return prev.filter((_, i) => i !== index);
    });
    setMediaFiles(prev => prev.filter((_, i) => i !== index));
  }

  async function handleSave(status: "Published" | "Draft") {
    if (!title.trim()) { setError('Введіть назву'); return; }
    if (publisher === "team" && !selectedTeam) { setError('Оберіть команду'); return; }
    setSubmitting(true);
    setError(null);

    try {
      const token = localStorage.getItem('accessToken');

      // When posting as a team, ensure a CollaborationSnapshot exists in Content API
      let collaborationSnapshotId: number | null = null;
      if (publisher === "team" && selectedTeam) {
        const snapRes = await fetch(`${GATEWAY}/api/collaborationSnapshot/ensure`, {
          method: 'POST',
          headers: {
            'Content-Type': 'application/json',
            ...(token ? { Authorization: `Bearer ${token}` } : {}),
          },
          body: JSON.stringify({
            teamName: selectedTeam.name,
            mongoTeamId: selectedTeam.id,
            avatarUrl: selectedTeam.avatarUrl ?? null,
          }),
        });
        if (snapRes.ok) {
          const snapData = await snapRes.json();
          collaborationSnapshotId = snapData.collaborationSnapshotId ?? null;
        }
      }

      const formData = new FormData();
      formData.append('Title', title.trim());
      if (description) formData.append('Description', description);
      if (link) formData.append('ExternalLink', link);
      // DB constraint: post must have either PostAuthorId OR CollaborationSnapshotId, not both
      if (collaborationSnapshotId != null) {
        formData.append('CollaborationSnapshotId', String(collaborationSnapshotId));
      } else if (contentUserId) {
        formData.append('PostAuthorId', String(contentUserId));
      }
      formData.append('CreatedBy', user?.id ?? '');
      formData.append('AuthorUsername', user?.userName ?? user?.email?.split('@')[0] ?? '');
      if (user?.avatarUrl) formData.append('AuthorAvatarUrl', user.avatarUrl);
      formData.append('Status', status);
      mediaFiles.forEach(f => formData.append('MediaFiles', f));
      selectedTagIds.forEach(id => formData.append('TagIds', String(id)));

      const res = await fetch(`${GATEWAY}/api/post`, {
        method: 'POST',
        headers: token ? { Authorization: `Bearer ${token}` } : {},
        body: formData,
      });

      if (!res.ok) {
        const text = await res.text();
        throw new Error(text || `HTTP ${res.status}`);
      }

      if (publisher === "team" && selectedTeam) {
        const created = await res.json();
        const teamPostRes = await fetch(`${GATEWAY}/api/team-posts`, {
          method: 'POST',
          headers: {
            'Content-Type': 'application/json',
            ...(token ? { Authorization: `Bearer ${token}` } : {}),
          },
          body: JSON.stringify({
            postId: String(created.postId),
            teamId: selectedTeam.id,
            authorUserId: user?.id ?? '',
            authorUsername: user?.userName ?? user?.email?.split('@')[0] ?? '',
            title: title.trim(),
          }),
        });
        if (!teamPostRes.ok) {
          const body = await teamPostRes.json().catch(() => null);
          const msg = body?.message ?? body?.title ?? `HTTP ${teamPostRes.status}`;
          throw new Error(`Не вдалося зв'язати пост з командою: ${msg}`);
        }
      }

      navigate(-1);
    } catch (ex: any) {
      setError(ex.message ?? 'Помилка збереження');
    } finally {
      setSubmitting(false);
    }
  }

  return (
    <div className={styles.page}>
      <FrameComponent2 />

      <div className={styles.rightCol}>
        <header className={styles.header}>
          <h1 className={styles.pageTitle}>Створення нового посту</h1>
          <button className={styles.cancelBtn} onClick={() => navigate(-1)}>Скасувати</button>
        </header>

        <div className={styles.formArea}>
          <div className={styles.card}>
            <p className={styles.cardLabel}>Основне</p>
            <div className={styles.field}>
              <label className={styles.fieldLabel}>Назва</label>
              <input
                className={styles.input}
                type="text"
                placeholder='«Кадри, що дихають»'
                value={title}
                onChange={e => setTitle(e.target.value)}
              />
            </div>
            <div className={styles.field}>
              <label className={styles.fieldLabel}>Опис</label>
              <textarea
                className={styles.textarea}
                rows={4}
                placeholder="Серія анімованих ілюстрацій..."
                value={description}
                onChange={e => setDescription(e.target.value)}
              />
            </div>
          </div>

          <div className={styles.bottomRow}>
            <div className={styles.leftCol}>
              <div className={styles.card}>
                <p className={styles.cardLabel}>Від кого публікувати</p>
                <div className={styles.publisherOptions}>
                  <div
                    className={publisher === "self" ? styles.publisherOption : styles.publisherOptionAlt}
                    onClick={() => setPublisher("self")}
                  >
                    <span className={styles.publisherTitle}>Від свого імені</span>
                    <span className={styles.publisherSub}>{user?.email ?? 'Ви'}</span>
                  </div>
                  <div
                    className={publisher === "team" ? styles.publisherOption : styles.publisherOptionAlt}
                    onClick={() => setPublisher("team")}
                  >
                    <span className={styles.publisherTitle}>Від імені команди</span>
                    <span className={styles.publisherSub}>
                      {publisher === "team" && selectedTeam ? selectedTeam.name : "Оберіть команду ↓"}
                    </span>
                  </div>
                </div>

                {publisher === "team" && (
                  <div className={styles.teamDropdownWrapper} ref={dropdownRef}>
                    <button
                      className={styles.teamDropdownTrigger}
                      onClick={() => setDropdownOpen(v => !v)}
                      type="button"
                    >
                      <span>{selectedTeam?.name ?? "Оберіть команду"}</span>
                      <span className={`${styles.dropdownArrow} ${dropdownOpen ? styles.dropdownArrowOpen : ""}`}>▾</span>
                    </button>
                    {dropdownOpen && (
                      <ul className={styles.teamDropdownList}>
                        {myTeams.length === 0 && (
                          <li className={styles.teamDropdownItem} style={{ color: "#666" }}>Немає команд</li>
                        )}
                        {myTeams.map(t => (
                          <li
                            key={t.id}
                            className={`${styles.teamDropdownItem} ${selectedTeam?.id === t.id ? styles.teamDropdownItemActive : ""}`}
                            onClick={() => { setSelectedTeam(t); setDropdownOpen(false); }}
                          >
                            {t.name}
                            {selectedTeam?.id === t.id && <span className={styles.checkmark}>✓</span>}
                          </li>
                        ))}
                      </ul>
                    )}
                  </div>
                )}
              </div>

              <div className={styles.card}>
                <p className={styles.cardLabel}>Зовнішнє посилання</p>
                <input
                  className={styles.input}
                  type="text"
                  placeholder="behance.net/kadry-project"
                  value={link}
                  onChange={e => setLink(e.target.value)}
                />
                <p className={styles.hint}>Behance, Dribbble, Figma, SoundCloud, GitHub тощо</p>
              </div>
            </div>

            <div className={styles.mediaCard}>
              <p className={styles.cardLabel}>Медія</p>

              {mediaFiles.length === 0 ? (
                <label className={styles.dropzone} htmlFor="media-upload">
                  <p className={styles.dropzoneText}>
                    Оберіть файли або перетягніть сюди<br />
                    PNG, JPG, MP4, MP3, PDF · до 50 МБ
                  </p>
                </label>
              ) : (
                <div style={{ display: "flex", flexDirection: "column", gap: 8 }}>
                  {mediaFiles.map((f, i) => {
                    const isImage = f.type.startsWith('image/');
                    const isVideo = f.type.startsWith('video/');
                    const isAudio = f.type.startsWith('audio/');
                    const icon = isVideo ? '🎬' : isAudio ? '🎵' : '📄';
                    return (
                      <div key={i} style={{
                        display: "flex", alignItems: "center", gap: 10,
                        background: "#242424", borderRadius: 12, padding: "8px 14px",
                      }}>
                        {isImage && previews[i] ? (
                          <img src={previews[i]} alt={f.name} style={{ width: 40, height: 40, objectFit: 'cover', borderRadius: 8, flexShrink: 0 }} />
                        ) : (
                          <span style={{ fontSize: 22, flexShrink: 0 }}>{icon}</span>
                        )}
                        <span style={{ flex: 1, fontSize: 13, color: "#ccc", overflow: "hidden", textOverflow: "ellipsis", whiteSpace: "nowrap" }}>
                          {f.name}
                        </span>
                        <span style={{ fontSize: 11, color: "#666", flexShrink: 0 }}>
                          {(f.size / 1024 / 1024).toFixed(1)} МБ
                        </span>
                        <button
                          type="button"
                          onClick={() => removeFile(i)}
                          style={{ background: "none", border: "none", color: "#ff6b6b", cursor: "pointer", fontSize: 16, padding: 0 }}
                        >×</button>
                      </div>
                    );
                  })}
                  <label
                    htmlFor="media-upload"
                    style={{ cursor: "pointer", color: "#7c5cfc", fontSize: 13, textAlign: "center", padding: "8px 0" }}
                  >
                    + Додати ще
                  </label>
                </div>
              )}

              <input
                id="media-upload"
                ref={fileInputRef}
                type="file"
                multiple
                accept="image/*,video/*,audio/*,.pdf"
                style={{ display: "none" }}
                onChange={handleFileChange}
              />
            </div>
          </div>

          <div className={styles.card}>
            <p className={styles.cardLabel}>Теги</p>
            {allTags.length === 0
              ? <p style={{ color: '#555', fontSize: 13, margin: 0 }}>Завантаження тегів...</p>
              : (
                <div className={styles.tagsWrap}>
                  {allTags.map(tag => (
                    <button
                      key={tag.tagId}
                      className={selectedTagIds.includes(tag.tagId) ? styles.tagBtnActive : styles.tagBtn}
                      onClick={() => setSelectedTagIds(prev =>
                        prev.includes(tag.tagId) ? prev.filter(id => id !== tag.tagId) : [...prev, tag.tagId]
                      )}
                      type="button"
                    >
                      {tagLabel(tag.name)}
                    </button>
                  ))}
                </div>
              )
            }
          </div>

          {error && <p style={{ color: "#ff6b6b", marginBottom: 8 }}>{error}</p>}

          <div className={styles.actions}>
            <button
              className={styles.publishBtn}
              disabled={submitting}
              onClick={() => handleSave("Published")}
            >
              {submitting ? '...' : 'Опублікувати'}
            </button>
          </div>
        </div>

        <Moderation moderationPosition="relative" moderationTop="unset" moderationLeft="unset" />
      </div>
    </div>
  );
};

export default NewPostPage;