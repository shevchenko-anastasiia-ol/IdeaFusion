import { useCallback, useEffect, useState } from "react";
import { Link } from "react-router-dom";
import { useAuth } from "../context/AuthContext";
import { aggregatorApi } from "../api/aggregator";
import type { UserDashboardDto } from "../api/types";
import { teamStatusLabel, timeAgo } from "../api/types";
import { Page } from "../layout/AppShell";
import { usePageMeta } from "../layout/PageMeta";
import { Avatar, ButtonLink, EmptyState, ErrorState, Panel, Skeleton, Stat, Status } from "../ui";
import { IconArrowRight, IconFeed, IconPlus, IconUsers } from "../ui/icons";
import { teamStatusTone } from "../lib/status";
import { plural } from "../lib/plural";
import sh from "./shared.module.css";
import s from "./DashboardPage.module.css";

export default function DashboardPage() {
  usePageMeta("Дашборд");
  const { user, contentUserId, loading: authLoading } = useAuth();
  const [dash, setDash] = useState<UserDashboardDto | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const displayName = user?.fullName || user?.userName || user?.email?.split("@")[0] || "Користувач";
  const today = new Date().toLocaleDateString("uk-UA", { weekday: "long", day: "numeric", month: "long" });

  const load = useCallback(() => {
    if (!user) {
      setLoading(false);
      return;
    }
    setLoading(true);
    setError(null);
    // Without a content author record there are simply no personal posts yet (id 0); saved
    // posts, teams and pending answers are read from the token and still belong here.
    aggregatorApi
      .getUserDashboard(contentUserId ?? 0, user.id)
      .then(setDash)
      .catch((e: Error) => setError(e.message))
      .finally(() => setLoading(false));
  }, [contentUserId, user]);

  useEffect(() => {
    if (authLoading) return;
    load();
  }, [authLoading, load]);

  const num = (v: number | undefined) => (loading ? "…" : String(v ?? 0));
  const pendingInvites = dash?.pendingInvitations ?? [];
  const pendingRequests = dash?.pendingRequests ?? [];

  return (
    <Page>
      <header className={s.hello}>
        <Avatar name={displayName} src={user?.avatarUrl} seed={user?.id} size={56} />
        <div className={s.helloText}>
          <span className={sh.mono}>{today}</span>
          <h1 className={s.greeting}>Привіт, {displayName}</h1>
          <span className={sh.muted}>{user?.specialization || user?.email}</span>
        </div>
        <div className={s.helloActions}>
          <ButtonLink to="/posts/new" variant="primary" icon={<IconPlus size={16} />}>
            Новий пост
          </ButtonLink>
          <ButtonLink to="/team/new" icon={<IconUsers size={16} />}>
            Нова команда
          </ButtonLink>
        </div>
      </header>

      {error && (
        <div style={{ marginBottom: "var(--sp-5)" }}>
          <ErrorState message={error} onRetry={load} />
        </div>
      )}

      <div className={sh.stats}>
        <Stat value={num(dash?.totalPosts)} label="моїх постів" />
        <Stat value={num(dash?.totalSavedPosts)} label="збережених" />
        <Stat value={num(dash?.totalTeams)} label="колаборацій" />
        <Stat value={num(pendingInvites.length + pendingRequests.length)} label="очікують відповіді" />
      </div>

      <div className={s.grid}>
        <Panel
          title="Мої команди"
          padded={false}
          actions={
            <ButtonLink to="/teams" size="sm" variant="ghost">
              Усі команди <IconArrowRight size={14} />
            </ButtonLink>
          }
        >
          {loading ? (
            <div className={s.pad}>
              <Skeleton height={40} />
            </div>
          ) : (dash?.myTeams ?? []).length === 0 ? (
            <EmptyState icon={<IconUsers />} title="Ви ще не в жодній команді" action={<ButtonLink to="/teams">Знайти команду</ButtonLink>} />
          ) : (
            <ul className={s.rows}>
              {(dash?.myTeams ?? []).map(t => (
                <li key={t.teamId}>
                  <Link to={`/teams/${t.teamId}`} className={s.row}>
                    <Avatar name={t.name} seed={t.teamId} size={32} square />
                    <span className={s.rowMain}>
                      <span className={s.rowTitle}>{t.name}</span>
                      <span className={sh.mono}>
                        {t.category} · {t.membersCount} {plural(t.membersCount, ["учасник", "учасники", "учасників"])}
                      </span>
                    </span>
                    <Status tone={teamStatusTone(t.status)}>{teamStatusLabel(t.status).toLowerCase()}</Status>
                  </Link>
                </li>
              ))}
            </ul>
          )}
          <div className={s.panelFoot}>
            <ButtonLink to="/team/new" size="sm" variant="ghost" icon={<IconPlus size={15} />}>
              Створити нову команду
            </ButtonLink>
          </div>
        </Panel>

        <div className={sh.stack}>
          <Panel
            title="Очікують відповіді"
            padded={false}
            actions={
              pendingInvites.length > 0 && (
                <ButtonLink to="/team/invitations" size="sm" variant="ghost">
                  Запрошення <IconArrowRight size={14} />
                </ButtonLink>
              )
            }
          >
            {loading ? (
              <div className={s.pad}>
                <Skeleton height={40} />
              </div>
            ) : pendingInvites.length + pendingRequests.length === 0 ? (
              <p className={s.padMuted}>Немає запитів і запрошень, що чекають на вас.</p>
            ) : (
              <ul className={s.rows}>
                {pendingInvites.map(inv => (
                  <li key={inv.invitationId}>
                    <Link to="/team/invitations" className={s.row}>
                      <span className={s.kind}>запрошення</span>
                      <span className={s.rowMain}>
                        <span className={s.rowTitle}>{inv.teamName}</span>
                        <span className={sh.mono}>
                          {inv.role} · від {inv.invitedByUsername} · {timeAgo(inv.createdAt)}
                        </span>
                      </span>
                    </Link>
                  </li>
                ))}
                {pendingRequests.map(r => (
                  <li key={r.requestId}>
                    <Link to={`/team/requests?teamId=${r.teamId}`} className={s.row}>
                      <span className={s.kind}>запит</span>
                      <span className={s.rowMain}>
                        <span className={s.rowTitle}>{r.fromUsername} → {r.teamName}</span>
                        <span className={sh.mono}>
                          {r.role} · {timeAgo(r.createdAt)}
                        </span>
                      </span>
                    </Link>
                  </li>
                ))}
              </ul>
            )}
          </Panel>

          <Panel
            title="Мої останні пости"
            padded={false}
            actions={
              <ButtonLink to="/personal" size="sm" variant="ghost">
                Портфоліо <IconArrowRight size={14} />
              </ButtonLink>
            }
          >
            {loading ? (
              <div className={s.pad}>
                <Skeleton height={40} />
              </div>
            ) : (dash?.myPosts ?? []).length === 0 ? (
              <EmptyState icon={<IconFeed />} title="Постів ще немає" action={<ButtonLink to="/posts/new">Створити пост</ButtonLink>} />
            ) : (
              <ul className={s.rows}>
                {(dash?.myPosts ?? []).slice(0, 5).map(p => (
                  <li key={p.postId}>
                    <Link to={`/posts/${p.postId}`} className={s.row}>
                      <span className={s.rowMain}>
                        <span className={s.rowTitle}>{p.title}</span>
                        <span className={sh.mono}>
                          {timeAgo(p.createdAt)} · {p.likesCount} вподоб. · {p.commentsCount} комент.
                        </span>
                      </span>
                    </Link>
                  </li>
                ))}
              </ul>
            )}
          </Panel>
        </div>
      </div>
    </Page>
  );
}
