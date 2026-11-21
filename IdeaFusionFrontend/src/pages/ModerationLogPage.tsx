import { useCallback, useEffect, useState } from "react";
import { auditApi, type AuditCategory, type AuditLogEntryDto } from "../api/audit";
import { useAuth } from "../context/AuthContext";
import { Page } from "../layout/AppShell";
import { usePageMeta } from "../layout/PageMeta";
import { Button, Checkbox, EmptyState, ErrorState, Field, Loading, PageHeader, Select, Status, type Tone } from "../ui";
import { IconRefresh, IconShield } from "../ui/icons";
import sh from "./shared.module.css";
import s from "./ModerationLogPage.module.css";

const CATEGORY_TONE: Record<AuditCategory, Tone> = {
  Security: "red",
  User: "amber",
  Content: "blue",
  Moderation: "accent",
  System: "neutral",
};

/** Roles may legitimately be part of the action string, so match on a prefix. */
const ACTION_FILTERS = [
  { label: "Усі дії", value: "" },
  { label: "Видалення постів", value: "post.moderate.delete" },
  { label: "Видалення коментарів", value: "comment.moderate.delete" },
  { label: "Зміни ролей", value: "user.role" },
] as const;

function formatWhen(iso: string): string {
  const d = new Date(iso);
  if (Number.isNaN(d.getTime())) return iso;
  return d.toLocaleString("uk-UA", { day: "2-digit", month: "2-digit", year: "numeric", hour: "2-digit", minute: "2-digit" });
}

/**
 * The moderation slice of the audit trail.
 *
 * Administrators additionally get a switch to the full trail, which is a separate
 * endpoint; moderators only ever see the content slice and the service filters it
 * server-side regardless of what this page asks for.
 */
export default function ModerationLogPage() {
  usePageMeta("Журнал модерації");
  const { isAdministrator } = useAuth();
  const [entries, setEntries] = useState<AuditLogEntryDto[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [action, setAction] = useState<string>("");
  const [scope, setScope] = useState<"moderation" | "all">("moderation");
  const [onlyModerators, setOnlyModerators] = useState(false);

  const load = useCallback(async () => {
    setLoading(true);
    setError(null);
    try {
      const query = { limit: 200, ...(action ? { action } : {}) };
      const data = scope === "all" && isAdministrator ? await auditApi.getAll(query) : await auditApi.getModeration(query);
      setEntries(data);
    } catch (e) {
      setError(e instanceof Error ? e.message : "Помилка завантаження");
      setEntries([]);
    } finally {
      setLoading(false);
    }
  }, [action, scope, isAdministrator]);

  useEffect(() => {
    void load();
  }, [load]);

  const visible = onlyModerators ? entries.filter(e => e.category === "Moderation" || e.category === "Content") : entries;

  return (
    <Page width="wide">
      <PageHeader
        title="Журнал модерації"
        description="Хто, що і коли змінив. Записи незмінні."
      />

      <div className={s.toolbar}>
        <Field label="Дія" htmlFor="ml-action">
          <Select id="ml-action" value={action} onChange={e => setAction(e.target.value)}>
            {ACTION_FILTERS.map(f => (
              <option key={f.value} value={f.value}>
                {f.label}
              </option>
            ))}
          </Select>
        </Field>
        {isAdministrator && (
          <Field label="Обсяг" htmlFor="ml-scope">
            <Select id="ml-scope" value={scope} onChange={e => setScope(e.target.value as "moderation" | "all")}>
              <option value="moderation">Контент і модерація</option>
              <option value="all">Увесь журнал</option>
            </Select>
          </Field>
        )}
        <div className={s.checkbox}>
          <Checkbox label="Лише модераторські рішення" checked={onlyModerators} onChange={e => setOnlyModerators(e.target.checked)} />
        </div>
        <Button icon={<IconRefresh size={15} />} onClick={() => void load()} loading={loading}>
          Оновити
        </Button>
      </div>

      {error && <ErrorState message={error} onRetry={() => void load()} />}
      {loading && entries.length === 0 && !error && <Loading />}
      {!loading && !error && visible.length === 0 && <EmptyState icon={<IconShield />} title="Записів немає" />}

      {visible.length > 0 && (
        <div className={sh.tableWrap}>
          <table className={`${sh.table} ${sh.tableStack}`}>
            <thead>
              <tr>
                <th>час</th>
                <th>категорія</th>
                <th>дія</th>
                <th>хто</th>
                <th>об'єкт</th>
              </tr>
            </thead>
            <tbody>
              {visible.map(entry => (
                <tr key={entry.id}>
                  <td data-label="час" className={s.when}>
                    {formatWhen(entry.createdAt)}
                  </td>
                  <td data-label="категорія">
                    <Status tone={CATEGORY_TONE[entry.category] ?? "neutral"}>{entry.category.toLowerCase()}</Status>
                  </td>
                  <td data-label="дія">
                    <div>
                      <code className={s.action}>{entry.action}</code>
                      {entry.details && <div className={s.details}>{entry.details}</div>}
                    </div>
                  </td>
                  <td data-label="хто">{entry.actorName ?? <span className={sh.mono}>{entry.actorId}</span>}</td>
                  <td data-label="об'єкт">
                    <span>
                      <span className={sh.mono}>{entry.targetType}</span> {entry.targetName ? `«${entry.targetName}»` : <span className={sh.mono}>#{entry.targetId}</span>}
                    </span>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}
    </Page>
  );
}
