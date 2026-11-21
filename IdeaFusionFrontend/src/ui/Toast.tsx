import { createContext, useCallback, useContext, useMemo, useRef, useState, type ReactNode } from "react";
import { createPortal } from "react-dom";
import s from "./ui.module.css";
import { IconAlert, IconCheck, IconClose, IconInfo } from "./icons";

type ToastTone = "success" | "error" | "info";
interface ToastItem {
  id: number;
  tone: ToastTone;
  text: string;
}

interface ToastApi {
  success: (text: string) => void;
  error: (text: string) => void;
  info: (text: string) => void;
  /** Shows the message of a caught error, falling back to `fallback`. */
  fromError: (err: unknown, fallback: string) => void;
}

const ToastContext = createContext<ToastApi | null>(null);

export function ToastProvider({ children }: { children: ReactNode }) {
  const [items, setItems] = useState<ToastItem[]>([]);
  const seq = useRef(0);

  const dismiss = useCallback((id: number) => setItems(list => list.filter(t => t.id !== id)), []);

  const push = useCallback(
    (tone: ToastTone, text: string) => {
      const id = ++seq.current;
      setItems(list => [...list.slice(-3), { id, tone, text }]);
      window.setTimeout(() => dismiss(id), tone === "error" ? 7000 : 4000);
    },
    [dismiss],
  );

  const api = useMemo<ToastApi>(
    () => ({
      success: t => push("success", t),
      error: t => push("error", t),
      info: t => push("info", t),
      fromError: (err, fallback) => push("error", err instanceof Error && err.message ? err.message : fallback),
    }),
    [push],
  );

  return (
    <ToastContext.Provider value={api}>
      {children}
      {createPortal(
        <div className={s.toasts} aria-live="polite">
          {items.map(t => (
            <div key={t.id} className={`${s.toast} ${s[`toast-${t.tone}`]}`} role={t.tone === "error" ? "alert" : "status"}>
              {t.tone === "success" ? <IconCheck size={16} /> : t.tone === "error" ? <IconAlert size={16} /> : <IconInfo size={16} />}
              <span className={s.toastText}>{t.text}</span>
              <button type="button" className={`${s.iconBtn} ${s.iconBtnSm}`} onClick={() => dismiss(t.id)} aria-label="Закрити">
                <IconClose size={14} />
              </button>
            </div>
          ))}
        </div>,
        document.body,
      )}
    </ToastContext.Provider>
  );
}

// eslint-disable-next-line react-refresh/only-export-components
export function useToast(): ToastApi {
  const ctx = useContext(ToastContext);
  if (!ctx) throw new Error("useToast must be used inside ToastProvider");
  return ctx;
}
