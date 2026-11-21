import { useCallback, useEffect, useMemo, useState } from "react";
import { Link } from "react-router-dom";
import { copyCheckApi, type CopyCheckRecord, type CopyZone } from "../api/copycheck";
import { postsApi } from "../api/posts";
import type { PostDto } from "../api/types";
import { CheckItem } from "../components/PostCopyReview";
import { Page } from "../layout/AppShell";
import { usePageMeta } from "../layout/PageMeta";
import { Button, EmptyState, ErrorState, Loading, Notice, PageHeader, Segmented, useDialogs, useToast } from "../ui";
import { IconCopy, IconRefresh, IconTrash } from "../ui/icons";
import { invalidateCopyFlags } from "../lib/copyFlags";
import { plural } from "../lib/plural";
import s from "./SuspiciousPostsPage.module.css";

type StatusFilter = "open" | "confirmed_copy" | "dismissed";
type ZoneFilter = "all" | CopyZone;

/** Post details by id, fetched once; null when the post is gone. */
const postCache = new Map<number, Promise<PostDto | null>>();
function fetchPost(id: number): Promise<PostDto | null> {
  let p = postCache.get(id);
  if (!p) {
    p = postsApi.getById(id).catch(() => null);
    postCache.set(id, p);
  }
  return p;
}

function PostHeader({
  postId,
  post,
  checks,
  onDelete,
  deleting,
}: {
  postId: number;
  /** undefined while loading, null when the post no longer exists. */
  post: PostDto | null | undefined;
  checks: CopyCheckRecord[];
  onDelete?: () => void;
  deleting?: boolean;
}) {
  const worst = checks.reduce((m, c) => Math.max(m, c.score), 0);
  const red = checks.some(c => c.zone === "red");
  const author = post?.collaboration?.name ?? post?.author?.userName;
  const cover = post?.mediaUrls[0];

  return (
    <header className={s.postHead}>
      <Link to={`/posts/${postId}`} className={s.thumb} aria-label={`Пост #${postId}`}>
        {cover ? <img src={cover} alt="" loading="lazy" /> : <IconCopy size={22} />}
      </Link>
      <div className={s.postText}>
        <Link to={`/posts/${postId}`} className={s.postTitle}>
          {post === undefined ? `Пост #${postId}` : post ? post.title : `Пост #${postId} (видалено)`}
        </Link>
        <span className={s.postMeta}>
          {author ? `${author} · ` : ""}
          {checks.length} {plural(checks.length, ["збіг", "збіги", "збігів"])}
        </span>
      </div>
      <span className={red ? s.scoreRed : s.scoreAmber} title="Найвища ймовірність копії серед збігів">
        {Math.round(worst * 100)}%
      </span>
      {onDelete && post && (
        <Button size="sm" variant="danger" icon={<IconTrash size={15} />} loading={deleting} onClick={onDelete}>
          Видалити пост
        </Button>
      )}
    </header>
  );
}

/**
 * Moderators' queue of posts whose images look like copies of other works. The model only
 * points at matches; every decision ("це копія" / "не копія") is the moderator's and lands
 * in the moderation log. Confirming a copy emails the author; confirmed copies are then
 * deleted from the "Підтверджені копії" view, one by one or all at once.
 */
export default function SuspiciousPostsPage() {
  usePageMeta("Підозрілі пости", [{ label: "Модерація" }]);
  const { confirm } = useDialogs();
  const toast = useToast();
  const [status, setStatus] = useState<StatusFilter>("open");
  const [zone, setZone] = useState<ZoneFilter>("all");
  const [checks, setChecks] = useState<CopyCheckRecord[] | null>(null);
  const [posts, setPosts] = useState<Record<number, PostDto | null>>({});
  const [error, setError] = useState<string | null>(null);
  const [deleting, setDeleting] = useState<Set<number>>(new Set());
  const [bulkProgress, setBulkProgress] = useState<string | null>(null);

  const load = useCallback(() => {
    setError(null);
    setChecks(null);
    copyCheckApi
      .queue({ status, zones: zone === "all" ? ["red", "yellow"] : [zone], limit: 300 })
      .then(setChecks)
      .catch((e: Error) => setError(e.message || "Не вдалося завантажити"));
  }, [status, zone]);

  useEffect(load, [load]);

  // Post details for every card: needed for titles, and to know which posts are already gone.
  useEffect(() => {
    if (!checks) return;
    let cancelled = false;
    const ids = [...new Set(checks.map(c => c.post_id).filter((x): x is number => x != null))];
    void Promise.all(ids.map(id => fetchPost(id).then(p => [id, p] as const))).then(pairs => {
      if (!cancelled) setPosts(prev => ({ ...prev, ...Object.fromEntries(pairs) }));
    });
    return () => {
      cancelled = true;
    };
  }, [checks]);

  /** One card per post, the most suspicious first. Deleted posts leave the confirmed list. */
  const groups = useMemo(() => {
    const byPost = new Map<number, CopyCheckRecord[]>();
    for (const c of checks ?? []) {
      if (c.post_id == null) continue;
      byPost.set(c.post_id, [...(byPost.get(c.post_id) ?? []), c]);
    }
    return [...byPost.entries()]
      .map(([postId, list]) => ({ postId, list: list.sort((a, b) => b.score - a.score) }))
      .filter(g => status !== "confirmed_copy" || posts[g.postId] !== null)
      .sort((a, b) => b.list[0].score - a.list[0].score);
  }, [checks, posts, status]);

  const deletable = status === "confirmed_copy" ? groups.filter(g => posts[g.postId]) : [];

  async function removePost(postId: number) {
    setDeleting(prev => new Set(prev).add(postId));
    try {
      await postsApi.moderateDelete(postId);
      postCache.set(postId, Promise.resolve(null));
      setPosts(prev => ({ ...prev, [postId]: null }));
      invalidateCopyFlags();
      return true;
    } catch (e: unknown) {
      toast.fromError(e, `Не вдалося видалити пост #${postId}`);
      return false;
    } finally {
      setDeleting(prev => {
        const next = new Set(prev);
        next.delete(postId);
        return next;
      });
    }
  }

  async function deleteOne(postId: number) {
    const title = posts[postId]?.title ?? `#${postId}`;
    const ok = await confirm({
      title: `Видалити пост «${title}»?`,
      message: "Пост визнано копією, автора вже повідомлено листом. Видалення не можна скасувати; воно потрапить у журнал модерації.",
      confirmLabel: "Видалити",
      danger: true,
    });
    if (ok && (await removePost(postId))) toast.success("Пост видалено");
  }

  async function deleteAll() {
    const targets = deletable.map(g => g.postId);
    if (targets.length === 0) return;
    const ok = await confirm({
      title: `Видалити ${targets.length} ${plural(targets.length, ["пост", "пости", "постів"])}?`,
      message: "Будуть видалені всі пости, визнані копіями. Цю дію не можна скасувати; кожне видалення потрапить у журнал модерації.",
      confirmLabel: "Видалити всі",
      danger: true,
    });
    if (!ok) return;
    let done = 0;
    for (const id of targets) {
      setBulkProgress(`Видалення ${done + 1} з ${targets.length}…`);
      if (await removePost(id)) done++;
    }
    setBulkProgress(null);
    toast.success(`Видалено ${done} ${plural(done, ["пост", "пости", "постів"])}`);
  }

  return (
    <Page>
      <PageHeader
        title="Підозрілі пости"
        description="Роботи, зображення яких схожі на вже опубліковані. Модель лише показує збіги — рішення ухвалює модератор, і воно потрапляє в журнал модерації. Коли пост визнано копією, автор отримує лист."
        actions={
          <>
            {status === "confirmed_copy" && deletable.length > 0 && (
              <Button variant="danger" icon={<IconTrash size={15} />} loading={!!bulkProgress} onClick={() => void deleteAll()}>
                Видалити всі ({deletable.length})
              </Button>
            )}
            <Button icon={<IconRefresh size={15} />} onClick={load}>
              Оновити
            </Button>
          </>
        }
      />

      <div className={s.filters}>
        <Segmented
          label="Статус"
          value={status}
          onChange={setStatus}
          items={[
            { value: "open", label: "Очікують рішення" },
            { value: "confirmed_copy", label: "Підтверджені копії" },
            { value: "dismissed", label: "Не копії" },
          ]}
        />
        <Segmented
          label="Ймовірність"
          value={zone}
          onChange={setZone}
          items={[
            { value: "all", label: "Усі" },
            { value: "red", label: "Ймовірні копії" },
            { value: "yellow", label: "Варто перевірити" },
          ]}
        />
        {checks && (
          <span className={s.count}>
            {groups.length} {plural(groups.length, ["пост", "пости", "постів"])}
          </span>
        )}
      </div>

      {bulkProgress && <Notice tone="info">{bulkProgress}</Notice>}

      {error ? (
        <ErrorState title="Не вдалося завантажити перевірки" message={error} onRetry={load} />
      ) : !checks ? (
        <Loading />
      ) : groups.length === 0 ? (
        <EmptyState
          icon={<IconCopy />}
          title={status === "open" ? "Підозрілих постів немає" : status === "confirmed_copy" ? "Підтверджених копій немає" : "Тут поки порожньо"}
          text={
            status === "open"
              ? "Нові збіги з'являтимуться тут одразу після публікації."
              : status === "confirmed_copy"
                ? "Пости, визнані копіями, з'являються тут, доки їх не видалено."
                : undefined
          }
        />
      ) : (
        <div className={s.list}>
          {groups.map(g => (
            <section key={g.postId} className={s.card}>
              <PostHeader
                postId={g.postId}
                post={posts[g.postId]}
                checks={g.list}
                onDelete={status === "confirmed_copy" ? () => void deleteOne(g.postId) : undefined}
                deleting={deleting.has(g.postId)}
              />
              <ul className={s.checks}>
                {g.list.map(c => (
                  <CheckItem key={c.id} check={c} onDecided={load} />
                ))}
              </ul>
            </section>
          ))}
        </div>
      )}
    </Page>
  );
}
