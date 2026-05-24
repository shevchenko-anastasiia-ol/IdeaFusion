import { FunctionComponent, useEffect, useRef, useState } from "react";
import { useNavigate, useSearchParams } from "react-router-dom";
import FrameComponent2 from "../components/FrameComponent2";
import Moderation from "../components/Moderation";
import { postsApi, tagsApi } from "../api/posts";
import type { TagDto } from "../api/types";
import { useAuth } from "../context/AuthContext";
import styles from "./EditPostPage.module.css";

function getMediaType(url: string): 'image' | 'video' | 'audio' | 'pdf' {
  const ext = url.split('?')[0].split('.').pop()?.toLowerCase() ?? '';
  if (['mp4', 'webm', 'mov', 'avi', 'mkv'].includes(ext)) return 'video';
  if (['mp3', 'wav', 'ogg', 'aac', 'm4a', 'flac'].includes(ext)) return 'audio';
  if (ext === 'pdf') return 'pdf';
  return 'image';
}

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

const EditPostPage: FunctionComponent = () => {
  const navigate = useNavigate();
  const [searchParams] = useSearchParams();
  const { user } = useAuth();
  const postId = searchParams.get('id') ? parseInt(searchParams.get('id')!) : null;
  const fileInputRef = useRef<HTMLInputElement>(null);

  const [pageTitle,     setPageTitle]     = useState('Редагування поста');
  const [title,         setTitle]         = useState('');
  const [description,   setDescription]   = useState('');
  const [externalLink,  setExternalLink]  = useState('');
  const [allTags,       setAllTags]       = useState<TagDto[]>([]);
  const [selectedTagIds, setSelectedTagIds] = useState<number[]>([]);
  const [existingMedia, setExistingMedia] = useState<string[]>([]);
  const [newFiles,      setNewFiles]      = useState<File[]>([]);
  const [newPreviews,   setNewPreviews]   = useState<string[]>([]);

  const [loading,  setLoading]  = useState(false);
  const [saving,   setSaving]   = useState(false);
  const [deleting, setDeleting] = useState(false);
  const [error,    setError]    = useState<string | null>(null);
  const [saved,    setSaved]    = useState(false);

  useEffect(() => {
    if (!postId) return;
    setLoading(true);
    Promise.all([
      postsApi.getById(postId),
      tagsApi.getAll(),
      tagsApi.getByPost(postId),
    ])
      .then(([post, tagList, postTags]) => {
        setTitle(post.title);
        setDescription(post.description ?? '');
        setExternalLink(post.externalLink ?? '');
        setPageTitle(`Редагування: ${post.title}`);
        setExistingMedia(post.mediaUrls ?? []);
        setAllTags(tagList);
        setSelectedTagIds(postTags.map(t => t.tagId));
      })
      .catch(e => setError((e as Error).message))
      .finally(() => setLoading(false));
  }, [postId]);

  function toggleTag(tagId: number) {
    setSelectedTagIds(prev =>
      prev.includes(tagId) ? prev.filter(id => id !== tagId) : [...prev, tagId]
    );
  }

  function handleFileChange(e: React.ChangeEvent<HTMLInputElement>) {
    const picked = Array.from(e.target.files ?? []);
    if (!picked.length) return;
    setNewFiles(prev => [...prev, ...picked]);
    picked.forEach(f => {
      const url = URL.createObjectURL(f);
      setNewPreviews(prev => [...prev, url]);
    });
    if (fileInputRef.current) fileInputRef.current.value = '';
  }

  function removeNewFile(idx: number) {
    setNewFiles(prev => prev.filter((_, i) => i !== idx));
    setNewPreviews(prev => {
      URL.revokeObjectURL(prev[idx]);
      return prev.filter((_, i) => i !== idx);
    });
  }

  async function handleSave() {
    if (!postId || !user || !title.trim()) return;
    setSaving(true);
    setError(null);
    setSaved(false);
    try {
      await postsApi.update(postId, {
        title: title.trim(),
        description: description.trim() || undefined,
        externalLink: externalLink.trim() || undefined,
        tagIds: selectedTagIds,
        updatedBy: user.id,
        newMediaFiles: newFiles.length > 0 ? newFiles : undefined,
      });
      setPageTitle(`Редагування: ${title.trim()}`);
      setSaved(true);
      setNewFiles([]);
      setNewPreviews([]);
      // reload existing media if files were uploaded
      if (newFiles.length > 0) {
        const refreshed = await postsApi.getById(postId);
        setExistingMedia(refreshed.mediaUrls ?? []);
      }
    } catch (e) {
      setError((e as Error).message ?? 'Помилка збереження');
    } finally {
      setSaving(false);
    }
  }

  async function handleDelete() {
    if (!postId) return;
    if (!window.confirm('Видалити цей пост? Цю дію не можна скасувати.')) return;
    setDeleting(true);
    try {
      await postsApi.delete(postId);
      navigate('/home');
    } catch (e) {
      setError((e as Error).message ?? 'Помилка видалення');
      setDeleting(false);
    }
  }

  return (
    <div className={styles.page}>
      <FrameComponent2 />

      <div className={styles.content}>
        <header className={styles.header}>
          <h2 className={styles.headerTitle}>{pageTitle}</h2>
          <button className={styles.cancelBtn} onClick={() => navigate(postId ? `/posts/${postId}` : '/home')}>
            Скасувати
          </button>
        </header>

        {loading && (
          <p style={{ color: '#aaa', padding: '32px 40px 0 164px' }}>Завантаження...</p>
        )}
        {!loading && (
          <div className={styles.body}>
            {/* Left column */}
            <div className={styles.leftCol}>

              <div className={styles.card}>
                <h3 className={styles.cardTitle}>Основне</h3>
                <div className={styles.field}>
                  <label className={styles.fieldLabel}>Назва</label>
                  <input
                    className={styles.input}
                    type="text"
                    placeholder="Введіть назву поста"
                    value={title}
                    onChange={e => setTitle(e.target.value)}
                  />
                </div>
                <div className={styles.field}>
                  <label className={styles.fieldLabel}>Опис</label>
                  <textarea
                    className={styles.textarea}
                    rows={4}
                    placeholder="Розкажіть про ваш проєкт..."
                    value={description}
                    onChange={e => setDescription(e.target.value)}
                  />
                </div>
              </div>

              <div className={styles.card}>
                <h3 className={styles.cardTitle}>Теги</h3>
                {allTags.length === 0
                  ? <p style={{ color: '#555', fontSize: 13, margin: 0 }}>Завантаження тегів...</p>
                  : (
                    <div className={styles.tagsWrap}>
                      {allTags.map(tag => (
                        <button
                          key={tag.tagId}
                          className={selectedTagIds.includes(tag.tagId) ? styles.tagBtnActive : styles.tagBtn}
                          onClick={() => toggleTag(tag.tagId)}
                          type="button"
                        >
                          {tagLabel(tag.name)}
                        </button>
                      ))}
                    </div>
                  )
                }
              </div>

              <div className={styles.card}>
                <h3 className={styles.cardTitle}>Зовнішнє посилання</h3>
                <input
                  className={styles.input}
                  type="text"
                  placeholder="Behance, Dribbble, Figma, SoundCloud, GitHub..."
                  value={externalLink}
                  onChange={e => setExternalLink(e.target.value)}
                />
                <p className={styles.hint}>Необов'язково</p>
              </div>

              {error && (
                <p style={{ color: '#ff6b6b', fontSize: 13, margin: 0 }}>{error}</p>
              )}
              {saved && (
                <p style={{ color: '#3fcca0', fontSize: 13, margin: 0 }}>Зміни збережено</p>
              )}

              <div className={styles.actions}>
                <button
                  className={styles.saveBtn}
                  onClick={handleSave}
                  disabled={saving || !title.trim()}
                >
                  {saving ? 'Збереження...' : 'Зберегти зміни'}
                </button>
                <button
                  className={styles.deleteBtn}
                  onClick={handleDelete}
                  disabled={deleting}
                >
                  {deleting ? 'Видалення...' : 'Видалити пост'}
                </button>
              </div>
            </div>

            {/* Right column — Media */}
            <div className={styles.rightCol}>
              <div className={styles.card}>
                <h3 className={styles.cardTitle}>Медіа</h3>

                {existingMedia.length > 0 && (
                  <>
                    <p className={styles.fieldLabel} style={{ marginBottom: 4 }}>Поточні файли</p>
                    <div className={styles.mediaGrid}>
                      {existingMedia.map((url, i) => {
                        const type = getMediaType(url);
                        if (type === 'pdf') return (
                          <div key={i} className={styles.mediaThumb} style={{ display: 'flex', alignItems: 'center', justifyContent: 'center', background: '#1a1a2e', fontSize: 28 }}>📄</div>
                        );
                        if (type === 'audio') return (
                          <div key={i} className={styles.mediaThumb} style={{ display: 'flex', alignItems: 'center', justifyContent: 'center', background: '#1a1a2e', fontSize: 28 }}>🎵</div>
                        );
                        if (type === 'video') return (
                          <video key={i} src={url} className={styles.mediaThumb} muted preload="metadata" style={{ objectFit: 'cover' }} />
                        );
                        return <img key={i} src={url} alt="" className={styles.mediaThumb} />;
                      })}
                    </div>
                  </>
                )}

                {newPreviews.length > 0 && (
                  <>
                    <p className={styles.fieldLabel} style={{ marginBottom: 4 }}>Нові файли (замінять існуючі)</p>
                    <div className={styles.mediaGrid}>
                      {newPreviews.map((url, i) => {
                        const f = newFiles[i];
                        const isImage = f?.type.startsWith('image/');
                        const isVideo = f?.type.startsWith('video/');
                        const isAudio = f?.type.startsWith('audio/');
                        const isPdf = f?.type === 'application/pdf';
                        return (
                          <div key={i} className={styles.mediaThumbWrap}>
                            {isImage ? (
                              <img src={url} alt="" className={styles.mediaThumb} />
                            ) : isVideo ? (
                              <video src={url} className={styles.mediaThumb} muted preload="metadata" style={{ objectFit: 'cover' }} />
                            ) : isPdf ? (
                              <div className={styles.mediaThumb} style={{ display: 'flex', alignItems: 'center', justifyContent: 'center', background: '#1a1a2e', fontSize: 28 }}>📄</div>
                            ) : isAudio ? (
                              <div className={styles.mediaThumb} style={{ display: 'flex', alignItems: 'center', justifyContent: 'center', background: '#1a1a2e', fontSize: 28 }}>🎵</div>
                            ) : (
                              <div className={styles.mediaThumb} style={{ display: 'flex', alignItems: 'center', justifyContent: 'center', background: '#1a1a2e', fontSize: 28 }}>📎</div>
                            )}
                            <button
                              className={styles.mediaRemoveBtn}
                              onClick={() => removeNewFile(i)}
                              type="button"
                            >×</button>
                          </div>
                        );
                      })}
                    </div>
                  </>
                )}

                <label className={styles.mediaUpload} style={{ cursor: 'pointer' }}>
                  <input
                    ref={fileInputRef}
                    type="file"
                    accept=".png,.jpg,.jpeg,.mp4,.mp3,.pdf"
                    multiple
                    style={{ display: 'none' }}
                    onChange={handleFileChange}
                  />
                  <p className={styles.mediaUploadText}>
                    Оберіть файли або перетягніть сюди<br />
                    <span className={styles.mediaUploadHint}>
                      {newFiles.length > 0
                        ? `Вибрано ${newFiles.length} файл(ів) — замінять поточні при збереженні`
                        : 'PNG, JPG, MP4, MP3, PDF · до 50 МБ'}
                    </span>
                  </p>
                </label>
              </div>
            </div>
          </div>
        )}
        <Moderation moderationPosition="relative" moderationTop="unset" moderationLeft="unset" />
      </div>
    </div>
  );
};

export default EditPostPage;