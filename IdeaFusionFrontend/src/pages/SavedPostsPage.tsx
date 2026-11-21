import { useCallback, useEffect, useState } from "react";
import { savedPostsApi } from "../api/posts";
import { timeAgo } from "../api/types";
import { useAuth } from "../context/AuthContext";
import { Page } from "../layout/AppShell";
import { usePageMeta } from "../layout/PageMeta";
import { ButtonLink, EmptyState, ErrorState, IconButton, PageHeader, Pagination, Skeleton, useToast } from "../ui";
import { IconBookmark, IconBookmarkFilled } from "../ui/icons";
import PostTile from "../components/PostTile";
import { plural } from "../lib/plural";
import sh from "./shared.module.css";
import s from "./SavedPostsPage.module.css";

type SavedPostItem = Awaited<ReturnType<typeof savedPostsApi.getMine>>[number];

const PAGE_SIZE = 15;

export default function SavedPostsPage() {
  usePageMeta("Збережене");
  const { loading: authLoading } = useAuth();
  const toast = useToast();
  const [posts, setPosts] = useState<SavedPostItem[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [page, setPage] = useState(1);
  const [busy, setBusy] = useState<number | null>(null);

  const load = useCallback(() => {
    setLoading(true);
    setError(null);
    savedPostsApi
      .getMine()
      .then(data => {
        setPosts(data);
        setPage(1);
      })
      .catch((e: Error) => setError(e.message ?? "Помилка завантаження"))
      .finally(() => setLoading(false));
  }, []);

  useEffect(() => {
    if (authLoading) return; // wait for auth to resolve
    load();
  }, [authLoading, load]);

  async function unsave(item: SavedPostItem) {
    setBusy(item.postId);
    try {
      await savedPostsApi.unsave(item.postId);
      setPosts(prev => prev.filter(p => p.postId !== item.postId));
      toast.success("Прибрано зі збереженого");
    } catch (err) {
      toast.fromError(err, "Не вдалося прибрати");
    } finally {
      setBusy(null);
    }
  }

  const totalPages = Math.max(1, Math.ceil(posts.length / PAGE_SIZE));
  const safePage = Math.min(page, totalPages);
  const pagePosts = posts.slice((safePage - 1) * PAGE_SIZE, safePage * PAGE_SIZE);

  return (
    <Page>
      <PageHeader
        title="Збережене"
        description={loading ? "Роботи, до яких ви хочете повернутися." : `${posts.length} ${plural(posts.length, ["публікація", "публікації", "публікацій"])}`}
      />

      {loading && (
        <div className={sh.tiles} aria-hidden="true">
          {[0, 1, 2, 3, 4, 5].map(i => (
            <div key={i} className={s.skeletonTile}>
              <Skeleton height="100%" radius={8} />
            </div>
          ))}
        </div>
      )}
      {!loading && error && <ErrorState message={error} onRetry={load} />}
      {!loading && !error && posts.length === 0 && (
        <EmptyState
          icon={<IconBookmark />}
          title="Ви ще нічого не зберегли"
          text="Натисніть на закладку під постом, щоб він з'явився тут."
          action={<ButtonLink to="/home">До стрічки</ButtonLink>}
        />
      )}
      {!loading && !error && posts.length > 0 && (
        <>
          <div className={sh.tiles}>
            {pagePosts.map(item => (
              <div key={item.savedPostId} className={s.cell}>
                <PostTile postId={item.postId} title={item.postTitle} mediaUrls={item.postMediaUrls} caption={`збережено ${timeAgo(item.savedAt)}`} />
                <IconButton label={`Прибрати «${item.postTitle}» зі збереженого`} size="sm" disabled={busy === item.postId} onClick={() => void unsave(item)} className={s.unsave}>
                  <IconBookmarkFilled size={16} />
                </IconButton>
              </div>
            ))}
          </div>
          <Pagination page={safePage} totalPages={totalPages} onChange={setPage} />
        </>
      )}
    </Page>
  );
}
