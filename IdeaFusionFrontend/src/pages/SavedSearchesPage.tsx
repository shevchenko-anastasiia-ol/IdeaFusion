import { useCallback, useEffect, useState } from "react";
import { useNavigate } from "react-router-dom";
import { KIND_LABEL, searchApi, toQueryString } from "../api/search";
import type { HistoryEntry, SavedSearch, SearchFilters, SearchNotification } from "../api/search";
import { Page } from "../layout/AppShell";
import { usePageMeta } from "../layout/PageMeta";
import { Badge, Button, ButtonLink, EmptyState, ErrorState, IconButton, Loading, Menu, PageHeader, Switch, Tabs, useDialogs, useToast, cx } from "../ui";
import { IconArrowLeft, IconBell, IconBookmark, IconClose, IconEdit, IconHistory, IconTrash } from "../ui/icons";
import sh from "./shared.module.css";

function describe(query: string, f: SearchFilters): string {
  const parts = [
    query && `«${query}»`,
    f.kind && KIND_LABEL[f.kind],
    f.category,
    f.tags?.map(t => `#${t}`).join(" "),
    f.role && `роль: ${f.role}`,
    f.hasOpenVacancies && "з вакансіями",
    f.minLikes && `від ${f.minLikes} вподобань`,
  ].filter(Boolean);
  return parts.length ? parts.join(" · ") : "Усі матеріали";
}

type Tab = "saved" | "notifications" | "history";

/** Saved searches with their subscription switch, new matches, and the search history. */
export default function SavedSearchesPage() {
  usePageMeta("Збережені пошуки", [{ label: "Пошук", to: "/search" }]);
  const navigate = useNavigate();
  const { confirm, prompt } = useDialogs();
  const toast = useToast();
  const [tab, setTab] = useState<Tab>("saved");
  const [saved, setSaved] = useState<SavedSearch[]>([]);
  const [notifications, setNotifications] = useState<SearchNotification[]>([]);
  const [history, setHistory] = useState<HistoryEntry[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const load = useCallback(() => {
    Promise.all([searchApi.saved(), searchApi.notifications(false, 30), searchApi.history("", 50)])
      .then(([sv, n, h]) => {
        setSaved(sv);
        setNotifications(n);
        setHistory(h);
        setError(null);
      })
      .catch(e => setError(e instanceof Error ? e.message : "Не вдалося завантажити"))
      .finally(() => setLoading(false));
  }, []);

  useEffect(load, [load]);

  const open = (query: string, filters: SearchFilters) => {
    const qs = toQueryString({ q: query || undefined, ...filters });
    navigate(`/search${qs ? `?${qs}` : ""}`);
  };

  const toggleNotify = async (sv: SavedSearch) => {
    try {
      const updated = await searchApi.updateSaved(sv.id, { name: sv.name, query: sv.query, filters: sv.filters, notify: !sv.notify });
      setSaved(list => list.map(x => (x.id === sv.id ? updated : x)));
    } catch (err) {
      toast.fromError(err, "Не вдалося змінити сповіщення");
    }
  };

  const rename = async (sv: SavedSearch) => {
    const name = (await prompt({ title: "Перейменувати пошук", label: "Нова назва", defaultValue: sv.name }))?.trim();
    if (!name || name === sv.name) return;
    try {
      const updated = await searchApi.updateSaved(sv.id, { name, query: sv.query, filters: sv.filters, notify: sv.notify });
      setSaved(list => list.map(x => (x.id === sv.id ? updated : x)));
    } catch {
      toast.error("Пошук з такою назвою вже є");
    }
  };

  const remove = async (sv: SavedSearch) => {
    const ok = await confirm({ title: `Видалити «${sv.name}»?`, message: "Сповіщення за цим пошуком теж зникнуть.", confirmLabel: "Видалити", danger: true });
    if (!ok) return;
    try {
      await searchApi.deleteSaved(sv.id);
      setSaved(list => list.filter(x => x.id !== sv.id));
      setNotifications(list => list.filter(n => n.savedSearchId !== sv.id));
    } catch (err) {
      toast.fromError(err, "Не вдалося видалити");
    }
  };

  const markAllRead = async () => {
    try {
      await searchApi.markRead();
      load();
    } catch (err) {
      toast.fromError(err, "Не вдалося позначити прочитаним");
    }
  };

  const openNotification = async (n: SearchNotification) => {
    if (!n.read) await searchApi.markRead([n.id]).catch(() => undefined);
    const sourceId = n.documentId.slice(n.documentId.indexOf(":") + 1);
    navigate({ post: `/posts/${sourceId}`, team: `/teams/${sourceId}`, vacancy: `/vacancies/${sourceId}`, user: `/users/${sourceId}` }[n.kind]);
  };

  const clearHistory = async () => {
    try {
      await searchApi.clearHistory();
      setHistory([]);
    } catch (err) {
      toast.fromError(err, "Не вдалося очистити історію");
    }
  };

  const removeHistory = async (h: HistoryEntry) => {
    try {
      await searchApi.deleteHistory(h.id);
      setHistory(list => list.filter(x => x.id !== h.id));
    } catch (err) {
      toast.fromError(err, "Не вдалося видалити");
    }
  };

  const unread = notifications.filter(n => !n.read).length;

  return (
    <Page width="narrow">
      <PageHeader
        title="Збережені пошуки"
        description="Підписки на пошуки, нові результати за ними та ваша історія."
        actions={
          <ButtonLink to="/search" variant="ghost" icon={<IconArrowLeft size={16} />}>
            До пошуку
          </ButtonLink>
        }
      />

      <Tabs
        label="Розділи"
        value={tab}
        onChange={setTab}
        items={[
          { value: "saved", label: "Мої пошуки", count: saved.length },
          { value: "notifications", label: "Нове за підписками", count: unread || undefined },
          { value: "history", label: "Історія", count: history.length },
        ]}
      />
      <div style={{ height: "var(--sp-5)" }} />

      {loading && <Loading />}
      {!loading && error && <ErrorState message={error} onRetry={load} />}

      {!loading && !error && tab === "saved" && (
        saved.length === 0 ? (
          <EmptyState
            icon={<IconBookmark />}
            title="Ще нічого не збережено"
            text="Налаштуйте фільтри на сторінці пошуку й натисніть «Зберегти пошук»."
            action={<ButtonLink to="/search">Відкрити пошук</ButtonLink>}
          />
        ) : (
          <ul className={sh.list}>
            {saved.map(sv => (
              <li key={sv.id} className={sh.row}>
                <div className={cx(sh.rowMain, sh.rowLink)} role="link" tabIndex={0} onClick={() => open(sv.query, sv.filters)} onKeyDown={e => e.key === "Enter" && open(sv.query, sv.filters)}>
                  <span className={sh.inline}>
                    <span className={sh.rowTitle}>{sv.name}</span>
                    {sv.unread > 0 && <Badge tone="accent">+{sv.unread}</Badge>}
                  </span>
                  <span className={sh.mono}>{describe(sv.query, sv.filters)}</span>
                </div>
                <div className={sh.rowActions}>
                  <Switch checked={sv.notify} onChange={() => void toggleNotify(sv)} label={<span className={sh.small}>Сповіщення</span>} />
                  <Menu
                    items={[
                      { label: "Перейменувати", icon: <IconEdit size={16} />, onSelect: () => void rename(sv) },
                      { label: "Видалити", icon: <IconTrash size={16} />, danger: true, onSelect: () => void remove(sv) },
                    ]}
                  />
                </div>
              </li>
            ))}
          </ul>
        )
      )}

      {!loading && !error && tab === "notifications" && (
        <>
          {unread > 0 && (
            <div className={sh.toolbar}>
              <span className={sh.mono}>непрочитаних: {unread}</span>
              <Button size="sm" variant="ghost" onClick={() => void markAllRead()}>
                Позначити все прочитаним
              </Button>
            </div>
          )}
          {notifications.length === 0 ? (
            <EmptyState icon={<IconBell />} title="Поки нічого нового" text="Тут з'являться нові пости, команди й вакансії, що відповідають пошукам зі сповіщеннями." />
          ) : (
            <ul className={sh.list}>
              {notifications.map(n => (
                <li key={n.id} className={cx(sh.row, sh.rowLink)} style={{ opacity: n.read ? 0.6 : 1 }} onClick={() => void openNotification(n)}>
                  <span style={{ width: 8, height: 8, borderRadius: "50%", flexShrink: 0, background: n.read ? "transparent" : "var(--accent)" }} aria-hidden="true" />
                  <div className={sh.rowMain}>
                    <span className={sh.rowTitle}>
                      <span className={sh.mono} style={{ marginRight: 8 }}>{KIND_LABEL[n.kind]}</span>
                      {n.title}
                    </span>
                    <span className={sh.mono}>
                      за пошуком «{n.savedSearchName}» · {new Date(n.createdAt).toLocaleString("uk-UA")}
                    </span>
                  </div>
                </li>
              ))}
            </ul>
          )}
        </>
      )}

      {!loading && !error && tab === "history" && (
        <>
          {history.length > 0 && (
            <div className={sh.toolbar}>
              <span className={sh.mono}>історію бачите лише ви</span>
              <Button size="sm" variant="ghost" onClick={() => void clearHistory()}>
                Очистити історію
              </Button>
            </div>
          )}
          {history.length === 0 ? (
            <EmptyState icon={<IconHistory />} title="Історія порожня" text="Її бачите лише ви." />
          ) : (
            <ul className={sh.list}>
              {history.map(h => (
                <li key={h.id} className={sh.row}>
                  <div className={cx(sh.rowMain, sh.rowLink)} role="link" tabIndex={0} onClick={() => open(h.query, h.filters)} onKeyDown={e => e.key === "Enter" && open(h.query, h.filters)}>
                    <span className={sh.rowTitle}>{describe(h.query, h.filters)}</span>
                    <span className={sh.mono}>
                      {new Date(h.searchedAt).toLocaleString("uk-UA")} · результатів: {h.resultsCount}
                    </span>
                  </div>
                  <IconButton label="Видалити з історії" size="sm" onClick={() => void removeHistory(h)}>
                    <IconClose size={15} />
                  </IconButton>
                </li>
              ))}
            </ul>
          )}
        </>
      )}
    </Page>
  );
}
