import { useEffect, useState, type FormEvent } from "react";
import { useNavigate } from "react-router-dom";
import { teamsApi } from "../api/teams";
import { tagsApi } from "../api/posts";
import type { PostDto, TeamEntity, TagDto } from "../api/types";
import { api, upload } from "../api/client";
import { hasPermission } from "../api/types";
import type { CopyCheckResult } from "../api/copycheck";
import { useAuth } from "../context/AuthContext";
import { Page } from "../layout/AppShell";
import { usePageMeta } from "../layout/PageMeta";
import { Avatar, Button, Field, Input, Notice, PageHeader, Select, Textarea, useDialogs, useToast, cx } from "../ui";
import { IconAlert, IconCheck } from "../ui/icons";
import CopyCheckNotice from "../components/CopyCheckNotice";
import MediaPicker from "../components/MediaPicker";
import TagPicker from "../components/TagPicker";
import s from "./PostForm.module.css";

export default function NewPostPage() {
  usePageMeta("Новий пост", [{ label: "Стрічка", to: "/home" }]);
  const navigate = useNavigate();
  const { user } = useAuth();
  const { confirm } = useDialogs();
  const toast = useToast();

  const [publisher, setPublisher] = useState<"self" | "team">("self");
  const [teamId, setTeamId] = useState("");
  const [myTeams, setMyTeams] = useState<TeamEntity[]>([]);
  const [teamsLoading, setTeamsLoading] = useState(true);
  const [title, setTitle] = useState("");
  const [description, setDescription] = useState("");
  const [link, setLink] = useState("");
  const [mediaFiles, setMediaFiles] = useState<File[]>([]);
  const [allTags, setAllTags] = useState<TagDto[]>([]);
  const [tagsLoading, setTagsLoading] = useState(true);
  const [selectedTagIds, setSelectedTagIds] = useState<number[]>([]);
  const [submitting, setSubmitting] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [titleError, setTitleError] = useState<string | null>(null);
  // Copy check of each selected image, keyed by the File so removing one keeps the others.
  const [copyChecks, setCopyChecks] = useState<Map<File, CopyCheckResult | null>>(new Map());

  const selectedTeam = myTeams.find(t => t.id === teamId) ?? null;

  useEffect(() => {
    if (!user) return;
    setTeamsLoading(true);
    teamsApi
      .getMine()
      .then(teams => setMyTeams(teams.filter(t => hasPermission(t, user.id, "canPublishPosts"))))
      .catch(() => setMyTeams([]))
      .finally(() => setTeamsLoading(false));
  }, [user]);

  useEffect(() => {
    tagsApi
      .getAll()
      .then(setAllTags)
      .catch(() => setAllTags([]))
      .finally(() => setTagsLoading(false));
  }, []);

  async function handleSubmit(e: FormEvent) {
    e.preventDefault();
    if (!title.trim()) {
      setTitleError("Введіть назву");
      return;
    }
    if (publisher === "team" && !selectedTeam) {
      setError("Оберіть команду");
      return;
    }
    const suspicious = mediaFiles.filter(f => copyChecks.get(f)?.zone === "red");
    if (suspicious.length > 0) {
      const ok = await confirm({
        title: "Опублікувати попри збіги?",
        message: `Зображення (${suspicious.map(f => f.name).join(", ")}) дуже схожі на вже опубліковані роботи. Пост побачить модератор; якщо це ваша робота або використана з дозволу — все гаразд.`,
        confirmLabel: "Опублікувати",
      });
      if (!ok) return;
    }

    setSubmitting(true);
    setError(null);
    try {
      // When posting as a team, ensure a CollaborationSnapshot exists in Content API
      let collaborationSnapshotId: number | null = null;
      if (publisher === "team" && selectedTeam) {
        try {
          const snapData = await api.post<{ collaborationSnapshotId: number }>(`/api/collaborationSnapshot/ensure`, {
            teamName: selectedTeam.name,
            mongoTeamId: selectedTeam.id,
            avatarUrl: selectedTeam.avatarUrl ?? null,
          });
          collaborationSnapshotId = snapData.collaborationSnapshotId ?? null;
        } catch {
          // Not fatal on its own: the post still saves, just without the snapshot.
          collaborationSnapshotId = null;
        }
      }

      const formData = new FormData();
      formData.append("Title", title.trim());
      if (description) formData.append("Description", description);
      if (link) formData.append("ExternalLink", link);
      // DB constraint: a post is owned by either the author or the collaboration,
      // never both. A personal post needs no author field — the backend takes the
      // author from the token, so omitting it is what selects the personal case.
      if (collaborationSnapshotId != null) {
        formData.append("CollaborationSnapshotId", String(collaborationSnapshotId));
      }
      if (user?.avatarUrl) formData.append("AuthorAvatarUrl", user.avatarUrl);
      formData.append("Status", "Published");
      mediaFiles.forEach(f => formData.append("MediaFiles", f));
      selectedTagIds.forEach(id => formData.append("TagIds", String(id)));

      const created = await upload<PostDto>(`/api/post`, formData);

      if (publisher === "team" && selectedTeam) {
        await api.post(`/api/team-posts`, {
          postId: String(created.postId),
          teamId: selectedTeam.id,
          title: title.trim(),
        });
      }
      toast.success("Пост опубліковано");
      navigate(created?.postId ? `/posts/${created.postId}` : "/home");
    } catch (ex) {
      setError(ex instanceof Error ? ex.message : "Помилка збереження");
    } finally {
      setSubmitting(false);
    }
  }

  const ownName = user?.fullName || user?.userName || user?.email?.split("@")[0] || "Ви";

  const images = mediaFiles.filter(f => f.type.startsWith("image/"));
  const checking = images.some(f => !copyChecks.has(f));

  function badge(f: File) {
    if (!f.type.startsWith("image/") || !copyChecks.has(f)) return null;
    const r = copyChecks.get(f);
    if (!r) return null;
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
      <PageHeader title="Новий пост" description="Поділіться роботою від свого імені або від команди." />

      <form className={s.compose} onSubmit={e => void handleSubmit(e)} noValidate>
        <div className={s.formCol}>
          <section className={s.block}>
            <MediaPicker
              files={mediaFiles}
              onAdd={files => setMediaFiles(prev => [...prev, ...files])}
              onRemove={i => setMediaFiles(prev => prev.filter((_, idx) => idx !== i))}
              accept="image/*,video/*,audio/*,.pdf"
              hint="png · jpg · mp4 · mp3 · pdf — до 50 МБ"
              renderBadge={badge}
            />
          </section>

          <section className={s.block}>
            <Field label="Назва" required htmlFor="post-title" error={titleError}>
              <Input
                id="post-title"
                placeholder="«Кадри, що дихають»"
                value={title}
                invalid={!!titleError}
                onChange={e => {
                  setTitle(e.target.value);
                  setTitleError(null);
                }}
                autoFocus
              />
            </Field>
            <Field label="Опис" htmlFor="post-desc">
              <Textarea id="post-desc" rows={5} placeholder="Про що робота, як вона створювалась, які інструменти…" value={description} onChange={e => setDescription(e.target.value)} />
            </Field>
          </section>

          <section className={s.block}>
            <span className={s.sectionLabel}>Від кого публікувати</span>
            <div className={s.publisher} role="radiogroup" aria-label="Від кого публікувати">
              <button type="button" role="radio" aria-checked={publisher === "self"} className={cx(s.option, publisher === "self" && s.optionOn)} onClick={() => setPublisher("self")}>
                <Avatar name={ownName} src={user?.avatarUrl} seed={user?.id} size={36} />
                <span className={s.optionText}>
                  <span className={s.optionTitle}>Від свого імені</span>
                  <span className={s.optionSub}>{user?.email}</span>
                </span>
              </button>
              <button
                type="button"
                role="radio"
                aria-checked={publisher === "team"}
                className={cx(s.option, publisher === "team" && s.optionOn)}
                onClick={() => setPublisher("team")}
                disabled={!teamsLoading && myTeams.length === 0}
              >
                <Avatar name={selectedTeam?.name ?? "Команда"} src={selectedTeam?.avatarUrl} seed={selectedTeam?.id} size={36} square />
                <span className={s.optionText}>
                  <span className={s.optionTitle}>Від імені команди</span>
                  <span className={s.optionSub}>
                    {teamsLoading ? "завантаження…" : myTeams.length === 0 ? "немає команд з правом публікації" : selectedTeam?.name ?? "оберіть команду"}
                  </span>
                </span>
              </button>
            </div>
            {publisher === "team" && (
              <Field label="Команда" htmlFor="post-team" required>
                <Select id="post-team" value={teamId} onChange={e => setTeamId(e.target.value)}>
                  <option value="">Оберіть команду…</option>
                  {myTeams.map(t => (
                    <option key={t.id} value={t.id}>
                      {t.name}
                    </option>
                  ))}
                </Select>
              </Field>
            )}
          </section>

          <section className={s.block}>
            <span className={s.sectionLabel}>Теги</span>
            <TagPicker tags={allTags} selected={selectedTagIds} onChange={setSelectedTagIds} loading={tagsLoading} />
          </section>

          <section className={s.block}>
            <Field label="Зовнішнє посилання" htmlFor="post-link" hint="Behance, Dribbble, Figma, SoundCloud, GitHub тощо">
              <Input id="post-link" placeholder="behance.net/kadry-project" value={link} onChange={e => setLink(e.target.value)} />
            </Field>
          </section>
        </div>

        <aside className={s.aside}>
          <section className={s.checkPanel} aria-labelledby="check-title">
            <h2 id="check-title" className={s.checkTitle}>
              Перевірка зображень
            </h2>
            <p className={s.checkNote}>Кожне зображення порівнюється з уже опублікованими роботами ще до публікації.</p>
            {images.length === 0 ? (
              <p className={s.checkEmpty}>Додайте зображення — результат з'явиться тут.</p>
            ) : (
              <ul className={s.checkList}>
                {images.map(f => (
                  <CopyCheckNotice key={`${f.name}-${f.size}-${f.lastModified}`} file={f} onResult={r => setCopyChecks(prev => new Map(prev).set(f, r))} />
                ))}
              </ul>
            )}
          </section>

          {error && <Notice tone="error">{error}</Notice>}

          <div className={s.actions}>
            <Button type="submit" variant="primary" size="lg" block loading={submitting}>
              {checking ? "Опублікувати (перевірка триває)" : "Опублікувати"}
            </Button>
            <Button variant="ghost" block onClick={() => navigate(-1)}>
              Скасувати
            </Button>
          </div>
        </aside>
      </form>
    </Page>
  );
}
