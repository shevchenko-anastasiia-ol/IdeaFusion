import { FunctionComponent, useEffect, useState } from 'react';
import { Link } from 'react-router-dom';
import { copyCheckApi, MATCH_TYPE_LABELS, ZONE_LABELS, type CopyCheckResult } from '../api/copycheck';
import styles from './CopyCheckNotice.module.css';

interface Props {
  file: File;
  /** Reports the finished check (or null when it could not run) to the page. */
  onResult?: (result: CopyCheckResult | null) => void;
}

/**
 * Checks one image against works already published on the platform and tells the author
 * what it resembles, before the post goes out. It never blocks: the author decides, and a
 * moderator sees the match later if the post is published anyway.
 */
const CopyCheckNotice: FunctionComponent<Props> = ({ file, onResult }) => {
  const [state, setState] = useState<'checking' | 'done' | 'unavailable'>('checking');
  const [result, setResult] = useState<CopyCheckResult | null>(null);

  useEffect(() => {
    let cancelled = false;
    setState('checking');
    copyCheckApi.preview(file)
      .then(r => {
        if (cancelled) return;
        setResult(r);
        setState('done');
        onResult?.(r);
      })
      .catch(() => {
        if (cancelled) return;
        setState('unavailable');
        onResult?.(null);
      });
    return () => { cancelled = true; };
    // onResult is a fresh closure on every render of the page; the check depends on the file only.
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [file]);

  if (state === 'checking') return <p className={styles.muted}>Перевіряємо на збіги з опублікованими роботами…</p>;
  if (state === 'unavailable') return <p className={styles.muted}>Перевірка на копії зараз недоступна.</p>;
  if (!result || result.zone === 'green' || !result.best) return <p className={styles.ok}>✓ {ZONE_LABELS.green}</p>;

  const best = result.best;
  return (
    <div className={result.zone === 'red' ? styles.red : styles.yellow}>
      <strong>{ZONE_LABELS[result.zone]}</strong>
      <span>
        {' '}— {MATCH_TYPE_LABELS[best.match_type]} роботи{' '}
        <Link to={`/posts/${best.post_id}`} target="_blank">#{best.post_id}</Link>
        {' '}(ймовірність копії {(best.p_copy * 100).toFixed(0)}%).
      </span>
      <p className={styles.hint}>
        Якщо це ваша робота, опублікована раніше з іншого акаунта, або вона використана з дозволу —
        можна публікувати: остаточне рішення ухвалює модератор, автоматичних санкцій немає.
      </p>
    </div>
  );
};

export default CopyCheckNotice;
