import { useState } from "react";
import { cx } from "../ui";
import { IconAudio, IconChevronLeft, IconChevronRight, IconExternal, IconFile, IconImage } from "../ui/icons";
import { fileNameOf, mediaKind } from "../lib/media";
import s from "./PostMedia.module.css";

const PLACEHOLDER_TONES = ["#1d1b2e", "#14261f", "#2a1a1f", "#18202c", "#241c14", "#1f1a2a"];

function Placeholder({ title, seed, kind }: { title: string; seed: number; kind: "none" | "audio" | "pdf" }) {
  const Icon = kind === "audio" ? IconAudio : kind === "pdf" ? IconFile : IconImage;
  return (
    <div className={s.placeholder} style={{ background: PLACEHOLDER_TONES[seed % PLACEHOLDER_TONES.length] }}>
      <Icon size={22} />
      <span className={s.placeholderLabel}>{kind === "audio" ? "аудіо" : kind === "pdf" ? "pdf" : "без прев'ю"}</span>
      <span className={s.placeholderTitle}>{title}</span>
    </div>
  );
}

/**
 * Media for a post.
 * - `thumb`: one 16:10 preview for lists (prefers the first image).
 * - `feed`: the post as it appears in the feed — every file at full width, swiped
 *   with arrows and dots; audio and PDF show as a compact file row.
 * - `full`: every file, with a pager for several, native players for video/audio.
 */
export default function PostMedia({
  urls,
  title,
  seed,
  variant,
  placeholder = true,
}: {
  urls: string[];
  title: string;
  seed: number;
  variant: "thumb" | "feed" | "full";
  /** Draw a titled placeholder when there are no files (tiles); otherwise render nothing. */
  placeholder?: boolean;
}) {
  const [index, setIndex] = useState(0);

  if (urls.length === 0 && !placeholder) return null;

  if (variant === "thumb") {
    if (urls.length === 0) return <div className={s.thumb}><Placeholder title={title} seed={seed} kind="none" /></div>;
    const image = urls.find(u => mediaKind(u) === "image");
    if (image) {
      return (
        <div className={s.thumb}>
          <img src={image} alt={title} loading="lazy" className={s.cover} />
          {urls.length > 1 && <span className={s.count}>1 / {urls.length}</span>}
        </div>
      );
    }
    const first = urls[0];
    const kind = mediaKind(first);
    return (
      <div className={s.thumb}>
        {kind === "video" ? (
          <video src={first} muted preload="metadata" className={s.cover} />
        ) : (
          <Placeholder title={title} seed={seed} kind={kind === "audio" ? "audio" : "pdf"} />
        )}
        {urls.length > 1 && <span className={s.count}>1 / {urls.length}</span>}
      </div>
    );
  }

  if (variant === "feed") {
    if (urls.length === 0) return null;
    const i = Math.min(index, urls.length - 1);
    const u = urls[i];
    const k = mediaKind(u);
    const go = (e: React.MouseEvent, step: number) => {
      e.stopPropagation();
      setIndex(n => (n + step + urls.length) % urls.length);
    };
    return (
      <div className={s.feed}>
        {k === "image" && <img src={u} alt={urls.length > 1 ? `${title} — ${i + 1}` : title} loading="lazy" className={s.feedImg} />}
        {k === "video" && <video src={u} controls preload="metadata" className={s.feedImg} onClick={e => e.stopPropagation()} />}
        {(k === "audio" || k === "pdf") && (
          <div className={s.feedFile} onClick={e => e.stopPropagation()}>
            {k === "audio" ? <IconAudio size={22} /> : <IconFile size={22} />}
            <span className={s.fileName}>{fileNameOf(u)}</span>
            {k === "audio" ? (
              <audio src={u} controls preload="metadata" />
            ) : (
              <a href={u} target="_blank" rel="noreferrer" className={s.openLink}>
                Відкрити PDF <IconExternal size={14} />
              </a>
            )}
          </div>
        )}
        {urls.length > 1 && (
          <>
            <button type="button" className={cx(s.nav, s.navPrev)} onClick={e => go(e, -1)} aria-label="Попередній файл">
              <IconChevronLeft />
            </button>
            <button type="button" className={cx(s.nav, s.navNext)} onClick={e => go(e, 1)} aria-label="Наступний файл">
              <IconChevronRight />
            </button>
            <span className={s.feedCount}>
              {i + 1} / {urls.length}
            </span>
            <span className={s.dots} aria-hidden="true">
              {urls.map((_, n) => (
                <i key={n} className={n === i ? s.dotOn : undefined} />
              ))}
            </span>
          </>
        )}
      </div>
    );
  }

  if (urls.length === 0) {
    return (
      <div className={cx(s.stage, s.stageEmpty)}>
        <Placeholder title={title} seed={seed} kind="none" />
      </div>
    );
  }

  const safeIndex = Math.min(index, urls.length - 1);
  const url = urls[safeIndex];
  const kind = mediaKind(url);

  return (
    <div className={s.gallery}>
      <div className={s.stage}>
        {kind === "image" && (
          <a href={url} target="_blank" rel="noreferrer" title="Відкрити оригінал">
            <img src={url} alt={`${title} — ${safeIndex + 1}`} className={s.full} />
          </a>
        )}
        {kind === "video" && <video src={url} controls preload="metadata" className={s.full} />}
        {kind === "audio" && (
          <div className={s.audio}>
            <IconAudio size={28} />
            <span className={s.fileName}>{fileNameOf(url)}</span>
            <audio src={url} controls preload="metadata" />
          </div>
        )}
        {kind === "pdf" && (
          <div className={s.audio}>
            <IconFile size={28} />
            <span className={s.fileName}>{fileNameOf(url)}</span>
            <a href={url} target="_blank" rel="noreferrer" className={s.openLink}>
              Відкрити PDF <IconExternal size={14} />
            </a>
          </div>
        )}
        {urls.length > 1 && (
          <>
            <button
              type="button"
              className={cx(s.nav, s.navPrev)}
              onClick={() => setIndex(i => (i - 1 + urls.length) % urls.length)}
              aria-label="Попередній файл"
            >
              <IconChevronLeft />
            </button>
            <button type="button" className={cx(s.nav, s.navNext)} onClick={() => setIndex(i => (i + 1) % urls.length)} aria-label="Наступний файл">
              <IconChevronRight />
            </button>
            <span className={s.count}>
              {safeIndex + 1} / {urls.length}
            </span>
          </>
        )}
      </div>
      {urls.length > 1 && (
        <div className={s.strip} role="tablist" aria-label="Файли посту">
          {urls.map((u, i) => {
            const k = mediaKind(u);
            return (
              <button
                key={u + i}
                type="button"
                role="tab"
                aria-selected={i === safeIndex}
                className={cx(s.stripItem, i === safeIndex && s.stripActive)}
                onClick={() => setIndex(i)}
                aria-label={`Файл ${i + 1}`}
              >
                {k === "image" ? <img src={u} alt="" loading="lazy" /> : k === "audio" ? <IconAudio size={16} /> : k === "pdf" ? <IconFile size={16} /> : <span className={s.stripLabel}>video</span>}
              </button>
            );
          })}
        </div>
      )}
    </div>
  );
}
