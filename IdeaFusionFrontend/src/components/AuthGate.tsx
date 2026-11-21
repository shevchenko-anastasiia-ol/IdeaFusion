import { useEffect, useState } from "react";
import { register } from "../auth/keycloak";
import { useAuth } from "../context/AuthContext";
import { Logo } from "../layout/AppShell";
import { Button, Notice } from "../ui";
import { IconGoogle, IconLogin, IconPlus } from "../ui/icons";
import s from "./AuthGate.module.css";

const PERKS = [
  "Дивіться роботи дизайнерів, музикантів, художників і аніматорів",
  "Збирайте команду під свій проєкт або приєднуйтесь до чужого",
  "Відгукуйтесь на вакансії та пишіть авторам напряму",
] as const;

/**
 * Covers a page that needs an account: the page stays visible but blurred underneath, and
 * the card offers to sign in or sign up. It cannot be dismissed; signing in is the way through.
 */
export default function AuthGate({ title = "Приєднуйтесь до IdeaFusion" }: { title?: string }) {
  const { login, loginWithGoogle } = useAuth();
  const [busy, setBusy] = useState<null | "login" | "register" | "google">(null);
  const [error, setError] = useState<string | null>(null);

  // The page underneath must not scroll while the gate is up.
  useEffect(() => {
    const prev = document.body.style.overflow;
    document.body.style.overflow = "hidden";
    return () => {
      document.body.style.overflow = prev;
    };
  }, []);

  async function go(kind: "login" | "register" | "google") {
    setBusy(kind);
    setError(null);
    try {
      if (kind === "login") await login();
      else if (kind === "google") await loginWithGoogle();
      else await register();
    } catch {
      setError("Не вдалося відкрити сторінку входу. Спробуйте ще раз за хвилину.");
      setBusy(null);
    }
  }

  return (
    <div className={s.overlay} role="dialog" aria-modal="true" aria-labelledby="auth-gate-title">
      <div className={s.card}>
        <div className={s.brand}>
          <Logo />
        </div>
        <h2 id="auth-gate-title" className={s.title}>
          {title}
        </h2>
        <p className={s.lead}>Стрічка робіт, команди й вакансії відкриваються після входу. Створити акаунт — безкоштовно і займає хвилину.</p>
        <ul className={s.perks}>
          {PERKS.map(p => (
            <li key={p}>{p}</li>
          ))}
        </ul>
        {error && <Notice tone="error">{error}</Notice>}
        <div className={s.actions}>
          <Button variant="primary" size="lg" block icon={<IconLogin size={18} />} loading={busy === "login"} disabled={busy !== null} onClick={() => void go("login")}>
            Увійти
          </Button>
          <Button size="lg" block icon={<IconPlus size={18} />} loading={busy === "register"} disabled={busy !== null} onClick={() => void go("register")}>
            Створити акаунт
          </Button>
          <div className={s.or}>
            <span>або</span>
          </div>
          <Button variant="ghost" size="lg" block icon={<IconGoogle size={18} />} loading={busy === "google"} disabled={busy !== null} onClick={() => void go("google")}>
            Продовжити з Google
          </Button>
        </div>
      </div>
    </div>
  );
}
