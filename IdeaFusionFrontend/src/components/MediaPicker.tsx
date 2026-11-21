import { useEffect, useRef, useState, type DragEvent, type ReactNode } from "react";
import { cx } from "../ui";
import { IconAudio, IconClose, IconFile, IconPlus, IconUpload, IconVideo } from "../ui/icons";
import { formatBytes } from "../lib/media";
import s from "./MediaPicker.module.css";

interface Props {
  files: File[];
  onAdd: (files: File[]) => void;
  onRemove: (index: number) => void;
  accept: string;
  hint: string;
  /** Small overlay on a file's tile (e.g. the image check result). */
  renderBadge?: (file: File) => ReactNode;
  label?: string;
}

function FileIcon({ file }: { file: File }) {
  if (file.type.startsWith("video/")) return <IconVideo size={22} />;
  if (file.type.startsWith("audio/")) return <IconAudio size={22} />;
  return <IconFile size={22} />;
}

/** Drop zone when empty; afterwards a grid of the chosen files with an "add more" tile. */
export default function MediaPicker({ files, onAdd, onRemove, accept, hint, renderBadge, label = "Оберіть файли" }: Props) {
  const input = useRef<HTMLInputElement>(null);
  const [drag, setDrag] = useState(false);
  const [previews, setPreviews] = useState<Map<File, string>>(new Map());

  useEffect(() => {
    const next = new Map<File, string>();
    for (const f of files) {
      if (f.type.startsWith("image/")) next.set(f, previews.get(f) ?? URL.createObjectURL(f));
    }
    for (const [f, url] of previews) if (!next.has(f)) URL.revokeObjectURL(url);
    setPreviews(next);
    // previews is derived state; recomputing on files only is intended.
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [files]);

  const dragProps = {
    onDragOver: (e: DragEvent) => {
      e.preventDefault();
      setDrag(true);
    },
    onDragLeave: () => setDrag(false),
    onDrop: (e: DragEvent) => {
      e.preventDefault();
      setDrag(false);
      const dropped = Array.from(e.dataTransfer.files);
      if (dropped.length) onAdd(dropped);
    },
  };

  const picker = (
    <input
      ref={input}
      type="file"
      multiple
      accept={accept}
      className="visually-hidden"
      tabIndex={-1}
      onChange={e => {
        if (e.target.files?.length) onAdd(Array.from(e.target.files));
        e.target.value = "";
      }}
    />
  );

  if (files.length === 0) {
    return (
      <div className={cx(s.zone, drag && s.zoneDrag)} {...dragProps}>
        <span className={s.zoneIcon}>
          <IconUpload size={22} />
        </span>
        <button type="button" className={s.pick} onClick={() => input.current?.click()}>
          {label}
        </button>
        <span className={s.or}>або перетягніть сюди</span>
        <span className={s.hint}>{hint}</span>
        {picker}
      </div>
    );
  }

  return (
    <ul className={cx(s.grid, drag && s.gridDrag)} {...dragProps}>
      {files.map((f, i) => (
        <li key={`${f.name}-${f.size}-${i}`} className={cx(s.tile, i === 0 && s.tileFirst)}>
          {previews.get(f) ? (
            <img src={previews.get(f)} alt={f.name} className={s.thumb} />
          ) : (
            <span className={s.fileTile}>
              <FileIcon file={f} />
              <span className={s.fileName}>{f.name}</span>
              <span className={s.fileSize}>{formatBytes(f.size)}</span>
            </span>
          )}
          <span className={s.index}>{i + 1}</span>
          {renderBadge && <span className={s.badge}>{renderBadge(f)}</span>}
          <button type="button" className={s.remove} onClick={() => onRemove(i)} aria-label={`Прибрати ${f.name}`}>
            <IconClose size={15} />
          </button>
        </li>
      ))}
      <li className={s.tile}>
        <button type="button" className={s.add} onClick={() => input.current?.click()}>
          <IconPlus size={20} />
          <span>Додати ще</span>
        </button>
        {picker}
      </li>
    </ul>
  );
}
