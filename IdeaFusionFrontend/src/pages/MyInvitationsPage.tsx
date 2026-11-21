import { useCallback, useEffect, useMemo, useState } from "react";
import { Link } from "react-router-dom";
import { groupInvitationsApi, teamsApi } from "../api/teams";
import type { GroupInvitationEntity, TeamEntity, PagedResult } from "../api/types";
import { invitationStatusKey, timeAgo } from "../api/types";
import { useAuth } from "../context/AuthContext";
import { Page } from "../layout/AppShell";
import { usePageMeta } from "../layout/PageMeta";
import { Avatar, Button, EmptyState, ErrorState, Loading, PageHeader, Status, Tabs, useToast } from "../ui";
import { IconInbox } from "../ui/icons";
import { useUsers, userDisplayName } from "../lib/users";
import { REQUEST_LABEL, requestTone } from "../lib/status";
import sh from "./shared.module.css";
import s from "./RequestList.module.css";

type Filter = "all" | "new" | "accepted" | "rejected";

export default function MyInvitationsPage() {
  usePageMeta("Мої запрошення", [{ label: "Команди", to: "/collaborations" }]);
  const { user, loading: authLoading } = useAuth();
  const toast = useToast();
  const [filter, setFilter] = useState<Filter>("all");
  const [invitations, setInvitations] = useState<GroupInvitationEntity[]>([]);
  const [teams, setTeams] = useState<Record<string, TeamEntity>>({});
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [busy, setBusy] = useState<string | null>(null);

  const load = useCallback(() => {
    if (!user) {
      setLoading(false);
      return;
    }
    setLoading(true);
    setError(null);
    Promise.all([
      groupInvitationsApi.getMine(),
      teamsApi
        .getPaged(1, 200)
        .then((r: PagedResult<TeamEntity>) => (Array.isArray(r) ? r : r.items ?? []))
        .catch(() => [] as TeamEntity[]),
    ])
      .then(([inv, list]) => {
        setInvitations(inv);
        const map: Record<string, TeamEntity> = {};
        list.forEach(t => (map[t.id] = t));
        setTeams(map);
      })
      .catch((e: Error) => setError(e.message))
      .finally(() => setLoading(false));
  }, [user]);

  useEffect(() => {
    if (authLoading) return;
    load();
  }, [authLoading, load]);

  const inviters = useUsers(invitations.map(i => i.invitedByUserId));

  async function respond(inv: GroupInvitationEntity, accept: boolean) {
    if (!user) return;
    setBusy(inv.id);
    try {
      if (accept) await groupInvitationsApi.accept(inv.id, { invitationId: inv.id });
      else await groupInvitationsApi.decline(inv.id, { invitationId: inv.id });
      setInvitations(prev => prev.map(x => (x.id === inv.id ? { ...x, status: accept ? 1 : 2 } : x)));
      toast.success(accept ? "Ви приєдналися до команди" : "Запрошення відхилено");
    } catch (err) {
      toast.fromError(err, "Не вдалося відповісти на запрошення");
    } finally {
      setBusy(null);
    }
  }

  const counts = useMemo(() => {
    const c = { new: 0, accepted: 0, rejected: 0 };
    invitations.forEach(i => c[invitationStatusKey(i.status)]++);
    return c;
  }, [invitations]);

  const filtered = invitations.filter(inv => filter === "all" || invitationStatusKey(inv.status) === filter);

  return (
    <Page width="narrow">
      <PageHeader title="Мої запрошення" description="Команди, які запросили вас долучитися." />
      <Tabs
        label="Статус"
        value={filter}
        onChange={setFilter}
        items={[
          { value: "all", label: "Усі", count: invitations.length },
          { value: "new", label: "Нові", count: counts.new },
          { value: "accepted", label: "Прийняті", count: counts.accepted },
          { value: "rejected", label: "Відхилені", count: counts.rejected },
        ]}
      />
      <div className={s.body}>
        {loading && <Loading />}
        {!loading && error && <ErrorState message={error} onRetry={load} />}
        {!loading && !error && filtered.length === 0 && <EmptyState icon={<IconInbox />} title="Запрошень немає" />}
        {!loading && !error && filtered.length > 0 && (
          <ul className={s.list}>
            {filtered.map(inv => {
              const key = invitationStatusKey(inv.status);
              const inviter = inviters[inv.invitedByUserId];
              const inviterName = userDisplayName(inviter, "Учасник команди");
              const team = teams[inv.teamId];
              return (
                <li key={inv.id} className={s.item} data-state={key}>
                  <div className={s.head}>
                    <Avatar name={team?.name ?? "Команда"} src={team?.avatarUrl} seed={inv.teamId} size={40} square />
                    <div className={s.headText}>
                      <Link to={`/teams/${inv.teamId}`} className={s.title}>
                        {team?.name ?? "Команда"}
                      </Link>
                      <span className={sh.mono}>
                        роль: {inv.role} · {timeAgo(inv.createdAt)}
                      </span>
                    </div>
                    <Status tone={requestTone(key)}>{REQUEST_LABEL[key].toLowerCase()}</Status>
                  </div>
                  <div className={s.from}>
                    <span className={sh.mono}>від</span>
                    <Link to={`/users/${inv.invitedByUserId}`} className={s.person}>
                      <Avatar name={inviterName} src={inviter?.avatarUrl} seed={inv.invitedByUserId} size={22} />
                      {inviterName}
                    </Link>
                  </div>
                  {inv.message && <p className={s.message}>{inv.message}</p>}
                  {key === "new" && (
                    <div className={s.actions}>
                      <Button size="sm" variant="primary" loading={busy === inv.id} onClick={() => void respond(inv, true)}>
                        Прийняти
                      </Button>
                      <Button size="sm" variant="ghost" disabled={busy === inv.id} onClick={() => void respond(inv, false)}>
                        Відхилити
                      </Button>
                    </div>
                  )}
                </li>
              );
            })}
          </ul>
        )}
      </div>
    </Page>
  );
}
