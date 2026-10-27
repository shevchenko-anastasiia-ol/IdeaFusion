import { FunctionComponent, useEffect, useState } from "react";
import { Box, Button } from "@mui/material";
import { useNavigate } from "react-router-dom";
import FrameComponent2 from "../components/FrameComponent2";
import SearchHitCard from "../components/SearchHitCard";
import { searchApi } from "../api/search";
import type { FeedItem, FeedResponse } from "../api/search";
import styles from "./SearchPage.module.css";

const PAGE_SIZE = 20;

/**
 * The "For you" feed: posts picked by the reader's likes, saves, views and own posts,
 * each with the reason it is shown. What was shown and opened is logged to compare feed variants.
 */
const ForYouPage: FunctionComponent = () => {
  const navigate = useNavigate();
  const [items, setItems] = useState<FeedItem[]>([]);
  const [last, setLast] = useState<FeedResponse | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const load = async (page: number, asOf?: string) => {
    setLoading(true);
    try {
      const r = await searchApi.feed(page, PAGE_SIZE, asOf);
      const offset = (page - 1) * PAGE_SIZE;
      setItems(prev => (page === 1 ? r.items : [...prev, ...r.items]));
      setLast(r);
      setError(null);
      if (r.items.length > 0) {
        searchApi.feedEvents(r.variant, r.items.map((i, n) => ({ documentId: i.post.id, type: "impression", position: offset + n })))
          .catch(() => undefined);
      }
    } catch (e) {
      setError(e instanceof Error ? e.message : "Стрічка недоступна");
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => { load(1); }, []);

  const click = (item: FeedItem, position: number) => {
    if (last) searchApi.feedEvents(last.variant, [{ documentId: item.post.id, type: "click", position }]).catch(() => undefined);
  };

  return (
    <Box className={styles.page}>
      <FrameComponent2 />
      <main className={styles.main}>
        <Box sx={{ display: "flex", alignItems: "center", justifyContent: "space-between", flexWrap: "wrap", gap: "12px" }}>
          <h1 className={styles.title}>Для вас</h1>
          <Button onClick={() => navigate("/search")} sx={{ color: "#a48fff", textTransform: "none" }}>Пошук →</Button>
        </Box>
        {last?.variant === "cold-start" && (
          <Box sx={{ color: "#888", fontSize: 14 }}>
            Поки що ми мало знаємо про ваші смаки, тож показуємо популярне. Вподобайте чи збережіть кілька робіт, і стрічка підлаштується.
          </Box>
        )}
        {error && <Box sx={{ color: "#ff6b6b" }}>{error}</Box>}
        <Box sx={{ display: "flex", flexDirection: "column", gap: "14px", maxWidth: 900 }}>
          {items.map((item, n) => (
            <SearchHitCard key={item.post.id} hit={item.post} note={item.reason.text} onOpen={() => click(item, n)} />
          ))}
          {!loading && items.length === 0 && !error && (
            <Box sx={{ color: "#888" }}>Нових робіт для вас поки немає.</Box>
          )}
          {loading && <Box sx={{ color: "#888" }}>Завантаження…</Box>}
          {!loading && last?.hasMore && (
            <Button onClick={() => load(last.page + 1, last.asOf)} sx={{ alignSelf: "center", color: "#a48fff", textTransform: "none" }}>
              Показати ще
            </Button>
          )}
        </Box>
      </main>
    </Box>
  );
};

export default ForYouPage;
