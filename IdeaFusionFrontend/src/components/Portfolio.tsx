import { useMemo, useState, type ReactNode } from "react";
import type { PostWithEngagementDto } from "../api/types";
import { EmptyState, Loading, Segmented, Tag, cx } from "../ui";
import { IconFeed, IconFilter } from "../ui/icons";
import PostTile from "./PostTile";
import TagFilterDialog from "./TagFilterDialog";
import { plural } from "../lib/plural";
import sh from "../pages/shared.module.css";
import s from "./Portfolio.module.css";

type Scope = "all" | "personal" | "team";

/**
 * A person's works, used on every profile page: split into personal and team works, and
 * filtered by tags through the same window as the feed.
 */
export default function Portfolio({
  posts,
  loading,
  empty,
  actions,
}: {
  posts: PostWithEngagementDto[];
  loading?: boolean;
  /** Shown when there are no works at all (not when a filter hides them). */
  empty: { title: string; text?: string; action?: ReactNode };
  /** Extra buttons on the right of the toolbar, e.g. "Новий пост". */
  actions?: ReactNode;
}) {
  const [scope, setScope] = useState<Scope>("all");
  const [tags, setTags] = useState<string[]>([]);
  const [tagsOpen, setTagsOpen] = useState(false);

  const personal = useMemo(() => posts.filter(p => !p.post.collaboration), [posts]);
  const team = useMemo(() => posts.filter(p => !!p.post.collaboration), [posts]);
  const inScope = scope === "personal" ? personal : scope === "team" ? team : posts;

  const shown = useMemo(() => {
    if (tags.length === 0) return inScope;
    const wanted = new Set(tags);
    return inScope.filter(p => p.post.tags.some(t => wanted.has(t.toLowerCase())));
  }, [inScope, tags]);

  const postTags = useMemo(() => inScope.map(p => p.post.tags), [inScope]);
  const tagNames = useMemo(() => {
    const map = new Map<string, string>();
    for (const list of postTags) for (const t of list) if (!map.has(t.toLowerCase())) map.set(t.toLowerCase(), t);
    return map;
  }, [postTags]);

  if (loading) return <Loading />;
  if (posts.length === 0) {
    return (
      <>
        {actions && <div className={s.toolbar}><span />{actions}</div>}
        <EmptyState icon={<IconFeed />} title={empty.title} text={empty.text} action={empty.action} />
      </>
    );
  }

  return (
    <>
      <div className={s.toolbar}>
        <Segmented
          label="Які роботи"
          value={scope}
          onChange={setScope}
          items={[
            { value: "all", label: `Усі · ${posts.length}` },
            { value: "personal", label: `Особисті · ${personal.length}` },
            { value: "team", label: `Командні · ${team.length}` },
          ]}
        />
        <div className={s.right}>
          <button
            type="button"
            className={cx(s.filterBtn, tags.length > 0 && s.filterBtnOn)}
            onClick={() => setTagsOpen(true)}
            aria-haspopup="dialog"
            aria-label={tags.length ? `Фільтр за тегами, вибрано ${tags.length}` : "Фільтр за тегами"}
            title="Фільтр за тегами"
          >
            <IconFilter size={16} />
            {tags.length > 0 && <span className={s.filterCount}>{tags.length}</span>}
          </button>
          {actions}
        </div>
      </div>

      {tags.length > 0 && (
        <div className={s.activeTags} aria-label="Вибрані теги">
          {tags.map(t => (
            <Tag key={t} active onRemove={() => setTags(prev => prev.filter(x => x !== t))}>
              #{tagNames.get(t) ?? t}
            </Tag>
          ))}
          <button type="button" className={s.clearTags} onClick={() => setTags([])}>
            скинути
          </button>
        </div>
      )}

      <TagFilterDialog open={tagsOpen} onClose={() => setTagsOpen(false)} selected={tags} postTags={postTags} onApply={setTags} />

      {shown.length === 0 ? (
        <EmptyState
          icon={<IconFeed />}
          title={
            tags.length > 0
              ? "Немає робіт із вибраними тегами"
              : scope === "personal"
                ? "Особистих робіт поки немає"
                : "Командних робіт поки немає"
          }
          text={
            tags.length > 0
              ? undefined
              : scope === "team"
                ? "Тут з'являються роботи, опубліковані від імені команд, у яких є ця людина."
                : undefined
          }
        />
      ) : (
        <>
          {scope === "all" && tags.length === 0 && team.length > 0 && personal.length > 0 && (
            <p className={s.hint}>
              {personal.length} {plural(personal.length, ["особиста робота", "особисті роботи", "особистих робіт"])} і {team.length}{" "}
              {plural(team.length, ["командна", "командні", "командних"])} — командні позначені назвою команди.
            </p>
          )}
          <div className={sh.tiles}>
            {shown.map(item => (
              <PostTile
                key={item.post.postId}
                postId={item.post.postId}
                title={item.post.title}
                mediaUrls={item.post.mediaUrls}
                likes={item.likesCount}
                comments={item.commentsCount}
                caption={item.post.collaboration?.name}
              />
            ))}
          </div>
        </>
      )}
    </>
  );
}
