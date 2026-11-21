import { createContext, useCallback, useContext, useEffect, useRef, useState, type ReactNode } from "react";
import { createPortal } from "react-dom";
import s from "./ui.module.css";
import { IconClose } from "./icons";

function cls(...p: (string | false | undefined)[]) {
  return p.filter(Boolean).join(" ");
}

const FOCUSABLE = 'button:not([disabled]), [href], input:not([disabled]), select:not([disabled]), textarea:not([disabled]), [tabindex]:not([tabindex="-1"])';

/**
 * Modal dialog: portal, Escape/overlay to close, focus moved in and trapped,
 * focus restored to the opener on close. Bottom sheet on small screens.
 */
export function Dialog({
  open,
  onClose,
  title,
  children,
  footer,
  wide,
  closeOnOverlay = true,
}: {
  open: boolean;
  onClose: () => void;
  title: ReactNode;
  children?: ReactNode;
  footer?: ReactNode;
  wide?: boolean;
  closeOnOverlay?: boolean;
}) {
  const ref = useRef<HTMLDivElement>(null);
  const onCloseRef = useRef(onClose);
  onCloseRef.current = onClose;

  useEffect(() => {
    if (!open) return;
    const opener = document.activeElement as HTMLElement | null;
    const node = ref.current;
    const first = node?.querySelector<HTMLElement>("[data-autofocus]") ?? node?.querySelectorAll<HTMLElement>(FOCUSABLE)[1] ?? node?.querySelector<HTMLElement>(FOCUSABLE);
    first?.focus();
    const prevOverflow = document.body.style.overflow;
    document.body.style.overflow = "hidden";

    function onKey(e: KeyboardEvent) {
      if (e.key === "Escape") {
        e.stopPropagation();
        onCloseRef.current();
      } else if (e.key === "Tab" && node) {
        const items = Array.from(node.querySelectorAll<HTMLElement>(FOCUSABLE));
        if (items.length === 0) return;
        const firstEl = items[0];
        const lastEl = items[items.length - 1];
        if (e.shiftKey && document.activeElement === firstEl) {
          e.preventDefault();
          lastEl.focus();
        } else if (!e.shiftKey && document.activeElement === lastEl) {
          e.preventDefault();
          firstEl.focus();
        }
      }
    }
    document.addEventListener("keydown", onKey);
    return () => {
      document.removeEventListener("keydown", onKey);
      document.body.style.overflow = prevOverflow;
      opener?.focus?.();
    };
  }, [open]);

  if (!open) return null;

  return createPortal(
    <div
      className={s.overlay}
      onMouseDown={e => {
        if (closeOnOverlay && e.target === e.currentTarget) onClose();
      }}
    >
      <div ref={ref} className={cls(s.dialog, wide && s.dialogWide)} role="dialog" aria-modal="true" aria-labelledby="dialog-title">
        <div className={s.dialogHeader}>
          <h2 id="dialog-title" className={s.dialogTitle}>
            {title}
          </h2>
          <button type="button" className={cls(s.iconBtn, s.iconBtnSm)} onClick={onClose} aria-label="Закрити">
            <IconClose size={16} />
          </button>
        </div>
        {children != null && <div className={s.dialogBody}>{children}</div>}
        {footer && <div className={s.dialogFooter}>{footer}</div>}
      </div>
    </div>,
    document.body,
  );
}

/* ── Confirm / prompt as promises ───────────────────────────────────── */
export interface ConfirmOptions {
  title: string;
  message?: ReactNode;
  confirmLabel?: string;
  cancelLabel?: string;
  danger?: boolean;
}

export interface PromptOptions {
  title: string;
  label?: string;
  defaultValue?: string;
  confirmLabel?: string;
}

type DialogState =
  | { kind: "confirm"; opts: ConfirmOptions; resolve: (v: boolean) => void }
  | { kind: "prompt"; opts: PromptOptions; resolve: (v: string | null) => void };

interface DialogApi {
  confirm: (opts: ConfirmOptions) => Promise<boolean>;
  prompt: (opts: PromptOptions) => Promise<string | null>;
}

const DialogContext = createContext<DialogApi | null>(null);

export function DialogProvider({ children }: { children: ReactNode }) {
  const [state, setState] = useState<DialogState | null>(null);
  const [value, setValue] = useState("");

  const confirm = useCallback(
    (opts: ConfirmOptions) => new Promise<boolean>(resolve => setState({ kind: "confirm", opts, resolve })),
    [],
  );
  const prompt = useCallback(
    (opts: PromptOptions) =>
      new Promise<string | null>(resolve => {
        setValue(opts.defaultValue ?? "");
        setState({ kind: "prompt", opts, resolve });
      }),
    [],
  );

  function close(result: boolean) {
    if (!state) return;
    if (state.kind === "confirm") state.resolve(result);
    else state.resolve(result ? value : null);
    setState(null);
  }

  return (
    <DialogContext.Provider value={{ confirm, prompt }}>
      {children}
      <Dialog
        open={!!state}
        onClose={() => close(false)}
        title={state?.opts.title ?? ""}
        footer={
          state && (
            <>
              <button type="button" className={cls(s.btn, s.ghost)} onClick={() => close(false)}>
                {state.kind === "confirm" ? state.opts.cancelLabel ?? "Скасувати" : "Скасувати"}
              </button>
              <button
                type="button"
                data-autofocus
                className={cls(s.btn, state.kind === "confirm" && state.opts.danger ? s.danger : s.primary)}
                onClick={() => close(true)}
              >
                {state.opts.confirmLabel ?? (state.kind === "confirm" ? "Підтвердити" : "Зберегти")}
              </button>
            </>
          )
        }
      >
        {state?.kind === "confirm" && state.opts.message}
        {state?.kind === "prompt" && (
          <form
            onSubmit={e => {
              e.preventDefault();
              close(true);
            }}
            className={s.field}
          >
            {state.opts.label && (
              <label className={s.label} htmlFor="prompt-input">
                {state.opts.label}
              </label>
            )}
            <input id="prompt-input" className={s.input} value={value} onChange={e => setValue(e.target.value)} />
          </form>
        )}
      </Dialog>
    </DialogContext.Provider>
  );
}

// eslint-disable-next-line react-refresh/only-export-components
export function useDialogs(): DialogApi {
  const ctx = useContext(DialogContext);
  if (!ctx) throw new Error("useDialogs must be used inside DialogProvider");
  return ctx;
}
