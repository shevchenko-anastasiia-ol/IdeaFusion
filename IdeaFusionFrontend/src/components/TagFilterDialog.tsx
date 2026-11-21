import { useEffect, useMemo, useState } from "react";
import { tagsApi } from "../api/posts";
import { Button, Dialog, Input, Loading, Tag } from "../ui";
import { IconSearch } from "../ui/icons";
import s from "./TagFilterDialog.module.css";

/**
 * Every tag on the platform in one window, with how many loaded works carry it.
 * The user ticks any number of them; the feed then shows works that have at least one.
 */
export default function TagFilterDialog({
  open,
  onClose,
  selected,
  onApply,
  postTags,
}: {
  open: boolean;
  onClose: () => void;
  /** Lower-cased tag names currently applied to the feed. */
  selected: string[];
  onApply: (tags: string[]) => void;
  /** Tags of every loaded post, used for the counts and for tags the catalogue lacks. */
  postTags: string[][];
}) {
  const [all, setAll] = useState<string[] | null>(null);
  const [failed, setFailed] = useState(false);
  const [query, setQuery] = useState("");
  const [draft, setDraft] = useState<string[]>(selected);

  // The catalogue is fetched once, the first time the window opens.
  useEffect(() => {
    if (!open || all) return;
    tagsApi
      .getAll()
      .then(list => setAll(list.map(t => t.name)))
      .catch(() => {
        setFailed(true);
        setAll([]);
      });
  }, [open, all]);

  // Each opening starts from what is applied now.
  useEffect(() => {
    if (open) {
      setDraft(selected);
      setQuery("");
    }
  }, [open, selected]);

  const counts = useMemo(() => {
    const map = new Map<string, number>();
    for (const tags of postTags)
      for (const t of new Set(tags.map(x => x.toLowerCase()))) map.set(t, (map.get(t) ?? 0) + 1);
    return map;
  }, [postTags]);

  /** Catalogue plus any tag only seen on posts, de-duplicated, most used first. */
  const tags = useMemo(() => {
    const byKey = new Map<string, string>();
    for (const name of all ?? []) byKey.set(name.toLowerCase(), name);
    for (const tags of postTags) for (const t of tags) if (!byKey.has(t.toLowerCase())) byKey.set(t.toLowerCase(), t);
    return [...byKey.entries()]
      .map(([key, name]) => ({ key, name, count: counts.get(key) ?? 0 }))
      .sort((a, b) => b.count - a.count || a.name.localeCompare(b.name, "uk"));
  }, [all, postTags, counts]);

  const q = query.trim().toLowerCase();
  const visible = q ? tags.filter(t => t.key.includes(q)) : tags;
  const matching = useMemo(() => {
    if (draft.length === 0) return postTags.length;
    const wanted = new Set(draft);
    return postTags.filter(tags => tags.some(t => wanted.has(t.toLowerCase()))).length;
  }, [draft, postTags]);

  const toggle = (key: string) => setDraft(d => (d.includes(key) ? d.filter(x => x !== key) : [...d, key]));

  return (
    <Dialog
      open={open}
      onClose={onClose}
      wide
      title="Фільтр за тегами"
      footer={
        <>
          <Button variant="ghost" disabled={draft.length === 0} onClick={() => setDraft([])}>
            Скинути
          </Button>
          <Button
            variant="primary"
            onClick={() => {
              onApply(draft);
              onClose();
            }}
          >
            Показати {matching} {matching === 1 ? "роботу" : matching >= 2 && matching <= 4 ? "роботи" : "робіт"}
          </Button>
        </>
      }
    >
      <div className={s.search}>
        <IconSearch size={16} />
        <Input data-autofocus placeholder="Знайти тег…" value={query} onChange={e => setQuery(e.target.value)} aria-label="Пошук тегу" />
      </div>
      <p className={s.hint}>Показуються роботи, що мають хоча б один із вибраних тегів.{draft.length > 0 && ` Вибрано: ${draft.length}.`}</p>
      {all === null ? (
        <Loading label="Завантаження тегів…" />
      ) : visible.length === 0 ? (
        <p className={s.empty}>{failed ? "Не вдалося завантажити теги." : q ? `Тегу «${query.trim()}» немає.` : "Тегів поки немає."}</p>
      ) : (
        <div className={s.cloud} role="group" aria-label="Теги">
          {visible.map(t => (
            <Tag key={t.key} active={draft.includes(t.key)} onClick={() => toggle(t.key)} title={`${t.count} у стрічці`}>
              #{t.name}
              <span className={t.count ? s.count : s.countZero}>{t.count}</span>
            </Tag>
          ))}
        </div>
      )}
    </Dialog>
  );
}
