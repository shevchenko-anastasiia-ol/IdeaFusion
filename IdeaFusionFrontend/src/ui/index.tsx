/* This module is the UI kit barrel: components plus a few helpers (cx, avatarTone) and
   re-exports. Fast refresh falls back to a full reload for it, which is acceptable. */
/* eslint-disable react-refresh/only-export-components */
import { createPortal } from "react-dom";
import {
  forwardRef,
  useEffect,
  useId,
  useLayoutEffect,
  useRef,
  useState,
  type CSSProperties,
  type ButtonHTMLAttributes,
  type InputHTMLAttributes,
  type ReactNode,
  type SelectHTMLAttributes,
  type TextareaHTMLAttributes,
} from "react";
import { Link } from "react-router-dom";
import s from "./ui.module.css";
import { IconAlert, IconCheck, IconChevronLeft, IconChevronRight, IconClose, IconInfo, IconMore } from "./icons";

export function cx(...parts: (string | false | null | undefined)[]): string {
  return parts.filter(Boolean).join(" ");
}

/* ── Button ─────────────────────────────────────────────────────────── */
export type ButtonVariant = "primary" | "secondary" | "ghost" | "danger" | "success";
export type ButtonSize = "sm" | "md" | "lg";

type ButtonOwnProps = {
  variant?: ButtonVariant;
  size?: ButtonSize;
  block?: boolean;
  loading?: boolean;
  icon?: ReactNode;
};

function btnClass(variant: ButtonVariant, size: ButtonSize, block?: boolean, extra?: string) {
  return cx(s.btn, s[variant], size !== "md" && s[size], block && s.block, extra);
}

export const Button = forwardRef<HTMLButtonElement, ButtonOwnProps & ButtonHTMLAttributes<HTMLButtonElement>>(
  function Button({ variant = "secondary", size = "md", block, loading, icon, className, children, disabled, type = "button", ...rest }, ref) {
    return (
      <button
        ref={ref}
        type={type}
        className={btnClass(variant, size, block, className)}
        disabled={disabled || loading}
        aria-busy={loading || undefined}
        {...rest}
      >
        {loading ? <span className={s.spinner} aria-hidden="true" /> : icon}
        {children}
      </button>
    );
  },
);

export function ButtonLink({
  to,
  variant = "secondary",
  size = "md",
  block,
  icon,
  className,
  children,
}: ButtonOwnProps & { to: string; className?: string; children?: ReactNode }) {
  return (
    <Link to={to} className={btnClass(variant, size, block, className)}>
      {icon}
      {children}
    </Link>
  );
}

export function IconButton({
  label,
  size = "md",
  className,
  children,
  type = "button",
  ...rest
}: { label: string; size?: "sm" | "md" } & ButtonHTMLAttributes<HTMLButtonElement>) {
  return (
    <button
      type={type}
      aria-label={label}
      title={label}
      className={cx(s.iconBtn, size === "sm" && s.iconBtnSm, className)}
      {...rest}
    >
      {children}
    </button>
  );
}

export function Spinner() {
  return <span className={s.spinner} aria-hidden="true" />;
}

/* ── Form fields ────────────────────────────────────────────────────── */
export function Field({
  label,
  hint,
  error,
  required,
  htmlFor,
  children,
  className,
}: {
  label?: ReactNode;
  hint?: ReactNode;
  error?: string | null;
  required?: boolean;
  htmlFor?: string;
  children: ReactNode;
  className?: string;
}) {
  return (
    <div className={cx(s.field, className)}>
      {label && (
        <label className={s.label} htmlFor={htmlFor}>
          {label}
          {required && <span className={s.required} aria-hidden="true">*</span>}
        </label>
      )}
      {children}
      {error ? <span className={s.error} role="alert">{error}</span> : hint ? <span className={s.hint}>{hint}</span> : null}
    </div>
  );
}

export const Input = forwardRef<HTMLInputElement, InputHTMLAttributes<HTMLInputElement> & { invalid?: boolean }>(
  function Input({ className, invalid, ...rest }, ref) {
    return <input ref={ref} className={cx(s.input, invalid && s.invalid, className)} aria-invalid={invalid || undefined} {...rest} />;
  },
);

export const Textarea = forwardRef<HTMLTextAreaElement, TextareaHTMLAttributes<HTMLTextAreaElement> & { invalid?: boolean }>(
  function Textarea({ className, invalid, ...rest }, ref) {
    return <textarea ref={ref} className={cx(s.textarea, invalid && s.invalid, className)} aria-invalid={invalid || undefined} {...rest} />;
  },
);

export const Select = forwardRef<HTMLSelectElement, SelectHTMLAttributes<HTMLSelectElement>>(
  function Select({ className, children, ...rest }, ref) {
    return (
      <select ref={ref} className={cx(s.select, className)} {...rest}>
        {children}
      </select>
    );
  },
);

export function Checkbox({ label, ...rest }: { label: ReactNode } & InputHTMLAttributes<HTMLInputElement>) {
  return (
    <label className={s.checkbox}>
      <input type="checkbox" {...rest} />
      <span>{label}</span>
    </label>
  );
}

export function Switch({
  checked,
  onChange,
  label,
  disabled,
}: {
  checked: boolean;
  onChange: (next: boolean) => void;
  label?: ReactNode;
  disabled?: boolean;
}) {
  return (
    <label className={s.switch}>
      <input type="checkbox" role="switch" checked={checked} disabled={disabled} onChange={e => onChange(e.target.checked)} />
      <span className={s.switchTrack} aria-hidden="true" />
      {label && <span>{label}</span>}
    </label>
  );
}

/* ── Tag / Badge / Status ───────────────────────────────────────────── */
export function Tag({
  children,
  active,
  onClick,
  onRemove,
  title,
}: {
  children: ReactNode;
  active?: boolean;
  onClick?: () => void;
  onRemove?: () => void;
  title?: string;
}) {
  const cls = cx(s.tag, active && s.tagActive);
  if (onClick) {
    return (
      <button type="button" className={cls} onClick={onClick} aria-pressed={active} title={title}>
        {children}
      </button>
    );
  }
  return (
    <span className={cls} title={title}>
      {children}
      {onRemove && (
        <button type="button" className={s.tagRemove} onClick={onRemove} aria-label="Прибрати">
          <IconClose size={12} />
        </button>
      )}
    </span>
  );
}

export type Tone = "neutral" | "accent" | "teal" | "amber" | "red" | "blue";

export function Badge({ tone = "neutral", children, icon }: { tone?: Tone; children: ReactNode; icon?: ReactNode }) {
  return (
    <span className={cx(s.badge, s[`badge-${tone}`])}>
      {icon}
      {children}
    </span>
  );
}

export function Status({ tone = "neutral", children }: { tone?: Tone; children: ReactNode }) {
  return (
    <span className={cx(s.status, s[`status-${tone}`])}>
      <span className={s.statusDot} aria-hidden="true" />
      {children}
    </span>
  );
}

/* ── Avatar ─────────────────────────────────────────────────────────── */
const AVATAR_TONES = ["#6a4de0", "#0b9f86", "#d65a5a", "#c98a2e", "#3d8fc4", "#9b51c9", "#4f6bd8", "#2f8f6b"];

export function avatarTone(seed: string | number | undefined | null): string {
  const str = String(seed ?? "");
  let h = 0;
  for (let i = 0; i < str.length; i++) h = (h * 31 + str.charCodeAt(i)) >>> 0;
  return AVATAR_TONES[h % AVATAR_TONES.length];
}

export function initialsOf(name: string | undefined | null): string {
  const clean = (name ?? "").trim();
  if (!clean) return "?";
  const parts = clean.split(/[\s._-]+/).filter(Boolean);
  if (parts.length >= 2) return (parts[0][0] + parts[1][0]).toUpperCase();
  return clean.slice(0, 2).toUpperCase();
}

export function Avatar({
  name,
  src,
  size = 32,
  square,
  seed,
  className,
}: {
  name?: string | null;
  src?: string | null;
  size?: number;
  square?: boolean;
  seed?: string | number | null;
  className?: string;
}) {
  const [failed, setFailed] = useState(false);
  useEffect(() => setFailed(false), [src]);
  const showImg = !!src && !failed;
  return (
    <span
      className={cx(s.avatar, square && s.avatarSquare, className)}
      style={{
        width: size,
        height: size,
        fontSize: Math.max(10, Math.round(size * 0.38)),
        background: showImg ? undefined : avatarTone(seed ?? name),
      }}
      aria-hidden={name ? undefined : true}
      title={name ?? undefined}
    >
      {showImg ? <img src={src!} alt={name ?? ""} onError={() => setFailed(true)} /> : initialsOf(name)}
    </span>
  );
}

export function AvatarStack({
  people,
  max = 4,
  size = 26,
}: {
  people: { id: string; name: string; src?: string | null }[];
  max?: number;
  size?: number;
}) {
  const shown = people.slice(0, max);
  const rest = people.length - shown.length;
  return (
    <span className={s.avatarStack}>
      {shown.map(p => (
        <Avatar key={p.id} name={p.name} src={p.src} seed={p.id} size={size} />
      ))}
      {rest > 0 && (
        <span className={cx(s.avatar, s.avatarMore)} style={{ width: size, height: size, fontSize: 11 }}>
          +{rest}
        </span>
      )}
    </span>
  );
}

/* ── Tabs / Segmented ───────────────────────────────────────────────── */
export function Tabs<T extends string>({
  value,
  onChange,
  items,
  className,
  label,
}: {
  value: T;
  onChange: (v: T) => void;
  items: { value: T; label: ReactNode; count?: number; hidden?: boolean }[];
  className?: string;
  label?: string;
}) {
  return (
    <div className={cx(s.tabs, className)} role="tablist" aria-label={label}>
      {items
        .filter(i => !i.hidden)
        .map(i => (
          <button
            key={i.value}
            type="button"
            role="tab"
            aria-selected={value === i.value}
            className={cx(s.tab, value === i.value && s.tabActive)}
            onClick={() => onChange(i.value)}
          >
            {i.label}
            {i.count != null && <span className={s.tabCount}>{i.count}</span>}
          </button>
        ))}
    </div>
  );
}

export function Segmented<T extends string>({
  value,
  onChange,
  items,
  label,
}: {
  value: T;
  onChange: (v: T) => void;
  items: { value: T; label: ReactNode }[];
  label?: string;
}) {
  return (
    <div className={s.segmented} role="radiogroup" aria-label={label}>
      {items.map(i => (
        <button
          key={i.value}
          type="button"
          role="radio"
          aria-checked={value === i.value}
          className={cx(s.segment, value === i.value && s.segmentActive)}
          onClick={() => onChange(i.value)}
        >
          {i.label}
        </button>
      ))}
    </div>
  );
}

/* ── Panel ──────────────────────────────────────────────────────────── */
export function Panel({
  title,
  actions,
  children,
  padded = true,
  className,
}: {
  title?: ReactNode;
  actions?: ReactNode;
  children: ReactNode;
  padded?: boolean;
  className?: string;
}) {
  return (
    <section className={cx(s.panel, className)}>
      {(title || actions) && (
        <header className={s.panelHeader}>
          {title && <h2 className={s.panelTitle}>{title}</h2>}
          {actions}
        </header>
      )}
      <div className={padded ? s.panelBody : undefined}>{children}</div>
    </section>
  );
}

export function SectionTitle({ children }: { children: ReactNode }) {
  return <h2 className={s.sectionTitle}>{children}</h2>;
}

/* ── Page header ────────────────────────────────────────────────────── */
export function PageHeader({
  title,
  description,
  actions,
}: {
  title: ReactNode;
  description?: ReactNode;
  actions?: ReactNode;
}) {
  return (
    <header className={s.pageHeader}>
      <div className={s.pageHeaderText}>
        <h1 className={s.pageTitle}>{title}</h1>
        {description && <p className={s.pageDesc}>{description}</p>}
      </div>
      {actions && <div className={s.pageActions}>{actions}</div>}
    </header>
  );
}

/* ── States ─────────────────────────────────────────────────────────── */
export function EmptyState({
  icon,
  title,
  text,
  action,
}: {
  icon?: ReactNode;
  title: ReactNode;
  text?: ReactNode;
  action?: ReactNode;
}) {
  return (
    <div className={s.empty}>
      {icon && <div className={s.emptyIcon}>{icon}</div>}
      <p className={s.emptyTitle}>{title}</p>
      {text && <p className={s.emptyText}>{text}</p>}
      {action && <div className={s.emptyAction}>{action}</div>}
    </div>
  );
}

export function ErrorState({
  title = "Не вдалося завантажити дані",
  message,
  onRetry,
}: {
  title?: string;
  message?: string | null;
  onRetry?: () => void;
}) {
  return (
    <div className={s.errorBox} role="alert">
      <IconAlert />
      <div className={s.errorBody}>
        <strong>{title}</strong>
        {message && <span className={s.errorMsg}>{message}</span>}
        {onRetry && (
          <Button size="sm" onClick={onRetry}>
            Спробувати ще раз
          </Button>
        )}
      </div>
    </div>
  );
}

export function Notice({
  tone = "info",
  children,
}: {
  tone?: "info" | "success" | "warning" | "error";
  children: ReactNode;
}) {
  const icon = tone === "success" ? <IconCheck size={16} /> : tone === "info" ? <IconInfo size={16} /> : <IconAlert size={16} />;
  return (
    <div className={cx(s.notice, s[`notice-${tone}`])} role={tone === "error" ? "alert" : "status"}>
      {icon}
      <div>{children}</div>
    </div>
  );
}

export function Skeleton({ width = "100%", height = 14, radius }: { width?: number | string; height?: number | string; radius?: number }) {
  return <span className={s.skeleton} style={{ width, height, borderRadius: radius }} aria-hidden="true" />;
}

export function Loading({ label = "Завантаження…" }: { label?: string }) {
  return (
    <div className={s.loading} role="status">
      <Spinner />
      {label}
    </div>
  );
}

/* ── Pagination ─────────────────────────────────────────────────────── */
export function Pagination({
  page,
  totalPages,
  hasNext,
  onChange,
}: {
  page: number;
  totalPages?: number;
  hasNext?: boolean;
  onChange: (page: number) => void;
}) {
  const canNext = totalPages != null ? page < totalPages : !!hasNext;
  if (page <= 1 && !canNext) return null;
  return (
    <nav className={s.pagination} aria-label="Сторінки">
      <Button size="sm" variant="ghost" icon={<IconChevronLeft size={16} />} disabled={page <= 1} onClick={() => onChange(page - 1)}>
        Назад
      </Button>
      <span className={s.pageInfo}>
        {page}
        {totalPages != null ? ` / ${totalPages}` : ""}
      </span>
      <Button size="sm" variant="ghost" disabled={!canNext} onClick={() => onChange(page + 1)}>
        Далі
        <IconChevronRight size={16} />
      </Button>
    </nav>
  );
}

/* ── Menu ───────────────────────────────────────────────────────────── */
export type MenuItem =
  | { label: ReactNode; onSelect: () => void; icon?: ReactNode; danger?: boolean; hidden?: boolean }
  | { separator: true; hidden?: boolean };

export function Menu({
  items,
  label = "Дії",
  trigger,
  placement = "down",
}: {
  items: MenuItem[];
  label?: string;
  trigger?: (props: { onClick: () => void; "aria-expanded": boolean; "aria-haspopup": "menu" }) => ReactNode;
  placement?: "down" | "up";
}) {
  const [open, setOpen] = useState(false);
  const [pos, setPos] = useState<CSSProperties | null>(null);
  const ref = useRef<HTMLDivElement>(null);
  const menuRef = useRef<HTMLDivElement>(null);
  const id = useId();

  // The menu is rendered into <body> with fixed coordinates taken from the trigger, so a
  // container with overflow: hidden (the collapsed sidebar) can no longer clip it.
  useLayoutEffect(() => {
    if (!open || !ref.current) {
      setPos(null);
      return;
    }
    const r = ref.current.getBoundingClientRect();
    const gap = 4;
    const vw = window.innerWidth;
    const vh = window.innerHeight;
    if (placement === "up") {
      // Opens upwards from the trigger's left edge; kept on screen on the right.
      setPos({ bottom: vh - r.top + gap, left: Math.max(8, Math.min(r.left, vw - 216)) });
    } else {
      // Opens downwards, right edges aligned, as before.
      setPos({ top: r.bottom + gap, right: Math.max(8, vw - r.right) });
    }
  }, [open, placement]);

  useEffect(() => {
    if (!open) return;
    function onDoc(e: MouseEvent) {
      const t = e.target as Node;
      if (ref.current?.contains(t) || menuRef.current?.contains(t)) return;
      setOpen(false);
    }
    function onKey(e: KeyboardEvent) {
      if (e.key === "Escape") setOpen(false);
    }
    // Fixed coordinates would drift from the trigger, so the menu closes instead.
    const onMove = () => setOpen(false);
    document.addEventListener("mousedown", onDoc);
    document.addEventListener("keydown", onKey);
    window.addEventListener("resize", onMove);
    window.addEventListener("scroll", onMove, true);
    return () => {
      document.removeEventListener("mousedown", onDoc);
      document.removeEventListener("keydown", onKey);
      window.removeEventListener("resize", onMove);
      window.removeEventListener("scroll", onMove, true);
    };
  }, [open]);

  const visible = items.filter(i => !i.hidden);
  if (visible.length === 0) return null;

  const triggerProps = {
    onClick: () => setOpen(o => !o),
    "aria-expanded": open,
    "aria-haspopup": "menu" as const,
  };

  return (
    <div className={s.menuWrap} ref={ref} onClick={e => e.stopPropagation()}>
      {trigger ? (
        trigger(triggerProps)
      ) : (
        <IconButton label={label} size="sm" aria-controls={id} {...triggerProps}>
          <IconMore />
        </IconButton>
      )}
      {open && pos && createPortal(
        <div
          ref={menuRef}
          className={cx(s.menu, s.menuFloating)}
          style={pos}
          role="menu"
          id={id}
          onClick={e => e.stopPropagation()}
        >
          {visible.map((item, i) =>
            "separator" in item ? (
              <div key={i} className={s.menuSep} role="separator" />
            ) : (
              <button
                key={i}
                type="button"
                role="menuitem"
                className={cx(s.menuItem, item.danger && s.menuItemDanger)}
                onClick={() => {
                  setOpen(false);
                  item.onSelect();
                }}
              >
                {item.icon}
                {item.label}
              </button>
            ),
          )}
        </div>,
        document.body,
      )}
    </div>
  );
}

/* ── Key/value, stat, meter ─────────────────────────────────────────── */
export function KeyValue({ items }: { items: { label: ReactNode; value: ReactNode; hidden?: boolean }[] }) {
  return (
    <dl className={s.kv}>
      {items
        .filter(i => !i.hidden)
        .map((i, idx) => (
          <div key={idx} style={{ display: "contents" }}>
            <dt>{i.label}</dt>
            <dd>{i.value}</dd>
          </div>
        ))}
    </dl>
  );
}

export function Stat({ value, label }: { value: ReactNode; label: ReactNode }) {
  return (
    <div className={s.stat}>
      <span className={s.statValue}>{value}</span>
      <span className={s.statLabel}>{label}</span>
    </div>
  );
}

export function Meter({ value, color, label }: { value: number; color: string; label?: string }) {
  const pct = Math.max(0, Math.min(1, value));
  return (
    <div className={s.meter}>
      <div className={s.meterTrack} role="meter" aria-valuemin={0} aria-valuemax={100} aria-valuenow={Math.round(pct * 100)} aria-label={label}>
        <div className={s.meterFill} style={{ width: `${pct * 100}%`, background: color }} />
      </div>
      <span className={s.meterValue}>{Math.round(pct * 100)}%</span>
    </div>
  );
}

export { s as uiStyles };
export * from "./Dialog";
export * from "./Toast";
export * from "./Combobox";
