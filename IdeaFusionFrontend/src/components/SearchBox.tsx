import { FunctionComponent, useEffect, useRef, useState } from "react";
import { Box, ClickAwayListener, InputBase, Paper } from "@mui/material";
import { useNavigate } from "react-router-dom";
import { KIND_LABEL, hitLink, searchApi } from "../api/search";
import type { HistoryEntry, SuggestResponse } from "../api/search";

export type SearchBoxProps = {
  value: string;
  onChange: (value: string) => void;
  /** Runs the search for the given text. */
  onSubmit: (value: string) => void;
  /** A tag from the suggestions was picked: add it as a filter. */
  onPickTag?: (key: string) => void;
  /** A history entry was picked: restore its query and filters. */
  onPickHistory?: (entry: HistoryEntry) => void;
  placeholder?: string;
};

const SUGGEST_DELAY_MS = 200;

const rowSx = {
  px: "18px", py: "9px", cursor: "pointer", display: "flex", alignItems: "center", gap: "10px",
  color: "#ddd", fontSize: 15, fontFamily: "var(--font-inter)",
  "&:hover, &.active": { background: "#232323" },
} as const;

const groupSx = { px: "18px", pt: "10px", pb: "4px", fontSize: 11, color: "#666", textTransform: "uppercase", letterSpacing: ".06em" } as const;

/**
 * The search field: autocomplete of words, tags and documents while typing, and the
 * user's recent searches when the field is empty or starts like one of them.
 */
const SearchBox: FunctionComponent<SearchBoxProps> = ({
  value, onChange, onSubmit, onPickTag, onPickHistory, placeholder = "Пошук постів, команд, вакансій, людей…",
}) => {
  const navigate = useNavigate();
  const [open, setOpen] = useState(false);
  const [suggest, setSuggest] = useState<SuggestResponse | null>(null);
  const [history, setHistory] = useState<HistoryEntry[]>([]);
  const seq = useRef(0);

  useEffect(() => {
    if (!open) return;
    const id = ++seq.current;
    const timer = setTimeout(() => {
      const text = value.trim();
      searchApi.history(text, text ? 3 : 8).then(h => { if (id === seq.current) setHistory(h); }).catch(() => setHistory([]));
      if (!text) { setSuggest(null); return; }
      searchApi.suggest(text, 6).then(s => { if (id === seq.current) setSuggest(s); }).catch(() => setSuggest(null));
    }, SUGGEST_DELAY_MS);
    return () => clearTimeout(timer);
  }, [value, open]);

  const submit = (text: string) => {
    setOpen(false);
    onSubmit(text);
  };

  const removeHistory = async (entry: HistoryEntry) => {
    await searchApi.deleteHistory(entry.id).catch(() => undefined);
    setHistory(h => h.filter(x => x.id !== entry.id));
  };

  const clearHistory = async () => {
    await searchApi.clearHistory().catch(() => undefined);
    setHistory([]);
  };

  const hasSuggestions = !!suggest && (suggest.completions.length + suggest.tags.length + suggest.documents.length) > 0;
  const showPanel = open && (history.length > 0 || hasSuggestions);

  return (
    <ClickAwayListener onClickAway={() => setOpen(false)}>
      <Box sx={{ position: "relative", width: "100%" }}>
        <Box component="form" role="search"
          onSubmit={e => { e.preventDefault(); submit(value); }}
          sx={{ display: "flex", alignItems: "center", gap: "14px", height: 64, px: "24px",
            background: "#161616", border: "1px solid #6d6c6c", borderRadius: "40px",
            "&:focus-within": { borderColor: "#7c5cfc" } }}>
          <svg width="22" height="22" viewBox="0 0 24 24" fill="none" aria-hidden="true">
            <circle cx="11" cy="11" r="7" stroke="#6d6c6c" strokeWidth="1.8" />
            <path d="M16.5 16.5L21 21" stroke="#6d6c6c" strokeWidth="1.8" strokeLinecap="round" />
          </svg>
          <InputBase
            value={value}
            placeholder={placeholder}
            onChange={e => { onChange(e.target.value); setOpen(true); }}
            onFocus={() => setOpen(true)}
            onKeyDown={e => { if (e.key === "Escape") setOpen(false); }}
            inputProps={{ "aria-label": "Пошук", maxLength: 200 }}
            sx={{ flex: 1, color: "#eee", fontSize: 20, fontFamily: "var(--font-inter)" }}
          />
          {value && (
            <Box component="button" type="button" aria-label="Очистити"
              onClick={() => { onChange(""); setOpen(true); }}
              sx={{ background: "none", border: "none", color: "#6d6c6c", fontSize: 22, cursor: "pointer" }}>
              ×
            </Box>
          )}
        </Box>

        {showPanel && (
          <Paper elevation={8} sx={{ position: "absolute", top: 70, left: 0, right: 0, zIndex: 20,
            background: "#1b1b1b", border: "1px solid #2a2a2a", borderRadius: "20px", overflow: "hidden", pb: "8px" }}>
            {history.length > 0 && (
              <>
                <Box sx={{ ...groupSx, display: "flex", justifyContent: "space-between" }}>
                  <span>Нещодавні пошуки</span>
                  {!value.trim() && (
                    <Box component="span" onClick={clearHistory} sx={{ cursor: "pointer", color: "#a48fff", textTransform: "none" }}>
                      Очистити
                    </Box>
                  )}
                </Box>
                {history.map(h => (
                  <Box key={h.id} sx={rowSx} onClick={() => { setOpen(false); onPickHistory?.(h); }}>
                    <Box component="span" sx={{ color: "#666" }}>↺</Box>
                    <Box sx={{ flex: 1, overflow: "hidden", textOverflow: "ellipsis", whiteSpace: "nowrap" }}>
                      {h.query || "Без запиту"}
                      {describeFilters(h).length > 0 && (
                        <Box component="span" sx={{ color: "#777", fontSize: 13 }}> · {describeFilters(h).join(", ")}</Box>
                      )}
                    </Box>
                    <Box component="span" sx={{ color: "#555", fontSize: 12 }}>{h.resultsCount}</Box>
                    <Box component="button" type="button" aria-label="Видалити з історії"
                      onClick={e => { e.stopPropagation(); removeHistory(h); }}
                      sx={{ background: "none", border: "none", color: "#666", cursor: "pointer", fontSize: 16 }}>
                      ×
                    </Box>
                  </Box>
                ))}
              </>
            )}

            {suggest && suggest.completions.length > 0 && (
              <>
                <Box sx={groupSx}>Запити</Box>
                {suggest.completions.map(c => (
                  <Box key={c} sx={rowSx} onClick={() => { onChange(c); submit(c); }}>
                    <Box component="span" sx={{ color: "#666" }}>⌕</Box>{c}
                  </Box>
                ))}
              </>
            )}

            {suggest && suggest.tags.length > 0 && onPickTag && (
              <>
                <Box sx={groupSx}>Теги</Box>
                <Box sx={{ display: "flex", flexWrap: "wrap", gap: "8px", px: "18px", py: "6px" }}>
                  {suggest.tags.map(t => (
                    <Box key={t.key} onClick={() => { setOpen(false); onPickTag(t.key); }}
                      sx={{ cursor: "pointer", px: "12px", py: "4px", borderRadius: "30px", fontSize: 13,
                        border: "1px solid #7c5cfc", color: "#a48fff", background: "rgba(124,92,252,.12)" }}>
                      #{t.label} <Box component="span" sx={{ color: "#777" }}>{t.documents}</Box>
                    </Box>
                  ))}
                </Box>
              </>
            )}

            {suggest && suggest.documents.length > 0 && (
              <>
                <Box sx={groupSx}>Знайдено</Box>
                {suggest.documents.map(d => (
                  <Box key={d.id} sx={rowSx} onClick={() => { setOpen(false); navigate(hitLink(d)); }}>
                    <Box component="span" sx={{ fontSize: 11, color: "#7c5cfc", minWidth: 64 }}>{KIND_LABEL[d.kind]}</Box>
                    <Box sx={{ flex: 1, overflow: "hidden", textOverflow: "ellipsis", whiteSpace: "nowrap" }}>{d.title}</Box>
                  </Box>
                ))}
              </>
            )}
          </Paper>
        )}
      </Box>
    </ClickAwayListener>
  );
};

function describeFilters(h: HistoryEntry): string[] {
  const f = h.filters;
  const parts: string[] = [];
  if (f.kind) parts.push(KIND_LABEL[f.kind]);
  if (f.category) parts.push(f.category);
  if (f.tags?.length) parts.push(f.tags.map(t => `#${t}`).join(" "));
  if (f.role) parts.push(`роль: ${f.role}`);
  if (f.hasOpenVacancies) parts.push("з вакансіями");
  return parts;
}

export default SearchBox;
