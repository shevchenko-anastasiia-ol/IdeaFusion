import { useCallback, useEffect, useMemo, useState } from "react";
import { Link } from "react-router-dom";
import { aggregatorApi } from "../api/aggregator";
import { vacanciesApi } from "../api/vacancies";
import type { PostWithEngagementDto, TeamVacancyEntity } from "../api/types";
import { useAuth } from "../context/AuthContext";
import { Page } from "../layout/AppShell";
import { usePageMeta } from "../layout/PageMeta";
import { Avatar, Button, ButtonLink, EmptyState, ErrorState, Segmented, SectionTitle, Skeleton, Tag, cx } from "../ui";
import { IconFeed, IconFilter } from "../ui/icons";
import PostCard from "../components/PostCard";
import TagFilterDialog from "../components/TagFilterDialog";
import AuthGate from "../components/AuthGate";
import { plural } from "../lib/plural";
import sh from "./shared.module.css";
import s from "./FeedPage.module.css";

const PAGE_SIZE = 10;
/** The aggregator returns the feed in one batch; filtering by tag happens here. */
const FEED_BATCH = 1000;

function FeedSkeleton() {
  return (
    <div className={s.posts} aria-hidden="true">
      {[0, 1].map(i => (
        <div key={i} className={s.skeletonCard}>
          <div className={sh.inline}>
            <Skeleton width={32} height={32} radius={16} />
            <Skeleton width={140} />
          </div>
          <Skeleton height={360} radius={12} />
          <Skeleton width="60%" height={18} />
          <Skeleton width="90%" />
        </div>
      ))}
    </div>
  );
}

export default function FeedPage() {
  usePageMeta("Стрічка");
  const { contentUserId, isAuthenticated, loading: authLoading } = useAuth();
  /** Tags picked in the filter window (lower-cased); a post needs at least one of them. */
  const [tagFilter, setTagFilter] = useState<string[]>([]);
  const [tagsOpen, setTagsOpen] = useState(false);
  const [sortBy, setSortBy] = useState<"time" | "popular">("time");
  const [posts, setPosts] = useState<PostWithEngagementDto[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [visible, setVisible] = useState(PAGE_SIZE);
  const [vacancies, setVacancies] = useState<TeamVacancyEntity[]>([]);
  const [vacancyTotal, setVacancyTotal] = useState(0);

  useEffect(() => {
    vacanciesApi
      .getPaged({ openOnly: true, pageSize: 4 })
      .then(page => {
        setVacancies(page.items);
        setVacancyTotal(page.totalCount);
      })
      .catch(() => setVacancies([]));
  }, []);

  const load = useCallback(() => {
    setLoading(true);
    setError(null);
    setVisible(PAGE_SIZE);
    // The feed needs an account: a guest sees the skeleton behind the sign-in card instead
    // of a failed request.
    if (authLoading || !isAuthenticated) return;
    aggregatorApi
      .getFeed(contentUserId ?? undefined, 0, FEED_BATCH, sortBy)
      .then(setPosts)
      .catch((e: Error) => setError(e.message))
      .finally(() => setLoading(false));
  }, [contentUserId, sortBy, authLoading, isAuthenticated]);

  useEffect(load, [load]);

  const filtered = useMemo(() => {
    if (tagFilter.length === 0) return posts;
    const wanted = new Set(tagFilter);
    return posts.filter(p => p.post.tags.some(t => wanted.has(t.toLowerCase())));
  }, [posts, tagFilter]);

  const postTags = useMemo(() => posts.map(p => p.post.tags), [posts]);
  /** Display names for the picked tags, as they are spelled on the posts. */
  const tagNames = useMemo(() => {
    const map = new Map<string, string>();
    for (const tags of postTags) for (const t of tags) if (!map.has(t.toLowerCase())) map.set(t.toLowerCase(), t);
    return map;
  }, [postTags]);


  /** Authors that publish most in the loaded feed. */
  const activeAuthors = useMemo(() => {
    const map = new Map<string, { key: string; name: string; avatar?: string; link: string | null; count: number; team: boolean }>();
    for (const { post } of posts) {
      const team = !!post.collaboration;
      const key = team ? `t:${post.collaboration!.collaborationId}` : `u:${post.author?.postAuthorId ?? post.createdBy}`;
      const name = post.collaboration?.name ?? post.author?.userName;
      if (!name) continue;
      const link = team ? (post.collaboration!.externalId ? `/teams/${post.collaboration!.externalId}` : null) : post.author?.externalId ? `/users/${post.author.externalId}` : null;
      const entry = map.get(key) ?? { key, name, avatar: post.collaboration?.avatarUrl ?? post.author?.avatarUrl, link, count: 0, team };
      entry.count++;
      map.set(key, entry);
    }
    return [...map.values()].sort((a, b) => b.count - a.count).slice(0, 5);
  }, [posts]);

  const shown = filtered.slice(0, visible);

  return (
    <Page width="feed">
      {!authLoading && !isAuthenticated && <AuthGate title="Увійдіть, щоб побачити стрічку" />}
      <div className={s.layout}>
        <div className={s.column}>
          <header className={s.header}>
            <div className={s.titleRow}>
              <h1 className={s.title}>Стрічка</h1>
              {!loading && !error && (
                <span className={sh.mono}>
                  {filtered.length} {plural(filtered.length, ["робота", "роботи", "робіт"])}
                </span>
              )}
            </div>
            <div className={s.headerControls}>
              <button
                type="button"
                className={cx(s.filterBtn, tagFilter.length > 0 && s.filterBtnOn)}
                onClick={() => setTagsOpen(true)}
                aria-haspopup="dialog"
                aria-label={tagFilter.length ? `Фільтр за тегами, вибрано ${tagFilter.length}` : "Фільтр за тегами"}
                title="Фільтр за тегами"
              >
                <IconFilter size={16} />
                {tagFilter.length > 0 && <span className={s.filterCount}>{tagFilter.length}</span>}
              </button>
              <Segmented
                label="Сортування"
                value={sortBy}
                onChange={setSortBy}
                items={[
                  { value: "time", label: "Нові" },
                  { value: "popular", label: "Популярні" },
                ]}
              />
            </div>
          </header>


          {tagFilter.length > 0 && (
            <div className={s.activeTags} aria-label="Вибрані теги">
              {tagFilter.map(t => (
                <Tag
                  key={t}
                  active
                  onRemove={() => {
                    setTagFilter(prev => prev.filter(x => x !== t));
                    setVisible(PAGE_SIZE);
                  }}
                >
                  #{tagNames.get(t) ?? t}
                </Tag>
              ))}
              <button type="button" className={s.clearTags} onClick={() => { setTagFilter([]); setVisible(PAGE_SIZE); }}>
                скинути
              </button>
            </div>
          )}

          {/* Separates the title, sorting and filter from the works themselves. */}
          <div className={s.divider} role="separator" aria-hidden="true" />

          <TagFilterDialog
            open={tagsOpen}
            onClose={() => setTagsOpen(false)}
            selected={tagFilter}
            postTags={postTags}
            onApply={tags => {
              setTagFilter(tags);
              setVisible(PAGE_SIZE);
            }}
          />

          {loading && <FeedSkeleton />}
          {!loading && error && <ErrorState message={error} onRetry={load} />}
          {!loading && !error && filtered.length === 0 && (
            <EmptyState
              icon={<IconFeed />}
              title={tagFilter.length > 0 ? "Немає робіт із вибраними тегами" : "Постів ще немає"}
              text={tagFilter.length > 0 ? "Спробуйте інші теги або подивіться всі пости." : "Станьте першим, хто поділиться роботою."}
              action={
                tagFilter.length > 0 ? (
                  <Button onClick={() => setTagFilter([])}>
                    Показати всі
                  </Button>
                ) : (
                  isAuthenticated && (
                    <ButtonLink to="/posts/new" variant="primary">
                      Створити пост
                    </ButtonLink>
                  )
                )
              }
            />
          )}
          {!loading && !error && shown.length > 0 && (
            <div className={s.posts}>
              {shown.map(item => (
                <PostCard key={item.post.postId} item={item} onDelete={id => setPosts(prev => prev.filter(p => p.post.postId !== id))} />
              ))}
              {visible < filtered.length && (
                <div className={s.more}>
                  <Button onClick={() => setVisible(v => v + PAGE_SIZE)}>Показати ще</Button>
                  <span className={sh.mono}>
                    {shown.length} / {filtered.length}
                  </span>
                </div>
              )}
            </div>
          )}
        </div>

        <aside className={s.rail}>
          {vacancies.length > 0 && (
            <section className={s.railBlock}>
              <div className={s.railHead}>
                <SectionTitle>Вакансії</SectionTitle>
                <Link to="/vacancies" className={s.railLink}>
                  усі {vacancyTotal} →
                </Link>
              </div>
              <ul className={s.vacancies}>
                {vacancies.map(v => (
                  <li key={v.id}>
                    <Link to={`/vacancies/${v.id}`} className={s.vacancy}>
                      <span className={s.vacancyRole}>{v.role}</span>
                      <span className={s.vacancyTeam}>
                        {v.teamName}
                        {v.category ? ` · ${v.category}` : ""}
                      </span>
                    </Link>
                  </li>
                ))}
              </ul>
            </section>
          )}
          {activeAuthors.length > 0 && (
            <section className={s.railBlock}>
              <SectionTitle>Найактивніші</SectionTitle>
              <ul className={s.authors}>
                {activeAuthors.map(a => {
                  const inner = (
                    <>
                      <Avatar name={a.name} src={a.avatar} seed={a.key} size={38} square={a.team} />
                      <span className={s.authorText}>
                        <span className={s.authorName}>{a.name}</span>
                        <span className={s.authorSub}>
                          {a.count} {plural(a.count, ["пост", "пости", "постів"])}
                          {a.team ? " · команда" : ""}
                        </span>
                      </span>
                    </>
                  );
                  return <li key={a.key}>{a.link ? <Link to={a.link} className={s.author}>{inner}</Link> : <span className={s.author}>{inner}</span>}</li>;
                })}
              </ul>
            </section>
          )}
        </aside>
      </div>
    </Page>
  );
}
