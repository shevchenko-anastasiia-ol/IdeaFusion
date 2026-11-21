import { useEffect, useState, type ReactNode } from "react";
import { authApi } from "../api/auth";
import { resetCredentialsUrl } from "../auth/keycloak";
import { useAuth } from "../context/AuthContext";
import { Page } from "../layout/AppShell";
import { usePageMeta } from "../layout/PageMeta";
import { Notice, PageHeader, Spinner, Status, uiStyles } from "../ui";
import { IconDevices, IconExternal, IconKey, IconShield, IconUser } from "../ui/icons";
import sh from "./shared.module.css";
import s from "./SecurityPage.module.css";

interface Row {
  icon: ReactNode;
  title: string;
  body: string;
  status?: ReactNode;
  action: { label: string; href: string };
}

/**
 * Password and two-factor are owned by Keycloak, not by this app. The page links
 * into the account console rather than reimplementing credential flows, which is
 * also the only way 2FA gets configured correctly.
 */
export default function SecurityPage() {
  usePageMeta("Безпека");
  const { user, accountConsoleUrl } = useAuth();
  const [twoFactorConfigured, setTwoFactorConfigured] = useState<boolean | null>(null);
  const [consoleUrl, setConsoleUrl] = useState<string>(accountConsoleUrl);
  const [error, setError] = useState<string | null>(null);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    let cancelled = false;
    setLoading(true);
    authApi
      .twoFactorStatus()
      .then(status => {
        if (cancelled) return;
        setTwoFactorConfigured(status.twoFactorConfigured);
        if (status.accountConsoleUrl) setConsoleUrl(status.accountConsoleUrl);
        setError(null);
      })
      .catch(e => {
        if (!cancelled) setError(e instanceof Error ? e.message : "Не вдалося перевірити статус 2FA");
      })
      .finally(() => {
        if (!cancelled) setLoading(false);
      });
    return () => {
      cancelled = true;
    };
  }, []);

  const rows: Row[] = [
    {
      icon: <IconKey />,
      title: "Пароль",
      body: "Зміна пароля відбувається у консолі облікового запису Keycloak. Після зміни всі сесії, крім поточної, завершаються.",
      action: { label: "Змінити пароль", href: resetCredentialsUrl },
    },
    {
      icon: <IconShield />,
      title: "Двофакторна автентифікація",
      body: "Додайте застосунок-аутентифікатор (TOTP). Ключ буде показано один раз — збережіть його в менеджері паролів.",
      status: loading ? <Spinner /> : twoFactorConfigured === null ? null : <Status tone={twoFactorConfigured ? "teal" : "amber"}>{twoFactorConfigured ? "увімкнено" : "не налаштовано"}</Status>,
      action: { label: "Налаштувати 2FA", href: `${consoleUrl}security` },
    },
    {
      icon: <IconDevices />,
      title: "Активні сесії",
      body: "Перегляд і завершення сесій, у яких ви увійшли на інших пристроях.",
      action: { label: "Керувати сесіями", href: `${consoleUrl}sessions` },
    },
    {
      icon: <IconUser />,
      title: "Персональні дані та акаунт",
      body: "Ім'я, email, аватар і видалення акаунта.",
      action: { label: "Відкрити профіль", href: `${consoleUrl}` },
    },
  ];

  return (
    <Page width="narrow">
      <PageHeader title="Безпека акаунта" description="Обліковим записом керує Keycloak. Пароль, 2FA та сесії змінюються там — єдине джерело автентифікації для всіх сервісів." />
      {error && (
        <div style={{ marginBottom: "var(--sp-4)" }}>
          <Notice tone="warning">{error}</Notice>
        </div>
      )}
      <ul className={sh.list}>
        {rows.map(row => (
          <li key={row.title} className={s.row}>
            <span className={s.icon}>{row.icon}</span>
            <div className={s.text}>
              <div className={sh.inline}>
                <h2 className={s.title}>{row.title}</h2>
                {row.status}
              </div>
              <p className={sh.muted}>{row.body}</p>
            </div>
            <a className={`${uiStyles.btn} ${uiStyles.secondary} ${uiStyles.sm}`} href={row.action.href} target="_blank" rel="noreferrer noopener">
              {row.action.label} <IconExternal size={14} />
            </a>
          </li>
        ))}
      </ul>
      {user && (
        <p className={s.foot}>
          Ви увійшли як <strong>{user.email}</strong>
        </p>
      )}
    </Page>
  );
}
