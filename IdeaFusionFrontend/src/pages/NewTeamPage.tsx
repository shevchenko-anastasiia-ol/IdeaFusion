import { FunctionComponent, useRef, useState } from "react";
import { useNavigate } from "react-router-dom";
import FrameComponent2 from "../components/FrameComponent2";
import Moderation from "../components/Moderation";
import { teamsApi } from "../api/teams";
import { mediaApi } from "../api/posts";
import { useAuth } from "../context/AuthContext";
import styles from "./NewTeamPage.module.css";

export type Component1Type = {};

const CATEGORIES = ["Дизайн", "Розробка", "Музика", "Арт", "Фото", "Відео", "Анімація", "Gamedev", "Медіа"];
const CLASSIFICATIONS = [
  "UI/UX дизайн","Графічний дизайн","Веб-дизайн","Моушн-дизайн","3D дизайн",
  "Веб-розробка","Мобільна розробка","Gamedev","Backend розробка","Ілюстрація",
  "Концепт-арт","Анімація","Комікси та манга","Композиція","Саундтрек / OST",
  "Звукорежисура","Вокал","Фотографія","Відеомонтаж","Сценарист",
];
const ROLES = [
  "UI/UX дизайнер","Графічний дизайнер","Веб-розробник","Мобільний розробник",
  "Game developer","Backend розробник","Ілюстратор","Аніматор","Концепт-художник",
  "Композитор","Звукорежисер","Вокаліст","Фотограф","Відеомонтажер","Сценарист","3D художник",
];

const Component1: FunctionComponent<Component1Type> = () => {
  const navigate       = useNavigate();
  const { user }       = useAuth();
  const [name,         setName]         = useState('');
  const [description,  setDescription]  = useState('');
  const [category,     setCategory]     = useState('');
  const [selectedTags, setSelectedTags] = useState<string[]>([]);
  const [selectedRoles,setSelectedRoles]= useState<string[]>([]);
  const [customTag,    setCustomTag]    = useState('');
  const [avatarFile,   setAvatarFile]   = useState<File | null>(null);
  const [avatarPreview,setAvatarPreview]= useState<string | null>(null);
  const [submitting,   setSubmitting]   = useState(false);
  const [error,        setError]        = useState<string | null>(null);
  const fileInputRef = useRef<HTMLInputElement>(null);

  function handleAvatarChange(e: React.ChangeEvent<HTMLInputElement>) {
    const file = e.target.files?.[0];
    if (!file) return;
    setAvatarFile(file);
    setAvatarPreview(URL.createObjectURL(file));
  }

  function toggleMulti(list: string[], setList: (v: string[]) => void, val: string) {
    setList(list.includes(val) ? list.filter(v => v !== val) : [...list, val]);
  }

  async function handleCreate() {
    if (!name.trim() || !description.trim() || !category) {
      setError("Заповніть назву, опис і категорію");
      return;
    }
    if (!user) { navigate('/login'); return; }
    setSubmitting(true);
    setError(null);
    try {
      const tags = customTag.trim() ? [...selectedTags, customTag.trim()] : selectedTags;
      let teamAvatarUrl: string | undefined;
      if (avatarFile) {
        try { teamAvatarUrl = await mediaApi.upload(avatarFile); } catch {}
      }
      const team = await teamsApi.create({
        name: name.trim(),
        description: description.trim(),
        category,
        tags,
        userId: user.id,
        username: user.userName ?? user.email.split('@')[0],
        avatarUrl: user.avatarUrl,
        teamAvatarUrl,
      });
      // Add required roles
      for (const role of selectedRoles) {
        try {
          await fetch(`/api/team/${team.id}/required-roles`, {
            method: 'POST',
            headers: {
              'Content-Type': 'application/json',
              Authorization: `Bearer ${localStorage.getItem('accessToken')}`,
            },
            body: JSON.stringify({ teamId: team.id, role, userId: user.id }),
          });
        } catch {}
      }
      navigate('/team');
    } catch (ex: any) {
      setError(ex.message);
    } finally {
      setSubmitting(false);
    }
  }

  return (
    <div className={styles.page}>
      <FrameComponent2 />

      <div className={styles.content}>
        <header className={styles.header}>
          <h1 className={styles.title}>Створення нової команди</h1>
          <button className={styles.cancelBtn} onClick={() => navigate(-1)}>Скасувати</button>
        </header>

        <div className={styles.form}>
          <section className={styles.section}>
            <p className={styles.sectionTitle}>Основне</p>

            <div className={styles.field}>
              <label className={styles.label}>Назва</label>
              <input className={styles.input} type="text" value={name} onChange={e => setName(e.target.value)} />
            </div>

            <div className={styles.field}>
              <label className={styles.label}>Опис</label>
              <textarea className={styles.textarea} rows={4} value={description} onChange={e => setDescription(e.target.value)} />
            </div>

            <div className={styles.field}>
              <label className={styles.label}>Категорія</label>
              <select className={styles.input} value={category} onChange={e => setCategory(e.target.value)}>
                <option value="">— Оберіть категорію —</option>
                {CATEGORIES.map(c => <option key={c}>{c}</option>)}
              </select>
            </div>
          </section>

          <div className={styles.bottomRow}>
            <section className={styles.sectionHalf}>
              <label className={styles.label}>Оберіть класифікацію</label>
              <select className={styles.input} multiple size={5} value={selectedTags} onChange={e => setSelectedTags(Array.from(e.target.selectedOptions, o => o.value))}>
                {CLASSIFICATIONS.map(c => <option key={c}>{c}</option>)}
              </select>
              <p className={styles.hint}>Утримуйте Ctrl (або Cmd) для вибору декількох</p>

              <div className={styles.field}>
                <label className={styles.label}>Створіть власну</label>
                <input className={styles.input} type="text" value={customTag} onChange={e => setCustomTag(e.target.value)} placeholder="Власна класифікація..." />
              </div>

              <div className={styles.field}>
                <label className={styles.label}>Необхідні ролі</label>
                <select className={styles.input} multiple size={5} value={selectedRoles} onChange={e => setSelectedRoles(Array.from(e.target.selectedOptions, o => o.value))}>
                  {ROLES.map(r => <option key={r}>{r}</option>)}
                </select>
                <p className={styles.hint}>Утримуйте Ctrl (або Cmd) для вибору декількох ролей</p>
              </div>
            </section>

            <section className={styles.sectionHalf}>
              <p className={styles.sectionTitle}>Аватар команди</p>
              <label
                className={styles.uploadArea}
                htmlFor="avatar-upload"
                style={avatarPreview ? { backgroundImage: `url(${avatarPreview})`, backgroundSize: 'cover', backgroundPosition: 'center' } : {}}
              >
                {!avatarPreview && (
                  <p className={styles.uploadText}>
                    Оберіть файл або перетягніть сюди<br />
                    PNG, JPG · до 50 МБ
                  </p>
                )}
              </label>
              <input
                id="avatar-upload"
                ref={fileInputRef}
                type="file"
                accept="image/*"
                style={{ display: "none" }}
                onChange={handleAvatarChange}
              />
              {avatarPreview && (
                <button
                  type="button"
                  onClick={() => { setAvatarFile(null); setAvatarPreview(null); if (fileInputRef.current) fileInputRef.current.value = ''; }}
                  style={{ marginTop: 8, background: 'none', border: 'none', color: '#ff6b6b', cursor: 'pointer', fontSize: 13 }}
                >
                  Видалити фото
                </button>
              )}
            </section>
          </div>

          {error && <p style={{ color: "#ff6b6b", marginBottom: 8 }}>{error}</p>}

          <div className={styles.submitRow}>
            <button className={styles.submitBtn} disabled={submitting} onClick={handleCreate}>
              {submitting ? '...' : 'Створити команду'}
            </button>
          </div>
        </div>

        <Moderation moderationPosition="relative" moderationTop="unset" moderationLeft="unset" />
      </div>
    </div>
  );
};

export default Component1;