import { Link } from "react-router-dom";
import { IconAudio, IconComment, IconFile, IconHeart, IconVideo } from "../ui/icons";
import { mediaKind } from "../lib/media";
import s from "./PostTile.module.css";

interface Props {
  postId: number | string;
  title: string;
  mediaUrls: string[];
  likes?: number;
  comments?: number;
  /** Extra line under the title on text-only tiles (author, tag…). */
  caption?: string;
}

/**
 * A square post preview for profile and team grids. Shows the first image (or video frame);
 * a post without pictures shows its title instead. Counts appear on hover when known.
 */
export default function PostTile({ postId, title, mediaUrls, likes, comments, caption }: Props) {
  const image = mediaUrls.find(u => mediaKind(u) === "image");
  const video = !image ? mediaUrls.find(u => mediaKind(u) === "video") : undefined;
  const other = !image && !video ? mediaUrls[0] : undefined;
  const otherKind = other ? mediaKind(other) : null;
  const hasCounts = likes != null || comments != null;

  return (
    <Link to={`/posts/${postId}`} className={s.tile} aria-label={title}>
      {image && <img src={image} alt="" loading="lazy" className={s.img} />}
      {video && <video src={video} muted preload="metadata" className={s.img} />}
      {!image && !video && (
        <span className={s.text}>
          {otherKind && (
            <span className={s.kind}>
              {otherKind === "audio" ? <IconAudio size={16} /> : <IconFile size={16} />}
              {otherKind === "audio" ? "аудіо" : "файл"}
            </span>
          )}
          <span className={s.title}>{title}</span>
          {caption && <span className={s.caption}>{caption}</span>}
        </span>
      )}
      {video && (
        <span className={s.corner} aria-hidden="true">
          <IconVideo size={16} />
        </span>
      )}
      {!video && mediaUrls.length > 1 && (
        <span className={s.corner} aria-hidden="true">
          {mediaUrls.length}
        </span>
      )}
      <span className={s.overlay}>
        {(image || video) && <span className={s.overlayTitle}>{title}</span>}
        {hasCounts && (
          <span className={s.counts}>
            {likes != null && (
              <span>
                <IconHeart size={16} /> {likes}
              </span>
            )}
            {comments != null && (
              <span>
                <IconComment size={16} /> {comments}
              </span>
            )}
          </span>
        )}
      </span>
    </Link>
  );
}
