import { FunctionComponent, ReactNode, useCallback, useEffect, useState } from "react";
import { Box, Button, Switch } from "@mui/material";
import { useNavigate } from "react-router-dom";
import FrameComponent2 from "../components/FrameComponent2";
import { KIND_LABEL, searchApi, toQueryString } from "../api/search";
import type { HistoryEntry, SavedSearch, SearchFilters, SearchNotification } from "../api/search";
import styles from "./SearchPage.module.css";

function describe(query: string, f: SearchFilters): string {
  const parts = [query && `«${query}»`, f.kind && KIND_LABEL[f.kind], f.category, f.tags?.map(t => `#${t}`).join(" "),
    f.role && `роль: ${f.role}`, f.hasOpenVacancies && "з вакансіями", f.minLikes && `від ${f.minLikes} ♥`].filter(Boolean);
  return parts.length ? parts.join(" · ") : "Усі матеріали";
}

const Section: FunctionComponent<{ title: string; action?: ReactNode; children: ReactNode }> = ({ title, action, children }) => (
  <Box component="section" sx={{ display: "flex", flexDirection: "column", gap: "10px" }}>
    <Box sx={{ display: "flex", alignItems: "center", justifyContent: "space-between" }}>
      <Box component="h2" sx={{ m: 0, fontSize: 22, color: "#fff", fontWeight: 600 }}>{title}</Box>
      {action}
    </Box>
    {children}
  </Box>
);

const rowSx = {
  display: "flex", alignItems: "center", gap: "14px", p: "14px 18px", background: "#181818",
  border: "1px solid #222", borderRadius: "18px",
} as const;

const linkButton = { color: "#a48fff", textTransform: "none", minWidth: 0 } as const;

/** Saved searches with their subscription switch, new matches, and the search history. */
const SavedSearchesPage: FunctionComponent = () => {
  const navigate = useNavigate();
  const [saved, setSaved] = useState<SavedSearch[]>([]);
  const [notifications, setNotifications] = useState<SearchNotification[]>([]);
  const [history, setHistory] = useState<HistoryEntry[]>([]);
  const [error, setError] = useState<string | null>(null);

  const load = useCallback(() => {
    Promise.all([searchApi.saved(), searchApi.notifications(false, 30), searchApi.history("", 50)])
      .then(([s, n, h]) => { setSaved(s); setNotifications(n); setHistory(h); setError(null); })
      .catch(e => setError(e instanceof Error ? e.message : "Не вдалося завантажити"));
  }, []);
  useEffect(load, [load]);

  const open = (query: string, filters: SearchFilters) => {
    const qs = toQueryString({ q: query || undefined, ...filters });
    navigate(`/search${qs ? `?${qs}` : ""}`);
  };

  const toggleNotify = async (s: SavedSearch) => {
    const updated = await searchApi.updateSaved(s.id, { name: s.name, query: s.query, filters: s.filters, notify: !s.notify });
    setSaved(list => list.map(x => (x.id === s.id ? updated : x)));
  };

  const rename = async (s: SavedSearch) => {
    const name = prompt("Нова назва", s.name)?.trim();
    if (!name || name === s.name) return;
    try {
      const updated = await searchApi.updateSaved(s.id, { name, query: s.query, filters: s.filters, notify: s.notify });
      setSaved(list => list.map(x => (x.id === s.id ? updated : x)));
    } catch {
      alert("Пошук з такою назвою вже є");
    }
  };

  const remove = async (s: SavedSearch) => {
    if (!confirm(`Видалити «${s.name}»?`)) return;
    await searchApi.deleteSaved(s.id);
    setSaved(list => list.filter(x => x.id !== s.id));
    setNotifications(list => list.filter(n => n.savedSearchId !== s.id));
  };

  const markAllRead = async () => {
    await searchApi.markRead();
    load();
  };

  const openNotification = async (n: SearchNotification) => {
    if (!n.read) await searchApi.markRead([n.id]).catch(() => undefined);
    const sourceId = n.documentId.slice(n.documentId.indexOf(":") + 1);
    navigate({ post: `/posts/${sourceId}`, team: `/teams/${sourceId}`, vacancy: `/vacancies/${sourceId}`, user: `/users/${sourceId}` }[n.kind]);
  };

  const unread = notifications.filter(n => !n.read).length;

  return (
    <Box className={styles.page}>
      <FrameComponent2 />
      <main className={styles.main}>
        <Box sx={{ display: "flex", alignItems: "center", justifyContent: "space-between", flexWrap: "wrap", gap: "12px" }}>
          <h1 className={styles.title}>Збережені пошуки</h1>
          <Button onClick={() => navigate("/search")} sx={linkButton}>← До пошуку</Button>
        </Box>
        {error && <Box sx={{ color: "#ff6b6b" }}>{error}</Box>}

        <Section title="Мої пошуки">
          {saved.length === 0 && (
            <Box sx={{ color: "#888" }}>Ще нічого не збережено. Налаштуйте фільтри на сторінці пошуку й натисніть «Зберегти пошук».</Box>
          )}
          {saved.map(s => (
            <Box key={s.id} sx={rowSx}>
              <Box sx={{ flex: 1, minWidth: 0, cursor: "pointer" }} onClick={() => open(s.query, s.filters)}>
                <Box sx={{ color: "#fff", fontWeight: 600 }}>
                  {s.name}
                  {s.unread > 0 && (
                    <Box component="span" sx={{ ml: "8px", px: "8px", borderRadius: "20px", background: "#7c5cfc", color: "#fff", fontSize: 12 }}>
                      +{s.unread}
                    </Box>
                  )}
                </Box>
                <Box sx={{ color: "#888", fontSize: 13 }}>{describe(s.query, s.filters)}</Box>
              </Box>
              <Box sx={{ display: "flex", alignItems: "center", color: "#888", fontSize: 13 }} title="Повідомляти про нові результати">
                <Switch size="small" checked={s.notify} onChange={() => toggleNotify(s)} />
                Сповіщення
              </Box>
              <Button onClick={() => rename(s)} sx={linkButton}>Перейменувати</Button>
              <Button onClick={() => remove(s)} sx={{ ...linkButton, color: "#ff6b6b" }}>Видалити</Button>
            </Box>
          ))}
        </Section>

        <Section title={`Нове за підписками${unread ? ` (${unread})` : ""}`}
          action={unread > 0 && <Button onClick={markAllRead} sx={linkButton}>Позначити все прочитаним</Button>}>
          {notifications.length === 0 && (
            <Box sx={{ color: "#888" }}>Тут з'являться нові пости, команди й вакансії, що відповідають пошукам зі сповіщеннями.</Box>
          )}
          {notifications.map(n => (
            <Box key={n.id} sx={{ ...rowSx, cursor: "pointer", opacity: n.read ? 0.6 : 1 }} onClick={() => openNotification(n)}>
              {!n.read && <Box sx={{ width: 8, height: 8, borderRadius: "50%", background: "#7c5cfc", flexShrink: 0 }} />}
              <Box sx={{ flex: 1, minWidth: 0 }}>
                <Box sx={{ color: "#fff" }}>{KIND_LABEL[n.kind]}: {n.title}</Box>
                <Box sx={{ color: "#888", fontSize: 13 }}>
                  за пошуком «{n.savedSearchName}» · {new Date(n.createdAt).toLocaleString("uk-UA")}
                </Box>
              </Box>
            </Box>
          ))}
        </Section>

        <Section title="Історія пошуку"
          action={history.length > 0 && (
            <Button onClick={async () => { await searchApi.clearHistory(); setHistory([]); }} sx={linkButton}>Очистити історію</Button>
          )}>
          {history.length === 0 && <Box sx={{ color: "#888" }}>Історія порожня. Її бачите лише ви.</Box>}
          {history.map(h => (
            <Box key={h.id} sx={rowSx}>
              <Box sx={{ flex: 1, minWidth: 0, cursor: "pointer" }} onClick={() => open(h.query, h.filters)}>
                <Box sx={{ color: "#ddd" }}>{describe(h.query, h.filters)}</Box>
                <Box sx={{ color: "#777", fontSize: 12 }}>
                  {new Date(h.searchedAt).toLocaleString("uk-UA")} · результатів: {h.resultsCount}
                </Box>
              </Box>
              <Button aria-label="Видалити з історії" sx={{ ...linkButton, color: "#888" }}
                onClick={async () => { await searchApi.deleteHistory(h.id); setHistory(list => list.filter(x => x.id !== h.id)); }}>
                ×
              </Button>
            </Box>
          ))}
        </Section>
      </main>
    </Box>
  );
};

export default SavedSearchesPage;
