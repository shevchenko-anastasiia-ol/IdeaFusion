import type { TagDto } from "../api/types";
import { Skeleton, Tag } from "../ui";
import { tagLabel } from "../lib/tags";
import s from "./TagPicker.module.css";

/** Toggle list of the platform's tags. */
export default function TagPicker({
  tags,
  selected,
  onChange,
  loading,
}: {
  tags: TagDto[];
  selected: number[];
  onChange: (ids: number[]) => void;
  loading?: boolean;
}) {
  if (loading) {
    return (
      <div className={s.wrap} aria-hidden="true">
        {[60, 80, 50, 70, 90].map((w, i) => (
          <Skeleton key={i} width={w} height={22} />
        ))}
      </div>
    );
  }
  if (tags.length === 0) return <p className={s.empty}>Тегів поки немає.</p>;
  return (
    <div className={s.wrap} role="group" aria-label="Теги">
      {tags.map(tag => {
        const on = selected.includes(tag.tagId);
        return (
          <Tag key={tag.tagId} active={on} onClick={() => onChange(on ? selected.filter(id => id !== tag.tagId) : [...selected, tag.tagId])} title={tagLabel(tag.name)}>
            #{tag.name.toLowerCase()}
          </Tag>
        );
      })}
    </div>
  );
}
