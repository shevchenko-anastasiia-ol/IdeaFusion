import { useCallback, useEffect, useMemo, useState } from "react";
import { Link, useSearchParams } from "react-router-dom";
import { collaborationRequestsApi, teamsApi } from "../api/teams";
import type { CollaborationRequestEntity, TeamEntity } from "../api/types";
import { requestStatusKey, timeAgo } from "../api/types";
import { useAuth } from "../context/AuthContext";
import { Page } from "../layout/AppShell";
import { usePageMeta } from "../layout/PageMeta";
import { Avatar, Button, ButtonLink, EmptyState, ErrorState, Loading, PageHeader, Status, Tabs, useToast } from "../ui";
import { IconArrowLeft, IconInbox } from "../ui/icons";
import { REQUEST_LABEL, requestTone } from "../lib/status";
import sh from "./shared.module.css";
import s from "./RequestList.module.css";

type Filter = "all" | "new" | "accepted" | "rejected";

export default function JoinRequestsListPage() {
  const [params] = useSearchParams();
  const { user } = useAuth();
  const toast = useToast();
  const teamId = params.get("teamId") ?? "";
  const [filter, setFilter] = useState<Filter>("all");
  const [requests, setRequests] = useState<CollaborationRequestEntity[]>([]);
  const [team, setTeam] = useState<TeamEntity | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [busy, setBusy] = useState<string | null>(null);

  usePageMeta("Запити на вступ", [
    { label: "Команди", to: "/collaborations" },
    ...(teamId ? [{ label: team?.name ?? "Команда", to: `/teams/${teamId}` }] : []),
  ]);

  const load = useCallback(() => {
    if (!teamId) {
      setLoading(false);
      return;
    }
    setLoading(true);
    setError(null);
    teamsApi.getById(teamId).then(setTeam).catch(() => setTeam(null));
    collaborationRequestsApi
      .getByTeam(teamId)
      .then(setRequests)
      .catch(e => setError(e?.message ?? "Помилка завантаження запитів"))
      .finally(() => setLoading(false));
  }, [teamId]);

  useEffect(load, [load]);

  async function respond(r: CollaborationRequestEntity, accept: boolean) {
    if (!user) return;
    setBusy(r.id);
    try {
      if (accept) await collaborationRequestsApi.accept(r.id, { requestId: r.id });
      else await collaborationRequestsApi.reject(r.id, { requestId: r.id });
      setRequests(prev => prev.map(x => (x.id === r.id ? { ...x, status: accept ? 1 : 2 } : x)));
      toast.success(accept ? "Учасника додано до команди" : "Запит відхилено");
    } catch (err) {
      toast.fromError(err, "Не вдалося обробити запит");
    } finally {
      setBusy(null);
    }
  }

  const counts = useMemo(() => {
    const c = { new: 0, accepted: 0, rejected: 0, cancelled: 0 };
    requests.forEach(r => c[requestStatusKey(r.status)]++);
    return c;
  }, [requests]);

  const filtered = requests.filter(r => filter === "all" || requestStatusKey(r.status) === filter);

  if (!teamId) {
    return (
      <Page width="narrow">
        <PageHeader title="Запити на вступ" />
        <EmptyState icon={<IconInbox />} title="Команду не вказано" text="Відкрийте запити зі сторінки своєї команди." action={<ButtonLink to="/collaborations">Мої команди</ButtonLink>} />
      </Page>
    );
  }

  return (
    <Page width="narrow">
      <PageHeader
        title="Запити на вступ"
        description={team ? `Хто хоче долучитися до «${team.name}».` : undefined}
        actions={
          <ButtonLink to={`/teams/${teamId}`} variant="ghost" icon={<IconArrowLeft size={16} />}>
            До команди
          </ButtonLink>
        }
      />
      <Tabs
        label="Статус"
        value={filter}
        onChange={setFilter}
        items={[
          { value: "all", label: "Усі", count: requests.length },
          { value: "new", label: "Нові", count: counts.new },
          { value: "accepted", label: "Прийняті", count: counts.accepted },
          { value: "rejected", label: "Відхилені", count: counts.rejected },
        ]}
      />
      <div className={s.body}>
        {loading && <Loading />}
        {!loading && error && <ErrorState message={error} onRetry={load} />}
        {!loading && !error && filtered.length === 0 && <EmptyState icon={<IconInbox />} title="Запитів немає" />}
        {!loading && !error && filtered.length > 0 && (
          <ul className={s.list}>
            {filtered.map(r => {
              const key = requestStatusKey(r.status);
              const name = r.fromUsername ?? "Користувач";
              return (
                <li key={r.id} className={s.item} data-state={key}>
                  <div className={s.head}>
                    <Avatar name={name} src={r.fromAvatarUrl} seed={r.fromUserId} size={40} />
                    <div className={s.headText}>
                      <Link to={`/users/${r.fromUserId}`} className={s.title}>
                        {name}
                      </Link>
                      <span className={sh.mono}>
                        роль: {r.role} · {timeAgo(r.createdAt)}
                      </span>
                    </div>
                    <Status tone={requestTone(key)}>{REQUEST_LABEL[key].toLowerCase()}</Status>
                  </div>
                  {r.message && <p className={s.message}>{r.message}</p>}
                  {key === "new" && (
                    <div className={s.actions}>
                      <Button size="sm" variant="primary" loading={busy === r.id} onClick={() => void respond(r, true)}>
                        Прийняти
                      </Button>
                      <Button size="sm" variant="ghost" disabled={busy === r.id} onClick={() => void respond(r, false)}>
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
