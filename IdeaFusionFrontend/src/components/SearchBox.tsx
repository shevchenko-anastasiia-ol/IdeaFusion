import { useEffect, useRef, useState, type KeyboardEvent } from "react";
import { useNavigate } from "react-router-dom";
import { KIND_LABEL, hitLink, searchApi } from "../api/search";
import type { HistoryEntry, SuggestResponse } from "../api/search";
import { IconButton } from "../ui";
import { IconClose, IconHistory, IconSearch } from "../ui/icons";
import s from "./SearchBox.module.css";

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
  autoFocus?: boolean;
  /** Changes every time focus is asked for again (Ctrl+K while already on the page). */
  focusToken?: unknown;
};

const SUGGEST_DELAY_MS = 200;

type Row =
  | { kind: "history"; entry: HistoryEntry }
  | { kind: "completion"; text: string }
  | { kind: "doc"; doc: SuggestResponse["documents"][number] };

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

/**
 * The search field: autocomplete of words, tags and documents while typing, and the
 * user's recent searches when the field is empty or starts like one of them.
 */
export default function SearchBox({
  value,
  onChange,
  onSubmit,
  onPickTag,
  onPickHistory,
  placeholder = "Пошук постів, команд, вакансій, людей…",
  autoFocus,
  focusToken,
}: SearchBoxProps) {
  const navigate = useNavigate();
  const [open, setOpen] = useState(false);
  const [suggest, setSuggest] = useState<SuggestResponse | null>(null);
  const [history, setHistory] = useState<HistoryEntry[]>([]);
  const [active, setActive] = useState(-1);
  const seq = useRef(0);
  const wrap = useRef<HTMLDivElement>(null);
  const input = useRef<HTMLInputElement>(null);

  useEffect(() => {
    if (autoFocus) {
      input.current?.focus();
      input.current?.select();
    }
  }, [autoFocus, focusToken]);

  useEffect(() => {
    if (!open) return;
    const id = ++seq.current;
    const timer = setTimeout(() => {
      const text = value.trim();
      searchApi
        .history(text, text ? 3 : 8)
        .then(h => {
          if (id === seq.current) setHistory(h);
        })
        .catch(() => setHistory([]));
      if (!text) {
        setSuggest(null);
        return;
      }
      searchApi
        .suggest(text, 6)
        .then(r => {
          if (id === seq.current) setSuggest(r);
        })
        .catch(() => setSuggest(null));
    }, SUGGEST_DELAY_MS);
    return () => clearTimeout(timer);
  }, [value, open]);

  useEffect(() => {
    if (!open) return;
    function onDoc(e: MouseEvent) {
      if (wrap.current && !wrap.current.contains(e.target as Node)) setOpen(false);
    }
    document.addEventListener("mousedown", onDoc);
    return () => document.removeEventListener("mousedown", onDoc);
  }, [open]);

  useEffect(() => setActive(-1), [suggest, history]);

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

  const rows: Row[] = [
    ...history.map(entry => ({ kind: "history" as const, entry })),
    ...(suggest?.completions ?? []).map(text => ({ kind: "completion" as const, text })),
    ...(suggest?.documents ?? []).map(doc => ({ kind: "doc" as const, doc })),
  ];
  const tags = onPickTag ? suggest?.tags ?? [] : [];
  const showPanel = open && (rows.length > 0 || tags.length > 0);

  function activate(row: Row) {
    setOpen(false);
    if (row.kind === "history") onPickHistory?.(row.entry);
    else if (row.kind === "completion") {
      onChange(row.text);
      onSubmit(row.text);
    } else navigate(hitLink(row.doc));
  }

  function onKey(e: KeyboardEvent<HTMLInputElement>) {
    if (e.key === "Escape") setOpen(false);
    else if (e.key === "ArrowDown" && rows.length) {
      e.preventDefault();
      setOpen(true);
      setActive(i => Math.min(rows.length - 1, i + 1));
    } else if (e.key === "ArrowUp" && rows.length) {
      e.preventDefault();
      setActive(i => Math.max(-1, i - 1));
    } else if (e.key === "Enter" && open && active >= 0 && rows[active]) {
      e.preventDefault();
      activate(rows[active]);
    }
  }

  let idx = -1;
  const rowProps = (row: Row) => {
    idx += 1;
    const i = idx;
    return {
      id: `sb-row-${i}`,
      role: "option" as const,
      "aria-selected": active === i,
      className: active === i ? `${s.row} ${s.rowActive}` : s.row,
      onMouseEnter: () => setActive(i),
      onMouseDown: (e: React.MouseEvent) => e.preventDefault(),
      onClick: () => activate(row),
    };
  };

  return (
    <div className={s.wrap} ref={wrap}>
      <form
        role="search"
        className={s.form}
        onSubmit={e => {
          e.preventDefault();
          submit(value);
        }}
      >
        <IconSearch size={20} className={s.icon} />
        <input
          ref={input}
          className={s.input}
          value={value}
          placeholder={placeholder}
          aria-label="Пошук"
          maxLength={200}
          role="combobox"
          aria-expanded={showPanel}
          aria-controls="search-suggestions"
          aria-activedescendant={active >= 0 ? `sb-row-${active}` : undefined}
          onChange={e => {
            onChange(e.target.value);
            setOpen(true);
          }}
          onFocus={() => setOpen(true)}
          onKeyDown={onKey}
        />
        {value && (
          <IconButton
            label="Очистити"
            size="sm"
            onClick={() => {
              onChange("");
              setOpen(true);
              input.current?.focus();
            }}
          >
            <IconClose size={16} />
          </IconButton>
        )}
        <kbd className={s.kbd}>Enter</kbd>
      </form>

      {showPanel && (
        <div className={s.panel} id="search-suggestions" role="listbox">
          {history.length > 0 && (
            <div className={s.group}>
              <div className={s.groupHead}>
                <span>нещодавні пошуки</span>
                {!value.trim() && (
                  <button type="button" className={s.clear} onMouseDown={e => e.preventDefault()} onClick={() => void clearHistory()}>
                    очистити
                  </button>
                )}
              </div>
              {history.map(h => {
                const extra = describeFilters(h);
                return (
                  <div key={h.id} {...rowProps({ kind: "history", entry: h })}>
                    <IconHistory size={15} className={s.rowIcon} />
                    <span className={s.rowText}>
                      {h.query || "без запиту"}
                      {extra.length > 0 && <span className={s.rowExtra}> · {extra.join(", ")}</span>}
                    </span>
                    <span className={s.rowCount}>{h.resultsCount}</span>
                    <button
                      type="button"
                      className={s.rowRemove}
                      aria-label="Видалити з історії"
                      onMouseDown={e => e.preventDefault()}
                      onClick={e => {
                        e.stopPropagation();
                        void removeHistory(h);
                      }}
                    >
                      <IconClose size={14} />
                    </button>
                  </div>
                );
              })}
            </div>
          )}
          {suggest && suggest.completions.length > 0 && (
            <div className={s.group}>
              <div className={s.groupHead}>
                <span>запити</span>
              </div>
              {suggest.completions.map(c => (
                <div key={c} {...rowProps({ kind: "completion", text: c })}>
                  <IconSearch size={15} className={s.rowIcon} />
                  <span className={s.rowText}>{c}</span>
                </div>
              ))}
            </div>
          )}
          {tags.length > 0 && (
            <div className={s.group}>
              <div className={s.groupHead}>
                <span>теги</span>
              </div>
              <div className={s.tags}>
                {tags.map(t => (
                  <button
                    key={t.key}
                    type="button"
                    className={s.tag}
                    onMouseDown={e => e.preventDefault()}
                    onClick={() => {
                      setOpen(false);
                      onPickTag?.(t.key);
                    }}
                  >
                    #{t.label} <span>{t.documents}</span>
                  </button>
                ))}
              </div>
            </div>
          )}
          {suggest && suggest.documents.length > 0 && (
            <div className={s.group}>
              <div className={s.groupHead}>
                <span>знайдено</span>
              </div>
              {suggest.documents.map(d => (
                <div key={d.id} {...rowProps({ kind: "doc", doc: d })}>
                  <span className={s.kind} data-kind={d.kind}>
                    {KIND_LABEL[d.kind]}
                  </span>
                  <span className={s.rowText}>{d.title}</span>
                </div>
              ))}
            </div>
          )}
        </div>
      )}
    </div>
  );
}
