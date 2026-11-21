import type { ReactNode } from "react";
import { Link, useNavigate } from "react-router-dom";
import { KIND_LABEL, hitLink } from "../api/search";
import type { SearchHit } from "../api/search";
import { IconBriefcase, IconEye, IconHeart, IconImage, IconUser, IconUsers } from "../ui/icons";
import s from "./SearchHitCard.module.css";

export type SearchHitCardProps = {
  hit: SearchHit;
  /** A line above the title, e.g. why the feed recommends this post. */
  note?: ReactNode;
  /** Called before navigating, e.g. to log a feed click. */
  onOpen?: () => void;
  onTagClick?: (tag: string) => void;
};

function KindIcon({ kind, size = 20 }: { kind: SearchHit["kind"]; size?: number }) {
  if (kind === "team") return <IconUsers size={size} />;
  if (kind === "vacancy") return <IconBriefcase size={size} />;
  if (kind === "user") return <IconUser size={size} />;
  return <IconImage size={size} />;
}

/** One search result or feed item of any kind: post, team, vacancy or person. */
export default function SearchHitCard({ hit, note, onOpen, onTagClick }: SearchHitCardProps) {
  const navigate = useNavigate();
  const link = hitLink(hit);
  const open = () => {
    onOpen?.();
    navigate(link);
  };

  return (
    <article className={s.card} data-kind={hit.kind} onClick={open}>
      {/* The kind is told by an icon, not a word: the tile's own icon when there is no
          picture, otherwise a small badge in the corner of the picture. */}
      <div className={s.media} title={KIND_LABEL[hit.kind]}>
        {hit.imageUrl ? (
          <>
            <img src={hit.imageUrl} alt="" loading="lazy" />
            <span className={s.kindBadge} aria-hidden="true">
              <KindIcon kind={hit.kind} size={13} />
            </span>
          </>
        ) : (
          <span className={s.mediaIcon}>
            <KindIcon kind={hit.kind} />
          </span>
        )}
      </div>
      <div className={s.body}>
        {note && <div className={s.note}>{note}</div>}
        <div className={s.titleRow}>
          <span className={s.srOnly}>{KIND_LABEL[hit.kind]}:</span>
          <h3 className={s.title}>
            <Link
              to={link}
              onClick={e => {
                e.stopPropagation();
                onOpen?.();
              }}
            >
              {hit.title}
            </Link>
          </h3>
        </div>
        {hit.subtitle && <div className={s.subtitle}>{hit.subtitle}</div>}
        {hit.snippet && <p className={s.snippet}>{hit.snippet}</p>}
        <div className={s.foot}>
          {hit.tags.slice(0, 6).map(t =>
            onTagClick ? (
              <button
                key={t}
                type="button"
                className={s.tag}
                onClick={e => {
                  e.stopPropagation();
                  onTagClick(t);
                }}
                title="Додати тег у фільтр"
              >
                #{t}
              </button>
            ) : (
              <span key={t} className={s.tag}>
                #{t}
              </span>
            ),
          )}
          <span className={s.meta}>
            {hit.kind === "post" && (
              <>
                <span><IconHeart size={13} /> {hit.likes}</span>
                <span><IconEye size={13} /> {hit.views}</span>
              </>
            )}
            {hit.kind === "team" && hit.openVacancies > 0 && <span>вакансій: {hit.openVacancies}</span>}
            {hit.kind !== "user" && <span>{new Date(hit.createdAt).toLocaleDateString("uk-UA")}</span>}
          </span>
        </div>
      </div>
    </article>
  );
}
