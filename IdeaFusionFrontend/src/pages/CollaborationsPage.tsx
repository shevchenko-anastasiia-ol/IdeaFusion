import { useCallback, useEffect, useMemo, useState } from "react";
import { Link } from "react-router-dom";
import { useAuth } from "../context/AuthContext";
import { teamsApi, groupInvitationsApi } from "../api/teams";
import type { TeamEntity, GroupInvitationEntity, PagedResult } from "../api/types";
import { teamStatusLabel, invitationStatusKey, timeAgo } from "../api/types";
import { Page } from "../layout/AppShell";
import { usePageMeta } from "../layout/PageMeta";
import { Avatar, Button, ButtonLink, EmptyState, ErrorState, Loading, PageHeader, SectionTitle, Segmented, useToast } from "../ui";
import { IconArrowRight, IconInbox, IconPlus, IconUsers } from "../ui/icons";
import TeamRow from "../components/TeamRow";
import sh from "./shared.module.css";
import s from "./CollaborationsPage.module.css";

type Filter = "all" | "Активна" | "У пошуку" | "Завершена";

export default function CollaborationsPage() {
  usePageMeta("Команди");
  const { user, loading: authLoading } = useAuth();
  const toast = useToast();
  const [filter, setFilter] = useState<Filter>("all");
  const [myTeams, setMyTeams] = useState<TeamEntity[]>([]);
  const [allTeams, setAllTeams] = useState<TeamEntity[]>([]);
  const [invitations, setInvitations] = useState<GroupInvitationEntity[]>([]);
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
      teamsApi.getMine(),
      teamsApi.getPaged(1, 100).then((r: PagedResult<TeamEntity>) => (Array.isArray(r) ? r : r.items ?? [])),
      groupInvitationsApi.getMine(),
    ])
      .then(([my, all, inv]) => {
        setMyTeams(my as TeamEntity[]);
        setAllTeams(all as TeamEntity[]);
        setInvitations(inv as GroupInvitationEntity[]);
      })
      .catch((e: Error) => setError(e.message))
      .finally(() => setLoading(false));
  }, [user]);

  useEffect(() => {
    if (authLoading) return;
    load();
  }, [authLoading, load]);

  const teamById = useMemo(() => {
    const map: Record<string, TeamEntity> = {};
    [...allTeams, ...myTeams].forEach(t => (map[t.id] = t));
    return map;
  }, [myTeams, allTeams]);

  const filteredTeams = myTeams.filter(t => filter === "all" || teamStatusLabel(t.status) === filter);
  const pending = invitations.filter(i => invitationStatusKey(i.status) === "new");

  async function respond(inv: GroupInvitationEntity, accept: boolean) {
    if (!user) return;
    setBusy(inv.id);
    try {
      if (accept) await groupInvitationsApi.accept(inv.id, { invitationId: inv.id });
      else await groupInvitationsApi.decline(inv.id, { invitationId: inv.id });
      setInvitations(prev => prev.map(i => (i.id === inv.id ? { ...i, status: accept ? 1 : 2 } : i)));
      toast.success(accept ? "Запрошення прийнято" : "Запрошення відхилено");
      if (accept) load();
    } catch (err) {
      toast.fromError(err, "Не вдалося відповісти на запрошення");
    } finally {
      setBusy(null);
    }
  }

  if (!authLoading && !user) {
    return (
      <Page width="narrow">
        <PageHeader title="Команди" />
        <EmptyState
          icon={<IconUsers />}
          title="Увійдіть, щоб бачити свої команди"
          text="Тут ваші команди та запрошення до нових проєктів. Переглядати команди платформи можна і без входу."
          action={
            <div className={sh.inline}>
              <ButtonLink to="/login" variant="primary">Увійти</ButtonLink>
              <ButtonLink to="/teams">Усі команди</ButtonLink>
            </div>
          }
        />
      </Page>
    );
  }

  return (
    <Page>
      <PageHeader
        title="Команди"
        description="Ваші команди та запрошення до спільних проєктів."
        actions={
          <>
            <ButtonLink to="/teams" variant="ghost">
              Усі команди
            </ButtonLink>
            <ButtonLink to="/team/new" variant="primary" icon={<IconPlus size={16} />}>
              Нова команда
            </ButtonLink>
          </>
        }
      />

      <div className={sh.withRail}>
        <section>
          <div className={sh.toolbar}>
            <SectionTitle>мої команди</SectionTitle>
            <Segmented
              label="Фільтр за статусом"
              value={filter}
              onChange={setFilter}
              items={[
                { value: "all", label: "Усі" },
                { value: "Активна", label: "Активні" },
                { value: "У пошуку", label: "У пошуку" },
                { value: "Завершена", label: "Завершені" },
              ]}
            />
          </div>
          {loading && <Loading />}
          {!loading && error && <ErrorState message={error} onRetry={load} />}
          {!loading && !error && myTeams.length === 0 && (
            <EmptyState
              icon={<IconUsers />}
              title="Ви ще не в жодній команді"
              text="Створіть свою команду або приєднайтесь до існуючої."
              action={
                <div className={sh.inline}>
                  <ButtonLink to="/team/new" variant="primary">Створити команду</ButtonLink>
                  <ButtonLink to="/teams">Знайти команду</ButtonLink>
                </div>
              }
            />
          )}
          {!loading && !error && myTeams.length > 0 && filteredTeams.length === 0 && <EmptyState title="Немає команд з таким статусом" />}
          {!loading && !error && filteredTeams.length > 0 && (
            <ul className={sh.list}>
              {filteredTeams.map(team => (
                <li key={team.id}>
                  <TeamRow team={team} />
                </li>
              ))}
            </ul>
          )}
        </section>

        <aside className={sh.rail}>
          <section className={s.invites}>
            <div className={sh.between}>
              <SectionTitle>запрошення {pending.length > 0 && <span className={s.count}>{pending.length}</span>}</SectionTitle>
              <Link to="/team/invitations" className={s.all}>
                усі <IconArrowRight size={13} />
              </Link>
            </div>
            {loading ? (
              <p className={sh.faint}>…</p>
            ) : pending.length === 0 ? (
              <div className={s.none}>
                <IconInbox size={18} />
                <span>Нових запрошень немає</span>
              </div>
            ) : (
              <ul className={s.inviteList}>
                {pending.slice(0, 4).map(inv => (
                  <li key={inv.id} className={s.invite}>
                    <div className={s.inviteHead}>
                      <Avatar name={teamById[inv.teamId]?.name ?? "Команда"} src={teamById[inv.teamId]?.avatarUrl} seed={inv.teamId} size={30} square />
                      <div className={s.inviteText}>
                        <Link to={`/teams/${inv.teamId}`} className={s.inviteTeam}>
                          {teamById[inv.teamId]?.name ?? "Команда"}
                        </Link>
                        <span className={sh.mono}>
                          {inv.role} · {timeAgo(inv.createdAt)}
                        </span>
                      </div>
                    </div>
                    {inv.message && <p className={s.inviteMsg}>{inv.message}</p>}
                    <div className={s.inviteActions}>
                      <Button size="sm" variant="primary" loading={busy === inv.id} onClick={() => void respond(inv, true)}>
                        Прийняти
                      </Button>
                      <Button size="sm" variant="ghost" disabled={busy === inv.id} onClick={() => void respond(inv, false)}>
                        Відхилити
                      </Button>
                    </div>
                  </li>
                ))}
              </ul>
            )}
          </section>
        </aside>
      </div>
    </Page>
  );
}
