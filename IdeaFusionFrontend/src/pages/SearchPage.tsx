import { useEffect, useMemo, useState, type ReactNode } from "react";
import { useLocation, useNavigate } from "react-router-dom";
import SearchBox from "../components/SearchBox";
import SearchHitCard from "../components/SearchHitCard";
import { SEARCH_KINDS, filtersOf, fromQueryString, searchApi, toQueryString } from "../api/search";
import type { HistoryEntry, RoleSuggestion, SearchParams, SearchResponse, SearchSort, TagSuggestion } from "../api/search";
import { Page } from "../layout/AppShell";
import { usePageMeta } from "../layout/PageMeta";
import {
  Button,
  ButtonLink,
  Checkbox,
  Combobox,
  Dialog,
  EmptyState,
  ErrorState,
  Field,
  Input,
  Notice,
  PageHeader,
  Select,
  Skeleton,
  Tabs,
  Tag,
  cx,
} from "../ui";
import { IconBookmark, IconFilter, IconSearch, IconSpark } from "../ui/icons";
import sh from "./shared.module.css";
import s from "./SearchPage.module.css";

const PAGE_SIZE = 20;

const SORTS: { value: SearchSort; label: string }[] = [
  { value: "relevance", label: "За релевантністю" },
  { value: "new", label: "Спершу нові" },
  { value: "popular", label: "Популярні" },
];

function FilterBlock({ title, children }: { title: string; children: ReactNode }) {
  return (
    <section className={s.filterBlock}>
      <h3 className={s.filterTitle}>{title}</h3>
      {children}
    </section>
  );
}

/** Tag autocomplete by any spelling or synonym ("ілюс", "ilustr", "3-д"). */
function TagSearch({ onPick }: { onPick: (key: string) => void }) {
  const [input, setInput] = useState("");
  const [options, setOptions] = useState<TagSuggestion[]>([]);
  useEffect(() => {
    const timer = setTimeout(() => searchApi.tags(input, 8).then(setOptions).catch(() => setOptions([])), 200);
    return () => clearTimeout(timer);
  }, [input]);
  return (
    <Combobox<TagSuggestion>
      inputValue={input}
      onInputChange={setInput}
      options={options}
      getKey={o => o.key}
      onSelect={o => {
        onPick(o.key);
        setInput("");
      }}
      renderOption={o => (
        <>
          <span>#{o.label}</span>
          <span className={sh.mono}>{o.documents}</span>
        </>
      )}
      placeholder="Додати тег"
      ariaLabel="Додати тег"
      emptyText="Немає такого тегу"
    />
  );
}

/** Role autocomplete across languages and keyboard layouts; free text is allowed too. */
function RoleSearch({ value, onPick }: { value: string; onPick: (role: string) => void }) {
  const [input, setInput] = useState(value);
  const [options, setOptions] = useState<RoleSuggestion[]>([]);
  useEffect(() => setInput(value), [value]);
  useEffect(() => {
    const timer = setTimeout(() => searchApi.roles(input, 8).then(setOptions).catch(() => setOptions([])), 200);
    return () => clearTimeout(timer);
  }, [input]);
  return (
    <div className={s.roleRow}>
      <Combobox<RoleSuggestion>
        inputValue={input}
        onInputChange={v => {
          setInput(v);
          if (!v) onPick("");
        }}
        options={options}
        getKey={o => o.label}
        onSelect={o => {
          setInput(o.label);
          onPick(o.label);
        }}
        onEnter={text => onPick(text.trim())}
        renderOption={o => (
          <>
            <span>{o.label}</span>
            <span className={sh.mono}>
              {[o.vacancies && `вак. ${o.vacancies}`, o.teams && `ком. ${o.teams}`, o.people && `люд. ${o.people}`].filter(Boolean).join(" · ")}
            </span>
          </>
        )}
        placeholder="ілюстратор, motion designer…"
        ariaLabel="Роль або спеціалізація"
      />
    </div>
  );
}

function SaveSearchDialog({
  open,
  params,
  onClose,
  onSaved,
}: {
  open: boolean;
  params: SearchParams;
  onClose: () => void;
  onSaved: () => void;
}) {
  const [name, setName] = useState("");
  const [notify, setNotify] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [saving, setSaving] = useState(false);

  useEffect(() => {
    if (open) {
      setName(params.q || params.role || params.category || "Мій пошук");
      setError(null);
    }
  }, [open, params]);

  const save = async () => {
    setSaving(true);
    try {
      await searchApi.createSaved({ name: name.trim(), query: params.q, filters: filtersOf(params), notify });
      onSaved();
    } catch (e) {
      const message = e instanceof Error ? e.message : "";
      setError(message.includes("409") ? "Пошук з такою назвою вже є або їх забагато" : "Не вдалося зберегти");
    } finally {
      setSaving(false);
    }
  };

  return (
    <Dialog
      open={open}
      onClose={onClose}
      title="Зберегти пошук"
      footer={
        <>
          <Button variant="ghost" onClick={onClose}>
            Скасувати
          </Button>
          <Button variant="primary" loading={saving} disabled={!name.trim()} onClick={() => void save()}>
            Зберегти
          </Button>
        </>
      }
    >
      <Field label="Назва" htmlFor="saved-name">
        <Input id="saved-name" data-autofocus value={name} maxLength={100} onChange={e => setName(e.target.value)} />
      </Field>
      <Checkbox label="Повідомляти про нові результати" checked={notify} onChange={e => setNotify(e.target.checked)} />
      {error && <Notice tone="error">{error}</Notice>}
    </Dialog>
  );
}

/**
 * Unified search over posts, teams, vacancies and people. The URL holds the whole search
 * (/search?q=…&kind=…&tag=…), so a search can be bookmarked, saved or restored from history.
 */
export default function SearchPage() {
  usePageMeta("Пошук");
  const location = useLocation();
  const navigate = useNavigate();
  const params = useMemo(() => fromQueryString(location.search), [location.search]);
  const [text, setText] = useState(params.q ?? "");
  const [result, setResult] = useState<SearchResponse | null>(null);
  const [loading, setLoading] = useState(true);
  const [loadingMore, setLoadingMore] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [saveOpen, setSaveOpen] = useState(false);
  const [filtersOpen, setFiltersOpen] = useState(false);
  const [reload, setReload] = useState(0);
  // A fresh value each time Ctrl+K is pressed, so the box refocuses even when already here.
  const focusToken = (location.state as { focus?: unknown } | null)?.focus;
  const focusOnArrive = !!focusToken;

  useEffect(() => setText(params.q ?? ""), [params.q]);

  useEffect(() => {
    let cancelled = false;
    setLoading(true);
    setError(null);
    searchApi
      .search({ ...params, pageSize: PAGE_SIZE })
      .then(r => {
        if (!cancelled) setResult(r);
      })
      .catch(e => {
        if (!cancelled) setError(e instanceof Error ? e.message : "Пошук недоступний");
      })
      .finally(() => {
        if (!cancelled) setLoading(false);
      });
    return () => {
      cancelled = true;
    };
  }, [params, reload]);

  const apply = (next: SearchParams) => {
    const qs = toQueryString(next);
    navigate(`/search${qs ? `?${qs}` : ""}`);
  };
  const update = (patch: Partial<SearchParams>) => apply({ ...params, ...patch });

  const loadMore = async () => {
    if (!result?.nextCursor) return;
    setLoadingMore(true);
    try {
      const more = await searchApi.search({ ...params, pageSize: PAGE_SIZE, cursor: result.nextCursor });
      setResult({ ...more, items: [...result.items, ...more.items] });
    } catch (e) {
      setError(e instanceof Error ? e.message : "Не вдалося завантажити");
    } finally {
      setLoadingMore(false);
    }
  };

  const tags = params.tags ?? [];
  const addTag = (key: string) => {
    if (!tags.includes(key)) update({ tags: [...tags, key] });
  };
  const removeTag = (key: string) => update({ tags: tags.filter(t => t !== key) });
  const pickHistory = (h: HistoryEntry) => apply({ q: h.query || undefined, ...h.filters });

  const activeFilters = Object.entries(filtersOf(params)).filter(([k, v]) => k !== "sort" && v !== undefined).length;
  const hasFilters = activeFilters > 0;
  const kinds = result?.facets.kinds ?? {};
  const allCount = Object.values(kinds).reduce((a, b) => a + (b ?? 0), 0);

  return (
    <Page width="wide">
      <PageHeader
        title="Пошук"
        description="Пости, команди, вакансії та люди — одним запитом."
        actions={
          <>
            <ButtonLink to="/search/saved" variant="ghost" icon={<IconBookmark size={16} />}>
              Збережені пошуки
            </ButtonLink>
            <ButtonLink to="/for-you" variant="ghost" icon={<IconSpark size={16} />}>
              Для вас
            </ButtonLink>
          </>
        }
      />

      <div className={s.searchRow}>
        <SearchBox
          value={text}
          onChange={setText}
          onSubmit={q => update({ q: q.trim() || undefined })}
          onPickTag={key => {
            setText("");
            apply({ ...params, q: undefined, tags: [...tags.filter(t => t !== key), key] });
          }}
          onPickHistory={pickHistory}
          autoFocus={focusOnArrive}
          focusToken={focusToken}
        />
      </div>

      {result?.didYouMean && (
        <p className={s.didYouMean}>
          Можливо, ви мали на увазі:{" "}
          <button type="button" className={s.linkBtn} onClick={() => update({ q: result.didYouMean })}>
            {result.didYouMean}
          </button>
        </p>
      )}

      <Tabs
        label="Тип результатів"
        value={params.kind ?? "all"}
        onChange={v => update({ kind: v === "all" ? undefined : (v as SearchParams["kind"]) })}
        items={SEARCH_KINDS.map(k => ({
          value: k.kind ?? "all",
          label: k.label,
          count: result ? (k.kind ? kinds[k.kind] ?? 0 : allCount) : undefined,
        }))}
        className={s.tabs}
      />

      <div className={s.toolbar}>
        <span className={sh.mono}>{result && !loading ? `знайдено: ${result.total}` : " "}</span>
        <div className={sh.inline}>
          <Button size="sm" variant="secondary" className={s.filtersToggle} icon={<IconFilter size={15} />} onClick={() => setFiltersOpen(o => !o)} aria-expanded={filtersOpen}>
            Фільтри{activeFilters > 0 ? ` · ${activeFilters}` : ""}
          </Button>
          <Select
            aria-label="Сортування"
            className={s.sort}
            value={params.sort ?? "relevance"}
            onChange={e => update({ sort: e.target.value === "relevance" ? undefined : (e.target.value as SearchSort) })}
          >
            {SORTS.map(o => (
              <option key={o.value} value={o.value}>
                {o.label}
              </option>
            ))}
          </Select>
          <Button size="sm" variant="primary" onClick={() => setSaveOpen(true)} disabled={!params.q && !hasFilters} icon={<IconBookmark size={15} />}>
            Зберегти пошук
          </Button>
        </div>
      </div>

      <div className={s.columns}>
        <section className={s.results} aria-live="polite" aria-busy={loading}>
          {loading && (
            <div className={sh.stack}>
              {[0, 1, 2, 3].map(i => (
                <div key={i} className={s.skeleton}>
                  <Skeleton width={76} height={76} radius={8} />
                  <div className={sh.stackSm} style={{ flex: 1 }}>
                    <Skeleton width="50%" height={16} />
                    <Skeleton width="80%" />
                    <Skeleton width="30%" />
                  </div>
                </div>
              ))}
            </div>
          )}
          {!loading && error && <ErrorState title="Пошук недоступний" message={error} onRetry={() => setReload(n => n + 1)} />}
          {!loading && !error && result && result.items.length === 0 && (
            <EmptyState
              icon={<IconSearch />}
              title="Нічого не знайдено"
              text={`Спробуйте інше слово${hasFilters ? " або скиньте фільтри" : ""}.`}
              action={hasFilters && <Button onClick={() => apply({ q: params.q, sort: params.sort })}>Скинути фільтри</Button>}
            />
          )}
          {!loading && !error && result && result.items.length > 0 && (
            <div className={sh.stack}>
              {result.items.map((hit, n) => (
                <SearchHitCard
                  key={hit.id}
                  hit={hit}
                  onTagClick={addTag}
                  onOpen={() => {
                    searchApi.click(hit.kind, n).catch(() => undefined);
                  }}
                />
              ))}
              {result.nextCursor && (
                <div className={s.more}>
                  <Button onClick={() => void loadMore()} loading={loadingMore}>
                    Показати ще
                  </Button>
                </div>
              )}
            </div>
          )}
        </section>

        <aside className={cx(s.filters, filtersOpen && s.filtersOpen)} aria-label="Фільтри">
          <FilterBlock title="категорії">
            <div className={s.chips}>
              {(result?.facets.categories ?? []).map(c => (
                <Tag key={c.value} active={params.category === c.value} onClick={() => update({ category: params.category === c.value ? undefined : c.value })}>
                  {c.value} <span className={s.facetCount}>{c.count}</span>
                </Tag>
              ))}
              {params.category && !(result?.facets.categories ?? []).some(c => c.value === params.category) && (
                <Tag active onClick={() => update({ category: undefined })}>
                  {params.category} ×
                </Tag>
              )}
              {!result?.facets.categories.length && !params.category && <span className={sh.faint}>—</span>}
            </div>
          </FilterBlock>

          <FilterBlock title="теги">
            {tags.length > 0 && (
              <div className={s.chips}>
                {tags.map(t => (
                  <Tag key={t} active onRemove={() => removeTag(t)}>
                    #{t}
                  </Tag>
                ))}
                {tags.length > 1 && (
                  <Tag onClick={() => update({ tagMode: params.tagMode === "all" ? undefined : "all" })} title="Чи мають бути всі теги одразу">
                    {params.tagMode === "all" ? "усі разом" : "будь-який"}
                  </Tag>
                )}
              </div>
            )}
            <TagSearch onPick={addTag} />
            <div className={s.chips}>
              {(result?.facets.tags ?? [])
                .filter(t => !tags.includes(t.key))
                .slice(0, 10)
                .map(t => (
                  <Tag key={t.key} onClick={() => addTag(t.key)}>
                    #{t.label} <span className={s.facetCount}>{t.count}</span>
                  </Tag>
                ))}
            </div>
          </FilterBlock>

          {params.kind !== "post" && (
            <FilterBlock title="роль або спеціалізація">
              <RoleSearch value={params.role ?? ""} onPick={role => update({ role: role || undefined })} />
              {(params.kind === undefined || params.kind === "team") && (
                <Checkbox
                  label="Лише команди з відкритими вакансіями"
                  checked={!!params.hasOpenVacancies}
                  onChange={e => update({ hasOpenVacancies: e.target.checked || undefined })}
                />
              )}
            </FilterBlock>
          )}

          <FilterBlock title="дата створення">
            <div className={s.dates}>
              <Field label="Від" htmlFor="f-from">
                <Input id="f-from" type="date" value={params.from?.slice(0, 10) ?? ""} onChange={e => update({ from: e.target.value || undefined })} />
              </Field>
              <Field label="До" htmlFor="f-to">
                <Input id="f-to" type="date" value={params.to?.slice(0, 10) ?? ""} onChange={e => update({ to: e.target.value || undefined })} />
              </Field>
            </div>
          </FilterBlock>

          {(params.kind === undefined || params.kind === "post") && (
            <FilterBlock title="щонайменше вподобань">
              <Input type="number" min={0} aria-label="Мінімум вподобань" value={params.minLikes ?? ""} onChange={e => update({ minLikes: Number(e.target.value) || undefined })} />
            </FilterBlock>
          )}

          {hasFilters && (
            <Button variant="ghost" size="sm" onClick={() => apply({ q: params.q, sort: params.sort })}>
              Скинути фільтри
            </Button>
          )}
        </aside>
      </div>

      <SaveSearchDialog
        open={saveOpen}
        params={params}
        onClose={() => setSaveOpen(false)}
        onSaved={() => {
          setSaveOpen(false);
          navigate("/search/saved");
        }}
      />
    </Page>
  );
}
