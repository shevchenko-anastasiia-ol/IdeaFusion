import { useCallback, useEffect, useMemo, useState } from "react";
import { Link } from "react-router-dom";
import { useAuth } from "../context/AuthContext";
import { authApi, REALM_ROLES, type RealmRole } from "../api/auth";
import type { UserDto } from "../api/types";
import { api } from "../api/client";
import { Page } from "../layout/AppShell";
import { usePageMeta } from "../layout/PageMeta";
import { Avatar, ButtonLink, EmptyState, ErrorState, Input, Loading, Menu, PageHeader, Pagination, Select, useDialogs, useToast } from "../ui";
import { IconSearch, IconTrash, IconUsers } from "../ui/icons";
import { plural } from "../lib/plural";
import sh from "./shared.module.css";
import s from "./AllUsersPage.module.css";

const ROLE_LABELS: Record<string, string> = { author: "Автор", moderator: "Модератор", administrator: "Адміністратор" };
const PAGE_SIZE = 15;

/**
 * Grants or revokes a realm role. The API is the enforcement point: it checks the
 * administrator policy and writes an audit entry for every change, so the control
 * only has to be honest about what it asked for.
 */
function RoleSelect({ user, onChanged }: { user: UserDto; onChanged: (userId: string, roles: string[]) => void }) {
  const { confirm } = useDialogs();
  const toast = useToast();
  const [busy, setBusy] = useState(false);
  const held = user.roles ?? [];
  const current = REALM_ROLES.filter(r => r === "administrator" || r === "moderator").find(r => held.includes(r));

  async function apply(role: RealmRole | "") {
    if (!role || role === current) return;
    const ok = await confirm({
      title: "Змінити роль?",
      message: `${user.fullName ?? user.userName ?? user.email} отримає роль «${ROLE_LABELS[role]}». Зміну буде записано в журнал.`,
      confirmLabel: "Змінити",
    });
    if (!ok) return;
    setBusy(true);
    try {
      const superseded = current && current !== role ? [current] : [];
      if (superseded.length) await authApi.revokeRoles(user.id, { roles: superseded });
      const result = await authApi.grantRoles(user.id, { roles: [role] });
      onChanged(user.id, result.roles);
      toast.success("Роль змінено");
    } catch (e) {
      toast.fromError(e, "Помилка зміни ролі");
    } finally {
      setBusy(false);
    }
  }

  return (
    <Select aria-label={`Роль ${user.email}`} className={s.roleSelect} value={current ?? ""} disabled={busy} onChange={e => void apply(e.target.value as RealmRole | "")}>
      <option value="">Без додаткової ролі</option>
      {REALM_ROLES.map(r => (
        <option key={r} value={r}>
          {ROLE_LABELS[r]}
        </option>
      ))}
    </Select>
  );
}

export default function AllUsersPage() {
  usePageMeta("Користувачі", [{ label: "Модерація", to: "/moderation" }]);
  const { user: me, isAdministrator } = useAuth();
  const { confirm } = useDialogs();
  const toast = useToast();
  const [users, setUsers] = useState<UserDto[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [page, setPage] = useState(1);
  const [query, setQuery] = useState("");

  const load = useCallback(() => {
    setLoading(true);
    setError(null);
    api
      .get<UserDto[]>("/api/users")
      .then(setUsers)
      .catch((e: Error) => setError(e.message))
      .finally(() => setLoading(false));
  }, []);

  useEffect(load, [load]);

  const handleRolesChanged = useCallback((userId: string, roles: string[]) => {
    setUsers(prev => prev.map(u => (u.id === userId ? { ...u, roles } : u)));
  }, []);

  async function handleDeleteUser(u: UserDto) {
    const ok = await confirm({ title: "Видалити користувача?", message: `${u.email} буде видалено назавжди.`, confirmLabel: "Видалити", danger: true });
    if (!ok) return;
    try {
      await api.delete<void>(`/api/users/${u.id}`);
      setUsers(prev => prev.filter(x => x.id !== u.id));
      toast.success("Користувача видалено");
    } catch (e) {
      toast.fromError(e, "Помилка видалення");
    }
  }

  const visible = useMemo(() => {
    const q = query.trim().toLowerCase();
    return users
      .filter(u => u.id !== me?.id)
      .filter(u => !q || [u.fullName, u.userName, u.email, u.specialization].some(v => v?.toLowerCase().includes(q)));
  }, [users, me?.id, query]);

  const totalPages = Math.max(1, Math.ceil(visible.length / PAGE_SIZE));
  const safePage = Math.min(page, totalPages);
  const pageUsers = visible.slice((safePage - 1) * PAGE_SIZE, safePage * PAGE_SIZE);

  return (
    <Page>
      <PageHeader title="Користувачі" description="Ролі та акаунти платформи. Кожна зміна ролі записується в журнал аудиту." />

      <div className={sh.toolbar}>
        <div className={s.search}>
          <IconSearch size={16} />
          <Input
            aria-label="Фільтр користувачів"
            placeholder="Ім'я, логін або email"
            value={query}
            onChange={e => {
              setQuery(e.target.value);
              setPage(1);
            }}
          />
        </div>
        <span className={sh.mono}>{loading ? "" : `${visible.length} ${plural(visible.length, ["користувач", "користувачі", "користувачів"])}`}</span>
      </div>

      {loading && <Loading />}
      {!loading && error && <ErrorState message={error} onRetry={load} />}
      {!loading && !error && visible.length === 0 && <EmptyState icon={<IconUsers />} title="Нікого не знайдено" />}
      {!loading && !error && pageUsers.length > 0 && (
        <>
          <div className={sh.tableWrap}>
            <table className={`${sh.table} ${sh.tableStack}`}>
              <thead>
                <tr>
                  <th>користувач</th>
                  <th>email</th>
                  <th>ролі</th>
                  <th>реєстрація</th>
                  <th aria-label="Дії" />
                </tr>
              </thead>
              <tbody>
                {pageUsers.map(u => {
                  const name = u.fullName ?? u.userName ?? u.email.split("@")[0];
                  const isAdmin = u.roles?.includes("administrator") ?? false;
                  return (
                    <tr key={u.id}>
                      <td data-label="користувач">
                        <Link to={`/users/${u.id}`} className={s.user}>
                          <Avatar name={name} src={u.avatarUrl} seed={u.id} size={30} />
                          <span className={s.userText}>
                            <span className={s.userName}>{name}</span>
                            {u.specialization && <span className={sh.mono}>{u.specialization}</span>}
                          </span>
                        </Link>
                      </td>
                      <td data-label="email" className={s.email}>
                        {u.email}
                      </td>
                      <td data-label="ролі">{isAdministrator ? <RoleSelect user={u} onChanged={handleRolesChanged} /> : (u.roles ?? []).map(r => ROLE_LABELS[r] ?? r).join(", ")}</td>
                      <td data-label="реєстрація" className={sh.mono}>
                        {u.createdAt ? new Date(u.createdAt).toLocaleDateString("uk-UA") : "—"}
                      </td>
                      <td className={s.actions}>
                        <ButtonLink to={`/team/invite-to-team?userId=${u.id}`} size="sm" variant="ghost">
                          Запросити
                        </ButtonLink>
                        {/* Nobody edits their own roles from this list, and the last administrator guard lives on the API. */}
                        {isAdministrator && !isAdmin && (
                          <Menu items={[{ label: "Видалити користувача", icon: <IconTrash size={16} />, danger: true, onSelect: () => void handleDeleteUser(u) }]} />
                        )}
                      </td>
                    </tr>
                  );
                })}
              </tbody>
            </table>
          </div>
          <Pagination page={safePage} totalPages={totalPages} onChange={setPage} />
        </>
      )}
    </Page>
  );
}
