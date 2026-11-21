import { useEffect, useState } from "react";
import { Link, Navigate } from "react-router-dom";
import { useAuth } from "../context/AuthContext";
import { resetCredentialsUrl } from "../auth/keycloak";
import { Button, Notice } from "../ui";
import { IconGoogle, IconLogin } from "../ui/icons";
import s from "./LoginPage.module.css";

const CAPABILITIES = [
  ["01", "Публікуйте роботи — дизайн, музику, арт, відео"],
  ["02", "Збирайте команди та шукайте учасників на ролі"],
  ["03", "Спілкуйтеся з командою в реальному часі"],
  ["04", "Кожне зображення перевіряється на збіги під час публікації"],
] as const;

/**
 * Authentication is Keycloak's job. There is no local credential form and no
 * register tab: accounts are created on first sign-in (including the Google
 * one), so self-registration is a realm setting rather than an app feature.
 */
export default function LoginPage() {
  const { login, loginWithGoogle, isAuthenticated, initError } = useAuth();
  const [busy, setBusy] = useState<null | "keycloak" | "google">(null);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    document.title = "Вхід · IdeaFusion";
  }, []);

  async function start(who: "keycloak" | "google") {
    setError(null);
    setBusy(who);
    try {
      if (who === "google") await loginWithGoogle();
      else await login();
    } catch (ex) {
      setError(ex instanceof Error ? ex.message : "Не вдалося увійти");
      setBusy(null);
    }
  }

  // Already signed in: the silent SSO check found a session, so there is nothing
  // to do on this page. Rendered rather than navigated, so no render-phase side effect.
  if (isAuthenticated) return <Navigate to="/home" replace />;

  return (
    <div className={s.page}>
      <section className={s.intro} aria-hidden="false">
        <Link to="/home" className={s.brand}>
          <img className={s.mark} src="/logo.svg" alt="" aria-hidden="true" />
          <span>Idea Fusion</span>
        </Link>
        <div className={s.introBody}>
          <p className={s.kicker}>Платформа творчих колаборацій</p>
          <h1 className={s.headline}>Місце, де ідеї знаходять своїх людей.</h1>
          <ul className={s.caps}>
            {CAPABILITIES.map(([key, text]) => (
              <li key={key}>
                <span className={s.capKey}>{key}</span>
                <span>{text}</span>
              </li>
            ))}
          </ul>
        </div>
      </section>

      <section className={s.panelWrap}>
        <div className={s.panel}>
          <div className={s.panelHead}>
            <h2 className={s.title}>Вхід</h2>
            <p className={s.sub}>Увійдіть, щоб створювати пости, команди та чати.</p>
          </div>

          {error && <Notice tone="error">{error}</Notice>}
          {initError && <Notice tone="error">Помилка автентифікації: {initError}</Notice>}

          <Button variant="primary" size="lg" block loading={busy === "keycloak"} disabled={busy !== null} icon={<IconLogin size={18} />} onClick={() => void start("keycloak")}>
            {busy === "keycloak" ? "Переходимо…" : "Увійти"}
          </Button>

          <div className={s.sep}>
            <span />
            або
            <span />
          </div>

          <Button size="lg" block loading={busy === "google"} disabled={busy !== null} icon={<IconGoogle size={18} />} onClick={() => void start("google")}>
            {busy === "google" ? "Переходимо…" : "Продовжити з Google"}
          </Button>

          <div className={s.links}>
            <a href={resetCredentialsUrl} target="_blank" rel="noreferrer noopener">
              Забули пароль?
            </a>
          </div>
          <p className={s.foot}>Акаунт створюється автоматично при першому вході.</p>
        </div>
      </section>
    </div>
  );
}
