import { FunctionComponent, useCallback, useEffect, useRef, useState } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import FrameComponent2 from '../components/FrameComponent2';
import {
  copyCheckApi,
  MATCH_TYPE_LABELS,
  ZONE_LABELS,
  type CopyCheckRecord,
  type CopyZone,
} from '../api/copycheck';
import styles from './CopyModerationPage.module.css';

const ZONE_FILTERS: { label: string; zones: CopyZone[] }[] = [
  { label: 'Червона й жовта зони', zones: ['red', 'yellow'] },
  { label: 'Лише червона', zones: ['red'] },
  { label: 'Лише жовта', zones: ['yellow'] },
];

const STATUS_FILTERS = [
  { label: 'Нерозглянуті', value: 'open' },
  { label: 'Підтверджені копії', value: 'confirmed_copy' },
  { label: 'Відхилені', value: 'dismissed' },
  { label: 'Усі', value: '' },
];

function formatWhen(iso: string): string {
  const d = new Date(iso);
  return Number.isNaN(d.getTime())
    ? iso
    : d.toLocaleString('uk-UA', { day: '2-digit', month: '2-digit', year: 'numeric', hour: '2-digit', minute: '2-digit' });
}

/** One possible copy: the side-by-side explanation is loaded on demand. */
const CheckCard: FunctionComponent<{ check: CopyCheckRecord; onDecided: () => void }> = ({ check, onDecided }) => {
  const [image, setImage] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const signals = check.result.best?.signals ?? {};

  useEffect(() => () => { if (image) URL.revokeObjectURL(image); }, [image]);

  async function showExplanation() {
    setError(null);
    const url = await copyCheckApi.explanationUrl(check.id);
    if (url) setImage(url);
    else setError('Зображення недоступні (можливо, пост уже видалено).');
  }

  async function decide(decision: 'confirmed_copy' | 'dismissed' | 'open') {
    setBusy(true);
    setError(null);
    try {
      await copyCheckApi.decide(check.id, decision);
      onDecided();
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Помилка');
    } finally {
      setBusy(false);
    }
  }

  return (
    <article className={styles.row}>
      <header className={styles.rowHead}>
        <span className={check.zone === 'red' ? styles.badgeRed : styles.badgeYellow}>{ZONE_LABELS[check.zone]}</span>
        <span className={styles.type}>{MATCH_TYPE_LABELS[check.match_type]}</span>
        <span className={styles.score}>P(копія) = {check.score.toFixed(2)}</span>
        <time className={styles.when}>{formatWhen(check.created_at)}</time>
      </header>

      <div className={styles.rowBody}>
        Пост <Link to={`/posts/${check.post_id}`}>#{check.post_id}</Link> схожий на пост{' '}
        <Link to={`/posts/${check.matched_post_id}`}>#{check.matched_post_id}</Link>
      </div>

      <div className={styles.signals}>
        {'sscd_similarity' in signals && <span>SSCD {Number(signals.sscd_similarity).toFixed(2)}</span>}
        {'phash_distance' in signals && <span>pHash Δ {signals.phash_distance}</span>}
        {'keypoint_inliers' in signals && <span>ключових точок {signals.keypoint_inliers}</span>}
        {'keypoint_centre_coverage' in signals && <span>покриття центру {Number(signals.keypoint_centre_coverage).toFixed(2)}</span>}
      </div>

      {image
        ? <img className={styles.explanation} src={image} alt="Порівняння зображень" />
        : <button className={styles.linkBtn} onClick={() => void showExplanation()}>Показати порівняння</button>}

      {check.status === 'open' ? (
        <div className={styles.actions}>
          <button className={styles.confirmBtn} disabled={busy} onClick={() => void decide('confirmed_copy')}>Це копія</button>
          <button className={styles.dismissBtn} disabled={busy} onClick={() => void decide('dismissed')}>Не копія</button>
        </div>
      ) : (
        <p className={styles.decided}>
          {check.status === 'confirmed_copy' ? 'Підтверджено як копію' : 'Відхилено'}
          {check.decided_by ? ` · ${check.decided_by}` : ''}
          {check.decided_at ? ` · ${formatWhen(check.decided_at)}` : ''}
          <button className={styles.linkBtn} disabled={busy} onClick={() => void decide('open')}>повернути в чергу</button>
        </p>
      )}
      {error && <p className={styles.error}>{error}</p>}
    </article>
  );
};

/**
 * Queue of possible copies found among published posts. The model only ranks and explains;
 * the moderator decides, and acts on the post through the usual moderation tools.
 */
const CopyModerationPage: FunctionComponent = () => {
  const navigate = useNavigate();
  const [zoneFilter, setZoneFilter] = useState(0);
  const [status, setStatus] = useState('open');
  const [checks, setChecks] = useState<CopyCheckRecord[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  // Only the latest request may update the list: after a decision the reload with the old
  // filter can finish after the one for a newly chosen filter and would overwrite it.
  const latestRequest = useRef(0);

  const load = useCallback(async () => {
    const request = ++latestRequest.current;
    setLoading(true);
    setError(null);
    try {
      const data = await copyCheckApi.queue({ zones: ZONE_FILTERS[zoneFilter].zones, status });
      if (request === latestRequest.current) setChecks(data);
    } catch (e) {
      if (request !== latestRequest.current) return;
      setError(e instanceof Error ? e.message : 'Помилка завантаження');
      setChecks([]);
    } finally {
      if (request === latestRequest.current) setLoading(false);
    }
  }, [zoneFilter, status]);

  useEffect(() => { void load(); }, [load]);

  // A card may finish its decision after the filters changed (or after it left the list);
  // it must reload with the filters current at that moment, not the ones it rendered with.
  const loadRef = useRef(load);
  loadRef.current = load;
  const reloadCurrent = useCallback(() => void loadRef.current(), []);

  return (
    <div className={styles.page}>
      <FrameComponent2 />
      <div className={styles.rightCol}>
        <header className={styles.header}>
          <button className={styles.backBtn} onClick={() => navigate(-1)}>← Назад</button>
          <h1 className={styles.pageTitle}>Можливі копії зображень</h1>
        </header>

        <div className={styles.toolbar}>
          <label className={styles.field}>
            <span>Зона</span>
            <select value={zoneFilter} onChange={e => setZoneFilter(Number(e.target.value))}>
              {ZONE_FILTERS.map((f, i) => <option key={f.label} value={i}>{f.label}</option>)}
            </select>
          </label>
          <label className={styles.field}>
            <span>Статус</span>
            <select value={status} onChange={e => setStatus(e.target.value)}>
              {STATUS_FILTERS.map(f => <option key={f.value} value={f.value}>{f.label}</option>)}
            </select>
          </label>
          <button className={styles.refreshBtn} onClick={() => void load()} disabled={loading}>
            {loading ? 'Завантаження…' : 'Оновити'}
          </button>
        </div>

        {error && <p className={styles.error}>Помилка: {error}</p>}
        {!loading && !error && checks.length === 0 && <p className={styles.empty}>Можливих копій немає.</p>}

        <div className={styles.list}>
          {checks.map(c => <CheckCard key={c.id} check={c} onDecided={reloadCurrent} />)}
        </div>
      </div>
    </div>
  );
};

export default CopyModerationPage;
