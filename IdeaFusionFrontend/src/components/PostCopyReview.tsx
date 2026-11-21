import { useCallback, useEffect, useState } from "react";
import { Link } from "react-router-dom";
import { copyCheckApi, MATCH_TYPE_LABELS, ZONE_LABELS, type CopyCheckRecord } from "../api/copycheck";
import { checksForPost, invalidateCopyFlags } from "../lib/copyFlags";
import { Button, Meter, Notice, Spinner, Status } from "../ui";
import { IconCheck, IconCopy } from "../ui/icons";
import { ZONE_COLOR } from "./CopyCheckNotice";
import s from "./PostCopyReview.module.css";

function formatWhen(iso: string): string {
  const d = new Date(iso);
  return Number.isNaN(d.getTime()) ? iso : d.toLocaleString("uk-UA", { day: "2-digit", month: "2-digit", year: "numeric", hour: "2-digit", minute: "2-digit" });
}

/** One match: the side-by-side picture from the model, the score and the moderator's decision. */
export function CheckItem({ check, onDecided }: { check: CopyCheckRecord; onDecided: () => void }) {
  const [image, setImage] = useState<string | null>(null);
  const [imageState, setImageState] = useState<"loading" | "ready" | "missing">("loading");
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    let cancelled = false;
    let url: string | null = null;
    copyCheckApi
      .explanationUrl(check.id)
      .then(u => {
        url = u;
        if (cancelled) {
          if (u) URL.revokeObjectURL(u);
          return;
        }
        setImage(u);
        setImageState(u ? "ready" : "missing");
      })
      .catch(() => !cancelled && setImageState("missing"));
    return () => {
      cancelled = true;
      if (url) URL.revokeObjectURL(url);
    };
  }, [check.id]);

  async function decide(decision: "confirmed_copy" | "dismissed" | "open") {
    setBusy(true);
    setError(null);
    try {
      await copyCheckApi.decide(check.id, decision);
      invalidateCopyFlags();
      onDecided();
    } catch (e) {
      setError(e instanceof Error ? e.message : "Не вдалося зберегти рішення");
    } finally {
      setBusy(false);
    }
  }

  return (
    <li className={s.item}>
      <div className={s.top}>
        <Status tone={check.zone === "red" ? "red" : "amber"}>{ZONE_LABELS[check.zone].toLowerCase()}</Status>
        <span className={s.score} style={{ color: ZONE_COLOR[check.zone] }}>
          {Math.round(check.score * 100)}%
        </span>
      </div>
      <p className={s.text}>
        {MATCH_TYPE_LABELS[check.match_type]} — схоже на{" "}
        {check.matched_post_id != null ? <Link to={`/posts/${check.matched_post_id}`}>пост #{check.matched_post_id}</Link> : "видалений пост"}
        <span className={s.when}> · {formatWhen(check.created_at)}</span>
      </p>
      <Meter value={check.score} color={ZONE_COLOR[check.zone]} label="Ймовірність копії" />
      <div className={s.compare}>
        {imageState === "loading" && (
          <span className={s.compareEmpty}>
            <Spinner /> порівняння…
          </span>
        )}
        {imageState === "missing" && <span className={s.compareEmpty}>Зображення для порівняння недоступне.</span>}
        {imageState === "ready" && image && <img src={image} alt="Нове зображення поруч зі знайденим збігом" />}
      </div>
      {error && <Notice tone="error">{error}</Notice>}
      {check.status === "open" ? (
        <div className={s.decision}>
          <Button size="sm" variant="danger" loading={busy} icon={<IconCopy size={15} />} onClick={() => void decide("confirmed_copy")}>
            Це копія
          </Button>
          <Button size="sm" variant="success" disabled={busy} icon={<IconCheck size={15} />} onClick={() => void decide("dismissed")}>
            Не копія
          </Button>
        </div>
      ) : (
        <div className={s.decided}>
          <Status tone={check.status === "confirmed_copy" ? "red" : "teal"}>{check.status === "confirmed_copy" ? "підтверджено як копію" : "не копія"}</Status>
          <span className={s.when}>
            {check.decided_by ? check.decided_by : ""}
            {check.decided_at ? ` · ${formatWhen(check.decided_at)}` : ""}
          </span>
          <Button size="sm" variant="ghost" disabled={busy} onClick={() => void decide("open")}>
            Переглянути знову
          </Button>
        </div>
      )}
    </li>
  );
}

/**
 * Shown to moderators on a post page: the image copy checks that ran when this post was
 * published. Renders nothing when the post's images had no match.
 */
export default function PostCopyReview({ postId }: { postId: number }) {
  const [checks, setChecks] = useState<CopyCheckRecord[] | null>(null);
  const [error, setError] = useState(false);

  const load = useCallback(() => {
    setError(false);
    checksForPost(postId)
      .then(setChecks)
      .catch(() => setError(true));
  }, [postId]);

  useEffect(load, [load]);

  if (error) {
    return (
      <section className={s.box} id="copy-check">
        <h2 className={s.title}>Перевірка зображень</h2>
        <Notice tone="warning">Не вдалося завантажити результати перевірки.</Notice>
        <Button size="sm" onClick={load}>
          Спробувати ще
        </Button>
      </section>
    );
  }
  if (!checks || checks.length === 0) return null;

  return (
    <section className={s.box} id="copy-check" aria-labelledby="copy-check-title">
      <h2 id="copy-check-title" className={s.title}>
        Перевірка зображень
      </h2>
      <p className={s.note}>Знайдено під час публікації. Модель лише показує збіги — рішення за модератором.</p>
      <ul className={s.list}>
        {checks.map(c => (
          <CheckItem key={c.id} check={c} onDecided={load} />
        ))}
      </ul>
    </section>
  );
}
