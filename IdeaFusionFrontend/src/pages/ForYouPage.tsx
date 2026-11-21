import { useCallback, useEffect, useState } from "react";
import SearchHitCard from "../components/SearchHitCard";
import { searchApi } from "../api/search";
import type { FeedItem, FeedResponse } from "../api/search";
import { Page } from "../layout/AppShell";
import { usePageMeta } from "../layout/PageMeta";
import { Button, ButtonLink, EmptyState, ErrorState, Loading, Notice, PageHeader } from "../ui";
import { IconSearch, IconSpark } from "../ui/icons";
import sh from "./shared.module.css";

const PAGE_SIZE = 20;

/**
 * The "For you" feed: posts picked by the reader's likes, saves, views and own posts,
 * each with the reason it is shown. What was shown and opened is logged to compare feed variants.
 */
export default function ForYouPage() {
  usePageMeta("Для вас");
  const [items, setItems] = useState<FeedItem[]>([]);
  const [last, setLast] = useState<FeedResponse | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const load = useCallback(async (page: number, asOf?: string) => {
    setLoading(true);
    try {
      const r = await searchApi.feed(page, PAGE_SIZE, asOf);
      const offset = (page - 1) * PAGE_SIZE;
      setItems(prev => (page === 1 ? r.items : [...prev, ...r.items]));
      setLast(r);
      setError(null);
      if (r.items.length > 0) {
        searchApi
          .feedEvents(r.variant, r.items.map((i, n) => ({ documentId: i.post.id, type: "impression", position: offset + n })))
          .catch(() => undefined);
      }
    } catch (e) {
      setError(e instanceof Error ? e.message : "Стрічка недоступна");
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    void load(1);
  }, [load]);

  const click = (item: FeedItem, position: number) => {
    if (last) searchApi.feedEvents(last.variant, [{ documentId: item.post.id, type: "click", position }]).catch(() => undefined);
  };

  return (
    <Page width="narrow">
      <PageHeader
        title="Для вас"
        description="Добірка робіт за вашими вподобаннями, збереженнями та переглядами — з поясненням, чому кожна тут."
        actions={
          <ButtonLink to="/search" variant="ghost" icon={<IconSearch size={16} />}>
            Пошук
          </ButtonLink>
        }
      />

      {last?.variant === "cold-start" && (
        <div style={{ marginBottom: "var(--sp-4)" }}>
          <Notice>Поки що ми мало знаємо про ваші смаки, тож показуємо популярне. Вподобайте чи збережіть кілька робіт, і стрічка підлаштується.</Notice>
        </div>
      )}

      {error && items.length === 0 && <ErrorState title="Стрічка недоступна" message={error} onRetry={() => void load(1)} />}

      <div className={sh.stack}>
        {items.map((item, n) => (
          <SearchHitCard key={item.post.id} hit={item.post} note={item.reason.text} onOpen={() => click(item, n)} />
        ))}
        {!loading && items.length === 0 && !error && (
          <EmptyState icon={<IconSpark />} title="Нових робіт для вас поки немає" text="Загляньте пізніше або перегляньте загальну стрічку." action={<ButtonLink to="/home">До стрічки</ButtonLink>} />
        )}
        {loading && <Loading />}
        {error && items.length > 0 && <Notice tone="error">{error}</Notice>}
        {!loading && last?.hasMore && (
          <div style={{ display: "flex", justifyContent: "center" }}>
            <Button onClick={() => void load(last.page + 1, last.asOf)}>Показати ще</Button>
          </div>
        )}
      </div>
    </Page>
  );
}
