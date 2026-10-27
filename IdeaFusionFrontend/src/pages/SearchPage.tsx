import { FunctionComponent, ReactNode, useEffect, useMemo, useState } from "react";
import {
  Autocomplete, Box, Button, Checkbox, Dialog, DialogActions, DialogContent, DialogTitle,
  FormControlLabel, MenuItem, Select, TextField,
} from "@mui/material";
import { useLocation, useNavigate } from "react-router-dom";
import FrameComponent2 from "../components/FrameComponent2";
import SearchBox from "../components/SearchBox";
import SearchHitCard from "../components/SearchHitCard";
import {
  SEARCH_KINDS, filtersOf, fromQueryString, searchApi, toQueryString,
} from "../api/search";
import type {
  HistoryEntry, RoleSuggestion, SearchParams, SearchResponse, SearchSort, TagSuggestion,
} from "../api/search";
import styles from "./SearchPage.module.css";

const PAGE_SIZE = 20;

const SORTS: { value: SearchSort; label: string }[] = [
  { value: "relevance", label: "За релевантністю" },
  { value: "new", label: "Спершу нові" },
  { value: "popular", label: "Популярні" },
];

const darkField = {
  "& .MuiInputBase-root": { color: "#ddd", background: "#1b1b1b", borderRadius: "12px", fontSize: 14 },
  "& fieldset": { borderColor: "#333" },
  "& .MuiInputLabel-root": { color: "#777" },
  "& .MuiSvgIcon-root": { color: "#777" },
} as const;

const Pill: FunctionComponent<{ active?: boolean; onClick?: () => void; children: ReactNode; title?: string }> = ({ active, onClick, children, title }) => (
  <Box component="button" type="button" onClick={onClick} title={title}
    sx={{ cursor: "pointer", px: "14px", py: "6px", borderRadius: "30px", fontSize: 14, fontFamily: "var(--font-inter)",
      border: `1px solid ${active ? "#7c5cfc" : "#333"}`, color: active ? "#fff" : "#aaa",
      background: active ? "rgba(124,92,252,.25)" : "transparent", "&:hover": { borderColor: "#7c5cfc" } }}>
    {children}
  </Box>
);

/**
 * Unified search over posts, teams, vacancies and people. The URL holds the whole search
 * (/search?q=…&kind=…&tag=…), so a search can be bookmarked, saved or restored from history.
 */
const SearchPage: FunctionComponent = () => {
  const location = useLocation();
  const navigate = useNavigate();
  const params = useMemo(() => fromQueryString(location.search), [location.search]);

  const [text, setText] = useState(params.q ?? "");
  const [result, setResult] = useState<SearchResponse | null>(null);
  const [loading, setLoading] = useState(true);
  const [loadingMore, setLoadingMore] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [saveOpen, setSaveOpen] = useState(false);

  useEffect(() => setText(params.q ?? ""), [params.q]);

  useEffect(() => {
    let cancelled = false;
    setLoading(true);
    setError(null);
    searchApi.search({ ...params, pageSize: PAGE_SIZE })
      .then(r => { if (!cancelled) setResult(r); })
      .catch(e => { if (!cancelled) setError(e instanceof Error ? e.message : "Пошук недоступний"); })
      .finally(() => { if (!cancelled) setLoading(false); });
    return () => { cancelled = true; };
  }, [params]);

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
  const addTag = (key: string) => { if (!tags.includes(key)) update({ tags: [...tags, key] }); };
  const removeTag = (key: string) => update({ tags: tags.filter(t => t !== key) });
  const pickHistory = (h: HistoryEntry) => apply({ q: h.query || undefined, ...h.filters });
  const hasFilters = Object.entries(filtersOf(params)).some(([k, v]) => k !== "sort" && v !== undefined);

  const kinds = result?.facets.kinds ?? {};
  const allCount = Object.values(kinds).reduce((a, b) => a + (b ?? 0), 0);

  return (
    <Box className={styles.page}>
      <FrameComponent2 />
      <main className={styles.main}>
        <Box sx={{ display: "flex", alignItems: "center", justifyContent: "space-between", gap: "12px", flexWrap: "wrap" }}>
          <h1 className={styles.title}>Пошук</h1>
          <Box sx={{ display: "flex", gap: "8px" }}>
            <Pill onClick={() => navigate("/search/saved")}>Збережені пошуки</Pill>
            <Pill onClick={() => navigate("/for-you")}>Для вас</Pill>
          </Box>
        </Box>

        <SearchBox
          value={text}
          onChange={setText}
          onSubmit={q => update({ q: q.trim() || undefined })}
          onPickTag={key => { setText(""); apply({ ...params, q: undefined, tags: [...tags.filter(t => t !== key), key] }); }}
          onPickHistory={pickHistory}
        />

        {result?.didYouMean && (
          <Box sx={{ fontSize: 15, color: "#aaa" }}>
            Можливо, ви мали на увазі:{" "}
            <Box component="button" type="button" onClick={() => update({ q: result.didYouMean })}
              sx={{ background: "none", border: "none", p: 0, color: "#a48fff", fontSize: 15, cursor: "pointer", textDecoration: "underline" }}>
              {result.didYouMean}
            </Box>
          </Box>
        )}

        <div className={styles.toolbar}>
          <div className={styles.tabs} role="tablist">
            {SEARCH_KINDS.map(k => (
              <Pill key={k.label} active={params.kind === k.kind} onClick={() => update({ kind: k.kind })}>
                {k.label}{" "}
                <Box component="span" sx={{ color: "#888" }}>{k.kind ? (kinds[k.kind] ?? 0) : allCount}</Box>
              </Pill>
            ))}
          </div>
          <Box sx={{ display: "flex", gap: "8px", alignItems: "center" }}>
            <Select size="small" value={params.sort ?? "relevance"} sx={{ ...darkField, minWidth: 180, color: "#ddd" }}
              onChange={e => update({ sort: e.target.value === "relevance" ? undefined : e.target.value as SearchSort })}>
              {SORTS.map(s => <MenuItem key={s.value} value={s.value}>{s.label}</MenuItem>)}
            </Select>
            <Button variant="contained" onClick={() => setSaveOpen(true)}
              disabled={!params.q && !hasFilters}
              sx={{ borderRadius: "30px", textTransform: "none", background: "#7c5cfc", "&:hover": { background: "#6a4ae6" } }}>
              Зберегти пошук
            </Button>
          </Box>
        </div>

        <div className={styles.columns}>
          <section className={styles.results} aria-live="polite">
            {loading && <Box sx={{ color: "#888" }}>Шукаємо…</Box>}
            {error && <Box sx={{ color: "#ff6b6b" }}>{error}</Box>}
            {!loading && result && (
              <>
                <Box sx={{ fontSize: 14, color: "#777" }}>Знайдено: {result.total}</Box>
                {result.items.length === 0 && (
                  <Box sx={{ color: "#888", py: "24px" }}>
                    Нічого не знайдено. Спробуйте інше слово{hasFilters ? " або скиньте фільтри" : ""}.
                  </Box>
                )}
                {result.items.map(hit => <SearchHitCard key={hit.id} hit={hit} onTagClick={addTag} />)}
                {result.nextCursor && (
                  <Button onClick={loadMore} disabled={loadingMore}
                    sx={{ alignSelf: "center", color: "#a48fff", textTransform: "none", fontSize: 15 }}>
                    {loadingMore ? "Завантаження…" : "Показати ще"}
                  </Button>
                )}
              </>
            )}
          </section>

          <aside className={styles.filters} aria-label="Фільтри">
            <FilterBlock title="Категорії">
              <div className={styles.chips}>
                {(result?.facets.categories ?? []).map(c => (
                  <Pill key={c.value} active={params.category === c.value}
                    onClick={() => update({ category: params.category === c.value ? undefined : c.value })}>
                    {c.value} <Box component="span" sx={{ color: "#777" }}>{c.count}</Box>
                  </Pill>
                ))}
                {params.category && !(result?.facets.categories ?? []).some(c => c.value === params.category) && (
                  <Pill active onClick={() => update({ category: undefined })}>{params.category} ×</Pill>
                )}
              </div>
            </FilterBlock>

            <FilterBlock title="Теги">
              {tags.length > 0 && (
                <Box sx={{ display: "flex", flexWrap: "wrap", gap: "6px", mb: "8px" }}>
                  {tags.map(t => <Pill key={t} active onClick={() => removeTag(t)} title="Прибрати">#{t} ×</Pill>)}
                  {tags.length > 1 && (
                    <Pill onClick={() => update({ tagMode: params.tagMode === "all" ? undefined : "all" })}
                      title="Чи мають бути всі теги одразу">
                      {params.tagMode === "all" ? "усі разом" : "будь-який"}
                    </Pill>
                  )}
                </Box>
              )}
              <TagPicker onPick={addTag} />
              <Box sx={{ display: "flex", flexWrap: "wrap", gap: "6px", mt: "8px" }}>
                {(result?.facets.tags ?? []).filter(t => !tags.includes(t.key)).slice(0, 10).map(t => (
                  <Pill key={t.key} onClick={() => addTag(t.key)}>
                    #{t.label} <Box component="span" sx={{ color: "#777" }}>{t.count}</Box>
                  </Pill>
                ))}
              </Box>
            </FilterBlock>

            {params.kind !== "post" && (
              <FilterBlock title="Роль або спеціалізація">
                <RolePicker value={params.role ?? ""} onPick={role => update({ role: role || undefined })} />
                {(params.kind === undefined || params.kind === "team") && (
                  <FormControlLabel sx={{ mt: "6px", color: "#aaa", "& .MuiTypography-root": { fontSize: 14 } }}
                    control={<Checkbox size="small" checked={!!params.hasOpenVacancies} sx={{ color: "#555" }}
                      onChange={e => update({ hasOpenVacancies: e.target.checked || undefined })} />}
                    label="Лише команди з відкритими вакансіями" />
                )}
              </FilterBlock>
            )}

            <FilterBlock title="Дата створення">
              <Box sx={{ display: "flex", gap: "8px" }}>
                <TextField type="date" size="small" label="Від" sx={darkField} slotProps={{ inputLabel: { shrink: true } }}
                  value={params.from?.slice(0, 10) ?? ""} onChange={e => update({ from: e.target.value || undefined })} />
                <TextField type="date" size="small" label="До" sx={darkField} slotProps={{ inputLabel: { shrink: true } }}
                  value={params.to?.slice(0, 10) ?? ""} onChange={e => update({ to: e.target.value || undefined })} />
              </Box>
            </FilterBlock>

            {(params.kind === undefined || params.kind === "post") && (
              <FilterBlock title="Щонайменше вподобань">
                <TextField type="number" size="small" sx={darkField} slotProps={{ htmlInput: { min: 0 } }}
                  value={params.minLikes ?? ""} onChange={e => update({ minLikes: Number(e.target.value) || undefined })} />
              </FilterBlock>
            )}

            {hasFilters && (
              <Button onClick={() => apply({ q: params.q, sort: params.sort })}
                sx={{ color: "#a48fff", textTransform: "none", alignSelf: "flex-start" }}>
                Скинути фільтри
              </Button>
            )}
          </aside>
        </div>
      </main>

      <SaveSearchDialog open={saveOpen} params={params} onClose={() => setSaveOpen(false)}
        onSaved={() => { setSaveOpen(false); navigate("/search/saved"); }} />
    </Box>
  );
};

const FilterBlock: FunctionComponent<{ title: string; children: ReactNode }> = ({ title, children }) => (
  <Box>
    <div className={styles.filterTitle}>{title}</div>
    {children}
  </Box>
);

/** Tag autocomplete by any spelling or synonym ("ілюс", "ilustr", "3-д"). */
const TagPicker: FunctionComponent<{ onPick: (key: string) => void }> = ({ onPick }) => {
  const [input, setInput] = useState("");
  const [options, setOptions] = useState<TagSuggestion[]>([]);
  useEffect(() => {
    const timer = setTimeout(() => searchApi.tags(input, 8).then(setOptions).catch(() => setOptions([])), 200);
    return () => clearTimeout(timer);
  }, [input]);
  return (
    <Autocomplete<TagSuggestion>
      size="small" options={options} filterOptions={x => x} value={null} inputValue={input}
      getOptionLabel={o => o.label} isOptionEqualToValue={(a, b) => a.key === b.key}
      onInputChange={(_, v) => setInput(v)}
      onChange={(_, v) => { if (v) { onPick(v.key); setInput(""); } }}
      renderOption={(props, o) => (
        <li {...props} key={o.key}>#{o.label}&nbsp;<Box component="span" sx={{ color: "#888" }}>{o.documents}</Box></li>
      )}
      renderInput={p => <TextField {...p} placeholder="Додати тег" sx={darkField} />}
      noOptionsText="Немає такого тегу"
    />
  );
};

/** Role autocomplete across languages and keyboard layouts; free text is allowed too. */
const RolePicker: FunctionComponent<{ value: string; onPick: (role: string) => void }> = ({ value, onPick }) => {
  const [input, setInput] = useState(value);
  const [options, setOptions] = useState<RoleSuggestion[]>([]);
  useEffect(() => setInput(value), [value]);
  useEffect(() => {
    const timer = setTimeout(() => searchApi.roles(input, 8).then(setOptions).catch(() => setOptions([])), 200);
    return () => clearTimeout(timer);
  }, [input]);
  return (
    <Autocomplete<RoleSuggestion, false, false, true>
      freeSolo size="small" options={options} filterOptions={x => x} inputValue={input}
      getOptionLabel={o => (typeof o === "string" ? o : o.label)}
      onInputChange={(_, v, reason) => { setInput(v); if (reason === "clear") onPick(""); }}
      onChange={(_, v) => onPick(typeof v === "string" ? v.trim() : v?.label ?? "")}
      renderOption={(props, o) => (
        <li {...props} key={o.label}>
          {o.label}&nbsp;
          <Box component="span" sx={{ color: "#888", fontSize: 12 }}>
            {[o.vacancies && `вакансій ${o.vacancies}`, o.teams && `команд ${o.teams}`, o.people && `людей ${o.people}`].filter(Boolean).join(", ")}
          </Box>
        </li>
      )}
      renderInput={p => (
        <TextField {...p} placeholder="ілюстратор, motion designer…" sx={darkField}
          onKeyDown={e => { if (e.key === "Enter") onPick(input.trim()); }} />
      )}
    />
  );
};

const SaveSearchDialog: FunctionComponent<{ open: boolean; params: SearchParams; onClose: () => void; onSaved: () => void }> = ({
  open, params, onClose, onSaved,
}) => {
  const [name, setName] = useState("");
  const [notify, setNotify] = useState(true);
  const [error, setError] = useState<string | null>(null);
  useEffect(() => {
    if (open) { setName(params.q || params.role || params.category || "Мій пошук"); setError(null); }
  }, [open, params]);

  const save = async () => {
    try {
      await searchApi.createSaved({ name: name.trim(), query: params.q, filters: filtersOf(params), notify });
      onSaved();
    } catch (e) {
      const message = e instanceof Error ? e.message : "";
      setError(message.includes("409") ? "Пошук з такою назвою вже є або їх забагато" : "Не вдалося зберегти");
    }
  };

  return (
    <Dialog open={open} onClose={onClose} slotProps={{ paper: { sx: { background: "#1b1b1b", color: "#ddd", borderRadius: "20px" } } }}>
      <DialogTitle>Зберегти пошук</DialogTitle>
      <DialogContent sx={{ display: "flex", flexDirection: "column", gap: "12px", minWidth: { sm: 400 } }}>
        <TextField autoFocus label="Назва" value={name} onChange={e => setName(e.target.value)} sx={{ ...darkField, mt: "6px" }}
          slotProps={{ htmlInput: { maxLength: 100 } }} />
        <FormControlLabel sx={{ color: "#aaa" }}
          control={<Checkbox checked={notify} onChange={e => setNotify(e.target.checked)} sx={{ color: "#555" }} />}
          label="Повідомляти про нові результати" />
        {error && <Box sx={{ color: "#ff6b6b", fontSize: 14 }}>{error}</Box>}
      </DialogContent>
      <DialogActions>
        <Button onClick={onClose} sx={{ color: "#aaa", textTransform: "none" }}>Скасувати</Button>
        <Button onClick={save} disabled={!name.trim()} variant="contained"
          sx={{ textTransform: "none", background: "#7c5cfc", borderRadius: "30px" }}>
          Зберегти
        </Button>
      </DialogActions>
    </Dialog>
  );
};

export default SearchPage;
