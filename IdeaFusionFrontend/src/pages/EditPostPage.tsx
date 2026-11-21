import { useCallback, useEffect, useState, type FormEvent } from "react";
import { useNavigate, useSearchParams } from "react-router-dom";
import { postsApi, tagsApi } from "../api/posts";
import type { TagDto } from "../api/types";
import { useAuth } from "../context/AuthContext";
import { Page } from "../layout/AppShell";
import { usePageMeta } from "../layout/PageMeta";
import { Button, EmptyState, ErrorState, Field, Input, Loading, Notice, PageHeader, Textarea, useDialogs, useToast, cx } from "../ui";
import type { CopyCheckResult } from "../api/copycheck";
import CopyCheckNotice from "../components/CopyCheckNotice";
import { IconAlert, IconAudio, IconCheck, IconFile, IconVideo } from "../ui/icons";
import MediaPicker from "../components/MediaPicker";
import TagPicker from "../components/TagPicker";
import { mediaKind } from "../lib/media";
import sh from "./shared.module.css";
import s from "./PostForm.module.css";

export default function EditPostPage() {
  const navigate = useNavigate();
  const [searchParams] = useSearchParams();
  const { user } = useAuth();
  const { confirm } = useDialogs();
  const toast = useToast();
  const postId = searchParams.get("id") ? parseInt(searchParams.get("id")!, 10) : null;

  const [originalTitle, setOriginalTitle] = useState("");
  const [title, setTitle] = useState("");
  const [description, setDescription] = useState("");
  const [externalLink, setExternalLink] = useState("");
  const [allTags, setAllTags] = useState<TagDto[]>([]);
  const [selectedTagIds, setSelectedTagIds] = useState<number[]>([]);
  const [existingMedia, setExistingMedia] = useState<string[]>([]);
  const [newFiles, setNewFiles] = useState<File[]>([]);
  const [loading, setLoading] = useState(true);
  const [loadError, setLoadError] = useState<string | null>(null);
  const [saving, setSaving] = useState(false);
  const [deleting, setDeleting] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [titleError, setTitleError] = useState<string | null>(null);
  const [copyChecks, setCopyChecks] = useState<Map<File, CopyCheckResult | null>>(new Map());

  usePageMeta(originalTitle ? `Редагування: ${originalTitle}` : "Редагування посту", [
    { label: "Стрічка", to: "/home" },
    ...(postId && originalTitle ? [{ label: originalTitle, to: `/posts/${postId}` }] : []),
  ]);

  const load = useCallback(() => {
    if (!postId) {
      setLoading(false);
      return;
    }
    setLoading(true);
    setLoadError(null);
    Promise.all([postsApi.getById(postId), tagsApi.getAll(), tagsApi.getByPost(postId)])
      .then(([post, tagList, postTags]) => {
        setTitle(post.title);
        setOriginalTitle(post.title);
        setDescription(post.description ?? "");
        setExternalLink(post.externalLink ?? "");
        setExistingMedia(post.mediaUrls ?? []);
        setAllTags(tagList);
        setSelectedTagIds(postTags.map(t => t.tagId));
      })
      .catch(e => setLoadError((e as Error).message))
      .finally(() => setLoading(false));
  }, [postId]);

  useEffect(load, [load]);

  async function handleSave(e: FormEvent) {
    e.preventDefault();
    if (!postId || !user) return;
    if (!title.trim()) {
      setTitleError("Введіть назву");
      return;
    }
    setSaving(true);
    setError(null);
    try {
      await postsApi.update(postId, {
        title: title.trim(),
        description: description.trim() || undefined,
        externalLink: externalLink.trim() || undefined,
        tagIds: selectedTagIds,
        newMediaFiles: newFiles.length > 0 ? newFiles : undefined,
      });
      setOriginalTitle(title.trim());
      const uploaded = newFiles.length > 0;
      setNewFiles([]);
      if (uploaded) {
        const refreshed = await postsApi.getById(postId);
        setExistingMedia(refreshed.mediaUrls ?? []);
      }
      toast.success("Зміни збережено");
    } catch (err) {
      setError((err as Error).message ?? "Помилка збереження");
    } finally {
      setSaving(false);
    }
  }

  async function handleDelete() {
    if (!postId) return;
    const ok = await confirm({
      title: "Видалити пост?",
      message: "Цю дію не можна скасувати.",
      confirmLabel: "Видалити",
      danger: true,
    });
    if (!ok) return;
    setDeleting(true);
    try {
      await postsApi.delete(postId);
      toast.success("Пост видалено");
      navigate("/home");
    } catch (err) {
      setError((err as Error).message ?? "Помилка видалення");
      setDeleting(false);
    }
  }

  if (!postId) {
    return (
      <Page width="narrow">
        <EmptyState title="Пост не вказано" text="Відкрийте редагування зі сторінки посту." />
      </Page>
    );
  }
  if (loading) {
    return (
      <Page width="narrow">
        <Loading />
      </Page>
    );
  }
  if (loadError) {
    return (
      <Page width="narrow">
        <ErrorState title="Не вдалося відкрити пост" message={loadError} onRetry={load} />
      </Page>
    );
  }

  const images = newFiles.filter(f => f.type.startsWith("image/"));

  function badge(f: File) {
    const r = copyChecks.get(f);
    if (!f.type.startsWith("image/") || !r) return null;
    if (r.zone === "green" || !r.best) {
      return (
        <span className={cx(s.badge, s.badgeOk)}>
          <IconCheck size={12} /> оригінальне
        </span>
      );
    }
    return (
      <span className={cx(s.badge, r.zone === "red" ? s.badgeRed : s.badgeWarn)}>
        <IconAlert size={12} /> схоже · {Math.round(r.best.p_copy * 100)}%
      </span>
    );
  }

  return (
    <Page width="wide">
      <PageHeader title="Редагування посту" description={originalTitle} />

      <form className={s.compose} onSubmit={e => void handleSave(e)} noValidate>
        <div className={s.formCol}>
          <section className={s.block}>
            <span className={s.sectionLabel}>Медіа</span>
            {existingMedia.length > 0 && (
              <div className={s.existing}>
                {existingMedia.map(url => {
                  const kind = mediaKind(url);
                  return (
                    <a key={url} href={url} target="_blank" rel="noreferrer" className={s.existingItem} title="Відкрити файл">
                      {kind === "image" ? <img src={url} alt="" loading="lazy" /> : kind === "video" ? <IconVideo /> : kind === "audio" ? <IconAudio /> : <IconFile />}
                    </a>
                  );
                })}
              </div>
            )}
            <MediaPicker
              files={newFiles}
              onAdd={files => setNewFiles(prev => [...prev, ...files])}
              onRemove={i => setNewFiles(prev => prev.filter((_, idx) => idx !== i))}
              accept=".png,.jpg,.jpeg,.mp4,.mp3,.pdf"
              hint="png · jpg · mp4 · mp3 · pdf — нові файли додаються до вже завантажених"
              label="Додати файли"
              renderBadge={badge}
            />
          </section>

          <section className={s.block}>
            <Field label="Назва" required htmlFor="edit-title" error={titleError}>
              <Input
                id="edit-title"
                value={title}
                invalid={!!titleError}
                onChange={e => {
                  setTitle(e.target.value);
                  setTitleError(null);
                }}
              />
            </Field>
            <Field label="Опис" htmlFor="edit-desc">
              <Textarea id="edit-desc" rows={5} placeholder="Розкажіть про ваш проєкт…" value={description} onChange={e => setDescription(e.target.value)} />
            </Field>
          </section>

          <section className={s.block}>
            <span className={s.sectionLabel}>Теги</span>
            <TagPicker tags={allTags} selected={selectedTagIds} onChange={setSelectedTagIds} />
          </section>

          <section className={s.block}>
            <Field label="Зовнішнє посилання" htmlFor="edit-link" hint="Behance, Dribbble, Figma, SoundCloud, GitHub тощо">
              <Input id="edit-link" placeholder="https://" value={externalLink} onChange={e => setExternalLink(e.target.value)} />
            </Field>
          </section>
        </div>

        <aside className={s.aside}>
          {images.length > 0 && (
            <section className={s.checkPanel} aria-labelledby="check-title">
              <h2 id="check-title" className={s.checkTitle}>
                Перевірка нових зображень
              </h2>
              <ul className={s.checkList}>
                {images.map(f => (
                  <CopyCheckNotice key={`${f.name}-${f.size}-${f.lastModified}`} file={f} onResult={r => setCopyChecks(prev => new Map(prev).set(f, r))} />
                ))}
              </ul>
            </section>
          )}

          {error && <Notice tone="error">{error}</Notice>}

          <div className={s.actions}>
            <Button type="submit" variant="primary" size="lg" block loading={saving}>
              Зберегти зміни
            </Button>
            <Button variant="ghost" block onClick={() => navigate(`/posts/${postId}`)}>
              Скасувати
            </Button>
          </div>

          <div className={s.danger}>
            <div className={s.dangerText}>
              <strong>Видалити пост</strong>
              <span className={sh.faint}>Пост, медіа та коментарі буде видалено без можливості відновлення.</span>
            </div>
            <Button variant="danger" loading={deleting} onClick={() => void handleDelete()}>
              Видалити
            </Button>
          </div>
        </aside>
      </form>
    </Page>
  );
}
