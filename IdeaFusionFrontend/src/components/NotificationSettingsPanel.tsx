import { useEffect, useState } from "react";
import { notificationsApi, type NotificationSettingsDto } from "../api/notifications";
import { useAuth } from "../context/AuthContext";
import { Loading, Notice, Switch, useToast } from "../ui";
import s from "./NotificationSettingsPanel.module.css";

const ROWS: { key: keyof NotificationSettingsDto; title: string; text: string }[] = [
  {
    key: "emailInvitations",
    title: "Запрошення в команди",
    text: "Вас запросили до команди, або на ваше запрошення відповіли.",
  },
  {
    key: "emailRequests",
    title: "Запити й відгуки",
    text: "Хтось хоче приєднатися до вашої команди чи відгукнувся на вакансію; вам відповіли на запит або відгук.",
  },
  {
    key: "emailMessages",
    title: "Особисті повідомлення",
    text: "Новий запит на листування, а також нові повідомлення (не частіше ніж раз на 5 хвилин для однієї розмови).",
  },
  {
    key: "emailComments",
    title: "Коментарі",
    text: "Хтось прокоментував ваш пост або відповів на ваш коментар.",
  },
];

/** The email switches; each change is saved straight away. */
export default function NotificationSettingsPanel() {
  const { user } = useAuth();
  const toast = useToast();
  const [settings, setSettings] = useState<NotificationSettingsDto | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [saving, setSaving] = useState<keyof NotificationSettingsDto | null>(null);

  useEffect(() => {
    notificationsApi
      .get()
      .then(setSettings)
      .catch((e: Error) => setError(e.message || "Не вдалося завантажити налаштування"));
  }, []);

  async function toggle(key: keyof NotificationSettingsDto, value: boolean) {
    if (!settings) return;
    const previous = settings;
    const next = { ...settings, [key]: value };
    setSettings(next);
    setSaving(key);
    try {
      setSettings(await notificationsApi.save(next));
    } catch (e: unknown) {
      setSettings(previous);
      toast.fromError(e, "Не вдалося зберегти");
    } finally {
      setSaving(null);
    }
  }

  return (
    <section className={s.panel}>
      <h2 className={s.title}>Сповіщення на пошту</h2>
      <p className={s.lead}>
        Листи приходять на <b>{user?.email}</b>. Вимкніть те, про що не хочете отримувати листи — у застосунку все одно буде видно.
      </p>
      {error && <Notice tone="error">{error}</Notice>}
      {!settings && !error && <Loading />}
      {settings && (
        <ul className={s.list}>
          {ROWS.map(r => (
            <li key={r.key} className={s.row}>
              <div className={s.text}>
                <span className={s.rowTitle}>{r.title}</span>
                <span className={s.rowText}>{r.text}</span>
              </div>
              <Switch checked={settings[r.key]} disabled={saving === r.key} onChange={v => void toggle(r.key, v)} />
            </li>
          ))}
        </ul>
      )}
    </section>
  );
}
