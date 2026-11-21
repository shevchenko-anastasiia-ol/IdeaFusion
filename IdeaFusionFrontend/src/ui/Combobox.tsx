import { useEffect, useId, useRef, useState, type ReactNode } from "react";
import s from "./Combobox.module.css";

/**
 * Text input with a suggestion list (WAI-ARIA combobox pattern).
 * Suggestions are supplied by the caller, typically fetched as the text changes.
 * With `onEnter`, Enter on free text (no highlighted option) is accepted too.
 */
export function Combobox<T>({
  inputValue,
  onInputChange,
  options,
  getKey,
  renderOption,
  onSelect,
  onEnter,
  placeholder,
  ariaLabel,
  emptyText,
  id,
}: {
  inputValue: string;
  onInputChange: (v: string) => void;
  options: T[];
  getKey: (o: T) => string;
  renderOption: (o: T) => ReactNode;
  onSelect: (o: T) => void;
  onEnter?: (text: string) => void;
  placeholder?: string;
  ariaLabel?: string;
  emptyText?: string;
  id?: string;
}) {
  const [open, setOpen] = useState(false);
  const [active, setActive] = useState(-1);
  const wrap = useRef<HTMLDivElement>(null);
  const listId = useId();

  useEffect(() => setActive(-1), [options]);

  useEffect(() => {
    if (!open) return;
    function onDoc(e: MouseEvent) {
      if (wrap.current && !wrap.current.contains(e.target as Node)) setOpen(false);
    }
    document.addEventListener("mousedown", onDoc);
    return () => document.removeEventListener("mousedown", onDoc);
  }, [open]);

  function choose(o: T) {
    onSelect(o);
    setOpen(false);
  }

  const showList = open && (options.length > 0 || (!!emptyText && inputValue.trim().length > 0));

  return (
    <div className={s.wrap} ref={wrap}>
      <input
        id={id}
        className={s.input}
        value={inputValue}
        placeholder={placeholder}
        aria-label={ariaLabel}
        role="combobox"
        aria-expanded={showList}
        aria-controls={listId}
        aria-autocomplete="list"
        aria-activedescendant={active >= 0 ? `${listId}-${active}` : undefined}
        onFocus={() => setOpen(true)}
        onChange={e => {
          onInputChange(e.target.value);
          setOpen(true);
        }}
        onKeyDown={e => {
          if (e.key === "ArrowDown") {
            e.preventDefault();
            setOpen(true);
            setActive(i => Math.min(options.length - 1, i + 1));
          } else if (e.key === "ArrowUp") {
            e.preventDefault();
            setActive(i => Math.max(-1, i - 1));
          } else if (e.key === "Enter") {
            if (open && active >= 0 && options[active]) {
              e.preventDefault();
              choose(options[active]);
            } else if (onEnter) {
              e.preventDefault();
              onEnter(inputValue);
              setOpen(false);
            }
          } else if (e.key === "Escape") {
            setOpen(false);
          }
        }}
      />
      {showList && (
        <ul className={s.list} id={listId} role="listbox">
          {options.length === 0 && <li className={s.empty}>{emptyText}</li>}
          {options.map((o, i) => (
            <li
              key={getKey(o)}
              id={`${listId}-${i}`}
              role="option"
              aria-selected={i === active}
              className={i === active ? `${s.option} ${s.active}` : s.option}
              onMouseDown={e => e.preventDefault()}
              onMouseEnter={() => setActive(i)}
              onClick={() => choose(o)}
            >
              {renderOption(o)}
            </li>
          ))}
        </ul>
      )}
    </div>
  );
}
