import { useEffect, useMemo, useState } from "react";
import { Link } from "react-router-dom";
import { copyCheckApi, MATCH_TYPE_LABELS, ZONE_LABELS, type CopyCheckResult, type CopyZone } from "../api/copycheck";
import { Spinner, cx } from "../ui";
import { IconAlert, IconCheck, IconInfo } from "../ui/icons";
import s from "./CopyCheckNotice.module.css";

interface Props {
  file: File;
  /** Reports the finished check (or null when it could not run) to the page. */
  onResult?: (result: CopyCheckResult | null) => void;
}

// eslint-disable-next-line react-refresh/only-export-components
export const ZONE_COLOR: Record<CopyZone, string> = {
  red: "var(--red)",
  yellow: "var(--amber)",
  green: "var(--teal)",
};

/**
 * One image in the "image check" panel of the post form. The image is compared with works
 * already published on the platform before the post goes out. It never blocks: the author
 * decides, and a moderator sees the match on the post if it is published anyway.
 */
export default function CopyCheckNotice({ file, onResult }: Props) {
  const [state, setState] = useState<"checking" | "done" | "unavailable">("checking");
  const [result, setResult] = useState<CopyCheckResult | null>(null);
  const preview = useMemo(() => URL.createObjectURL(file), [file]);

  useEffect(() => () => URL.revokeObjectURL(preview), [preview]);

  useEffect(() => {
    let cancelled = false;
    setState("checking");
    copyCheckApi
      .preview(file)
      .then(r => {
        if (cancelled) return;
        setResult(r);
        setState("done");
        onResult?.(r);
      })
      .catch(() => {
        if (cancelled) return;
        setState("unavailable");
        onResult?.(null);
      });
    return () => {
      cancelled = true;
    };
    // onResult is a fresh closure on every render of the page; the check depends on the file only.
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [file]);

  const match = state === "done" && result && result.zone !== "green" && result.best ? result : null;
  const tone = match ? match.zone : state === "done" ? "green" : null;

  return (
    <li className={cx(s.row, match && (match.zone === "red" ? s.red : s.yellow))} role="status">
      <div className={s.main}>
        <img src={preview} alt="" className={s.thumb} />
        <span className={s.text}>
          <span className={s.name}>{file.name}</span>
          <span className={s.state}>
            {state === "checking" && (
              <>
                <Spinner /> перевіряється…
              </>
            )}
            {state === "unavailable" && (
              <>
                <IconInfo size={13} /> перевірка зараз недоступна
              </>
            )}
            {state === "done" && !match && (
              <>
                <IconCheck size={13} /> {ZONE_LABELS.green.toLowerCase()}
              </>
            )}
            {match && (
              <>
                <IconAlert size={13} /> {ZONE_LABELS[match.zone].toLowerCase()}
              </>
            )}
          </span>
        </span>
        {tone && result && (
          <span className={s.score} style={{ color: ZONE_COLOR[tone] }}>
            {Math.round((match ? match.best!.p_copy : result.score) * 100)}%
          </span>
        )}
      </div>
      {match && (
        <p className={s.detail}>
          {MATCH_TYPE_LABELS[match.best!.match_type]} — найближча робота{" "}
          <Link to={`/posts/${match.best!.post_id}`} target="_blank">
            #{match.best!.post_id}
          </Link>
          . Якщо це ваша робота або вона використана з дозволу — можна публікувати: рішення ухвалює модератор, автоматичних санкцій немає.
        </p>
      )}
    </li>
  );
}
