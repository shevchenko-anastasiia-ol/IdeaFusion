import { useState } from "react";
import { Link } from "react-router-dom";
import type { AggregatorTeamPostDto, CollaborationRequestEntity, TeamEntity, TeamMemberEntity, TeamVacancyEntity } from "../../api/types";
import { TEAM_PERMISSIONS, teamStatusLabel, timeAgo, vacancyStatusKey, vacancyStatusLabel } from "../../api/types";
import { Avatar, Badge, Button, ButtonLink, EmptyState, Field, IconButton, Input, KeyValue, Menu, Status, Switch, Dialog, Notice, cx } from "../../ui";
import { IconBriefcase, IconFeed, IconInbox, IconPlus, IconRefresh, IconTrash, IconKey } from "../../ui/icons";
import PostTile from "../../components/PostTile";
import VacancyFormDialog, { type VacancyFormValues } from "../../components/VacancyFormDialog";
import { teamStatusTone, vacancyTone } from "../../lib/status";
import sh from "../shared.module.css";
import s from "./TeamDetailPage.module.css";

function effectivePermissions(member: TeamMemberEntity): string[] {
  const perms = member.permissions ?? [];
  return perms.length === 0 ? ["canPublishPosts"] : perms;
}

/* ── Overview ───────────────────────────────────────────────────────── */
export function TeamAbout({ team, leaderEmail }: { team: TeamEntity; leaderEmail?: string }) {
  const leader = team.members.find(m => m.role === "Owner");
  return (
    <div className={sh.stack}>
      <section className={sh.formSection}>
        <h2 className={s.blockTitle}>Про команду</h2>
        {team.description ? <p className={cx(sh.prewrap, s.about)}>{team.description}</p> : <p className={sh.faint}>Опис ще не додано.</p>}
        <KeyValue
          items={[
            { label: "Статус", value: <Status tone={teamStatusTone(team.status)}>{teamStatusLabel(team.status).toLowerCase()}</Status> },
            { label: "Категорія", value: team.category || "—" },
            { label: "Теги", value: team.tags.length ? <span className={s.tagLine}>{team.tags.map(t => `#${t}`).join("  ")}</span> : "—" },
            { label: "Створено", value: new Date(team.createdAt).toLocaleDateString("uk-UA"), hidden: !team.createdAt },
          ]}
        />
      </section>
      {leader && (
        <section className={sh.formSection}>
          <h2 className={s.blockTitle}>Контакти лідера</h2>
          <Link to={`/users/${leader.user.userId}`} className={s.person}>
            <Avatar name={leader.user.username} src={leader.user.avatarUrl} seed={leader.user.userId} size={36} />
            <span className={s.personText}>
              <span className={s.personName}>{leader.user.username}</span>
              <span className={sh.mono}>{leaderEmail ?? "email доступний після входу"}</span>
            </span>
          </Link>
        </section>
      )}
    </div>
  );
}

/* ── Roles ──────────────────────────────────────────────────────────── */
export function TeamRoles({
  team,
  canEdit,
  canApply,
  onAdd,
}: {
  team: TeamEntity;
  canEdit: boolean;
  canApply: boolean;
  onAdd: (role: string) => Promise<void>;
}) {
  const [open, setOpen] = useState(false);
  const [role, setRole] = useState("");
  const [busy, setBusy] = useState(false);
  const [err, setErr] = useState<string | null>(null);

  async function submit() {
    if (!role.trim()) return;
    setBusy(true);
    setErr(null);
    try {
      await onAdd(role.trim());
      setRole("");
      setOpen(false);
    } catch (e) {
      setErr(e instanceof Error ? e.message : "Помилка");
    } finally {
      setBusy(false);
    }
  }

  return (
    <section className={sh.formSection}>
      <div className={sh.between}>
        <h2 className={s.blockTitle}>Кого шукаємо</h2>
        {canEdit && (
          <Button size="sm" variant="ghost" icon={<IconPlus size={15} />} onClick={() => setOpen(true)}>
            Додати роль
          </Button>
        )}
      </div>
      {team.requiredRoles.length === 0 ? (
        <p className={sh.faint}>Відкритих ролей немає.</p>
      ) : (
        <ul className={s.roles}>
          {team.requiredRoles.map((r, i) => (
            <li key={`${r.role}-${i}`} className={s.role}>
              <span className={s.roleName}>{r.role}</span>
              {r.description && <span className={sh.mono}>{r.description}</span>}
              {canApply && (
                <ButtonLink to={`/team/join-request?teamId=${team.id}`} size="sm" variant="ghost">
                  Відгукнутися
                </ButtonLink>
              )}
            </li>
          ))}
        </ul>
      )}
      <Dialog
        open={open}
        onClose={() => setOpen(false)}
        title="Нова роль"
        footer={
          <>
            <Button variant="ghost" onClick={() => setOpen(false)}>
              Скасувати
            </Button>
            <Button variant="primary" loading={busy} disabled={!role.trim()} onClick={() => void submit()}>
              Додати
            </Button>
          </>
        }
      >
        <Field label="Назва ролі" htmlFor="new-role">
          <Input
            id="new-role"
            data-autofocus
            placeholder="Напр.: Концепт-художник"
            value={role}
            onChange={e => setRole(e.target.value)}
            onKeyDown={e => {
              if (e.key === "Enter") {
                e.preventDefault();
                void submit();
              }
            }}
          />
        </Field>
        {err && <Notice tone="error">{err}</Notice>}
      </Dialog>
    </section>
  );
}

/* ── Members ────────────────────────────────────────────────────────── */
export function TeamMembers({
  team,
  isOwner,
  canRemove,
  currentUserId,
  onRemove,
  onPermissions,
}: {
  team: TeamEntity;
  isOwner: boolean;
  canRemove: boolean;
  currentUserId?: string;
  onRemove: (memberId: string) => void;
  onPermissions: (memberId: string, perms: string[]) => void;
}) {
  const [editing, setEditing] = useState<string | null>(null);
  return (
    <ul className={sh.list}>
      {team.members.map(m => {
        const owner = m.role === "Owner";
        const effective = effectivePermissions(m);
        return (
          <li key={m.user.userId}>
            <div className={sh.row}>
              <Link to={`/users/${m.user.userId}`} className={s.person}>
                <Avatar name={m.user.username} src={m.user.avatarUrl} seed={m.user.userId} size={36} />
                <span className={s.personText}>
                  <span className={s.personName}>
                    {m.user.username}
                    {m.user.userId === currentUserId && <span className={sh.mono}> (ви)</span>}
                  </span>
                  <span className={sh.mono}>
                    {owner ? "лідер" : m.position || (m.role === "Leader" ? "заступник" : "учасник")} · з {new Date(m.joinedAt).toLocaleDateString("uk-UA")}
                  </span>
                </span>
              </Link>
              <div className={sh.rowActions}>
                {owner ? (
                  <Badge tone="accent">лідер</Badge>
                ) : (
                  <>
                    {isOwner && (
                      <Button size="sm" variant={editing === m.user.userId ? "secondary" : "ghost"} icon={<IconKey size={14} />} onClick={() => setEditing(editing === m.user.userId ? null : m.user.userId)} aria-expanded={editing === m.user.userId}>
                        Доступ
                      </Button>
                    )}
                    {canRemove && (
                      <Menu items={[{ label: "Видалити з команди", icon: <IconTrash size={16} />, danger: true, onSelect: () => onRemove(m.user.userId) }]} />
                    )}
                  </>
                )}
              </div>
            </div>
            {isOwner && !owner && editing === m.user.userId && (
              <div className={s.perms}>
                <span className={sh.mono}>права учасника — зберігаються одразу</span>
                <div className={s.permGrid}>
                  {TEAM_PERMISSIONS.map(p => {
                    const checked = effective.includes(p.key);
                    return (
                      <Switch
                        key={p.key}
                        checked={checked}
                        label={p.label}
                        onChange={() => onPermissions(m.user.userId, checked ? effective.filter(k => k !== p.key) : [...effective, p.key])}
                      />
                    );
                  })}
                </div>
              </div>
            )}
          </li>
        );
      })}
    </ul>
  );
}

/* ── Vacancies ──────────────────────────────────────────────────────── */
export function TeamVacancies({
  vacancies,
  isOwner,
  onCreate,
  onUpdate,
  onClose,
}: {
  vacancies: TeamVacancyEntity[];
  isOwner: boolean;
  onCreate: (v: VacancyFormValues) => Promise<void>;
  onUpdate: (vacancyId: string, v: VacancyFormValues) => Promise<void>;
  onClose: (vacancyId: string) => void;
}) {
  const [creating, setCreating] = useState(false);
  const [editing, setEditing] = useState<TeamVacancyEntity | null>(null);

  const openCount = vacancies.filter(v => vacancyStatusKey(v.status) === "open").length;

  return (
    <>
      <div className={sh.toolbar}>
        <span className={sh.mono}>відкритих: {openCount} з {vacancies.length}</span>
        {isOwner && (
          <Button size="sm" variant="primary" icon={<IconPlus size={15} />} onClick={() => setCreating(true)}>
            Створити вакансію
          </Button>
        )}
      </div>
      {vacancies.length === 0 ? (
        <EmptyState icon={<IconBriefcase />} title="Вакансій поки немає" text={isOwner ? "Опишіть роль, яку шукаєте, — вакансія з'явиться в загальному каталозі." : undefined} />
      ) : (
        <ul className={sh.list}>
          {vacancies.map(v => {
            const isOpen = vacancyStatusKey(v.status) === "open";
            const bare = !v.description && !v.requirements;
            return (
              <li key={v.id} className={sh.row}>
                <Link to={`/vacancies/${v.id}`} className={sh.rowMain} style={{ color: "inherit", textDecoration: "none" }}>
                  <span className={sh.rowTitle}>{v.role}</span>
                  {v.description && <span className={sh.rowSub}>{v.description}</span>}
                  <span className={sh.mono}>
                    {timeAgo(v.createdAt)}
                    {v.requirements ? " · є вимоги" : ""}
                    {isOwner && isOpen && bare ? " · без опису" : ""}
                  </span>
                </Link>
                <div className={sh.rowActions}>
                  <Status tone={vacancyTone(v.status)}>{vacancyStatusLabel(v.status).toLowerCase()}</Status>
                  {isOwner && isOpen && (
                    <>
                      <Button size="sm" variant={bare ? "primary" : "ghost"} onClick={() => setEditing(v)}>
                        {bare ? "Додати опис" : "Редагувати"}
                      </Button>
                      <Button size="sm" variant="ghost" onClick={() => onClose(v.id)}>
                        Закрити
                      </Button>
                    </>
                  )}
                </div>
              </li>
            );
          })}
        </ul>
      )}
      <VacancyFormDialog open={creating} onClose={() => setCreating(false)} onSubmit={onCreate} />
      <VacancyFormDialog
        open={!!editing}
        initial={editing ? { role: editing.role, description: editing.description, requirements: editing.requirements } : null}
        onClose={() => setEditing(null)}
        onSubmit={values => onUpdate(editing!.id, values)}
      />
    </>
  );
}

/* ── Portfolio ──────────────────────────────────────────────────────── */
export function TeamPortfolio({ posts, teamName, canPublish }: { posts: AggregatorTeamPostDto[]; teamName: string; canPublish: boolean }) {
  return (
    <>
      {posts.length === 0 ? (
        <EmptyState
          icon={<IconFeed />}
          title="Публікацій поки немає"
          action={
            canPublish && (
              <ButtonLink to="/posts/new" variant="primary" icon={<IconPlus size={15} />}>
                Опублікувати пост
              </ButtonLink>
            )
          }
        />
      ) : (
        <div className={sh.tiles}>
          {posts.map((p, i) => (
            <PostTile key={`${p.postId}-${i}`} postId={p.postId} title={p.title} mediaUrls={p.mediaUrls ?? []} caption={p.author?.username ?? teamName} />
          ))}
        </div>
      )}
    </>
  );
}

/* ── Join requests ──────────────────────────────────────────────────── */
export function TeamRequests({
  teamId,
  requests,
  error,
  onRefresh,
  onAccept,
  onReject,
}: {
  teamId: string;
  requests: CollaborationRequestEntity[];
  error: string | null;
  onRefresh: () => void;
  onAccept: (id: string) => void;
  onReject: (id: string) => void;
}) {
  return (
    <>
      <div className={sh.toolbar}>
        <span className={sh.mono}>очікують: {requests.length}</span>
        <div className={sh.inline}>
          <IconButton label="Оновити список" size="sm" onClick={onRefresh}>
            <IconRefresh size={15} />
          </IconButton>
          <ButtonLink to={`/team/requests?teamId=${teamId}`} size="sm" variant="ghost">
            Уся історія запитів
          </ButtonLink>
        </div>
      </div>
      {error && <Notice tone="error">{error}</Notice>}
      {!error && requests.length === 0 && <EmptyState icon={<IconInbox />} title="Нових запитів немає" />}
      {requests.length > 0 && (
        <ul className={sh.list}>
          {requests.map(r => {
            const name = r.fromUsername ?? "Користувач";
            return (
              <li key={r.id} className={sh.row}>
                <Avatar name={name} src={r.fromAvatarUrl} seed={r.fromUserId} size={36} />
                <div className={sh.rowMain}>
                  <Link to={`/users/${r.fromUserId}`} className={sh.rowTitle}>
                    {name}
                  </Link>
                  <span className={sh.mono}>
                    роль: {r.role} · {timeAgo(r.createdAt)}
                  </span>
                  {r.message && <span className={sh.rowSub}>{r.message}</span>}
                </div>
                <div className={cx(sh.rowActions, sh.rowActionsStretch)}>
                  <Button size="sm" variant="primary" onClick={() => onAccept(r.id)}>
                    Прийняти
                  </Button>
                  <Button size="sm" variant="ghost" onClick={() => onReject(r.id)}>
                    Відхилити
                  </Button>
                </div>
              </li>
            );
          })}
        </ul>
      )}
    </>
  );
}
