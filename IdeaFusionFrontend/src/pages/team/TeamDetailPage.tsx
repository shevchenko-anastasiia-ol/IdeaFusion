import { useCallback, useEffect, useRef, useState, type FormEvent } from "react";
import { Link, useNavigate, useParams, useSearchParams } from "react-router-dom";
import { teamsApi, collaborationRequestsApi } from "../../api/teams";
import { vacanciesApi } from "../../api/vacancies";
import { aggregatorApi } from "../../api/aggregator";
import { authApi } from "../../api/auth";
import { useAuth } from "../../context/AuthContext";
import type { AggregatorTeamPostDto, CollaborationRequestEntity, TeamEntity, TeamVacancyEntity } from "../../api/types";
import { teamStatusLabel, hasPermission } from "../../api/types";
import { Page } from "../../layout/AppShell";
import { usePageMeta } from "../../layout/PageMeta";
import {
  Avatar,
  Badge,
  Button,
  ButtonLink,
  EmptyState,
  ErrorState,
  Field,
  Input,
  Loading,
  Notice,
  Select,
  Status,
  Tabs,
  Textarea,
  useDialogs,
  useToast,
  cx,
} from "../../ui";
import { IconArrowLeft, IconCheck, IconPlus, IconTrash, IconUpload, IconUsers } from "../../ui/icons";
import { teamStatusTone } from "../../lib/status";
import { plural } from "../../lib/plural";
import { useUsers } from "../../lib/users";
import { TeamAbout, TeamMembers, TeamPortfolio, TeamRequests, TeamRoles, TeamVacancies } from "./TeamSections";
import sh from "../shared.module.css";
import s from "./TeamDetailPage.module.css";

const STATUS_OPTIONS = [
  { value: 0, label: "Активна" },
  { value: 1, label: "У пошуку" },
  { value: 2, label: "Завершена" },
];

function statusNum(status: number | string): number {
  if (typeof status === "number") return status;
  if (status === "Active") return 0;
  if (status === "Searching") return 1;
  return 2;
}

type Tab = "overview" | "portfolio" | "vacancies" | "members" | "requests" | "settings";
const TABS: Tab[] = ["overview", "portfolio", "vacancies", "members", "requests", "settings"];

export default function TeamDetailPage() {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const { user, isAuthenticated } = useAuth();
  const { confirm } = useDialogs();
  const toast = useToast();
  const [searchParams, setSearchParams] = useSearchParams();

  const [team, setTeam] = useState<TeamEntity | null>(null);
  const [posts, setPosts] = useState<AggregatorTeamPostDto[]>([]);
  const [requests, setRequests] = useState<CollaborationRequestEntity[]>([]);
  const [myRequest, setMyRequest] = useState<CollaborationRequestEntity | null>(null);
  const [vacancies, setVacancies] = useState<TeamVacancyEntity[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [requestsError, setRequestsError] = useState<string | null>(null);

  // Settings form
  const [editName, setEditName] = useState("");
  const [editDesc, setEditDesc] = useState("");
  const [editCategory, setEditCategory] = useState("");
  const [editTags, setEditTags] = useState("");
  const [editStatus, setEditStatus] = useState(1);
  const [saving, setSaving] = useState(false);
  const [saveError, setSaveError] = useState<string | null>(null);
  const avatarInputRef = useRef<HTMLInputElement>(null);
  const [avatarUploading, setAvatarUploading] = useState(false);

  usePageMeta(team?.name ?? "Команда", [{ label: "Команди", to: "/collaborations" }]);

  const isOwner = !!(team && user && team.members.some(m => m.user.userId === user.id && m.role === "Owner"));
  const isMember = !!(team && user && team.members.some(m => m.user.userId === user.id));
  const canEditTeam = hasPermission(team, user?.id, "canEditTeam");
  const canManageMembers = hasPermission(team, user?.id, "canRemoveMembers");
  const canPublishPosts = hasPermission(team, user?.id, "canPublishPosts");

  const leader = team?.members.find(m => m.role === "Owner");
  const leaderUser = useUsers(isAuthenticated && leader ? [leader.user.userId] : [])[leader?.user.userId ?? ""];

  const tabParam = searchParams.get("tab") as Tab | null;
  const allowed: Record<Tab, boolean> = {
    overview: true,
    portfolio: true,
    vacancies: true,
    members: true,
    requests: canManageMembers,
    settings: canEditTeam,
  };
  const tab: Tab = tabParam && TABS.includes(tabParam) && allowed[tabParam] ? tabParam : "portfolio";
  const setTab = (t: Tab) => setSearchParams(t === "portfolio" ? {} : { tab: t }, { replace: true });

  const fillForm = (t: TeamEntity) => {
    setEditName(t.name);
    setEditDesc(t.description);
    setEditCategory(t.category);
    setEditTags(t.tags.join(", "));
    setEditStatus(statusNum(t.status));
  };

  const load = useCallback(() => {
    if (!id) return;
    setLoading(true);
    setError(null);
    setTeam(null);
    setPosts([]);
    setRequests([]);
    setMyRequest(null);
    setVacancies([]);
    Promise.all([teamsApi.getById(id), aggregatorApi.getTeamFull(id).catch(() => null), vacanciesApi.getByTeam(id, 1, 50).catch(() => null)])
      .then(([t, full, vacs]) => {
        setTeam(t);
        fillForm(t);
        if (full) setPosts(full.posts);
        if (vacs) setVacancies(vacs.items);
      })
      .catch(e => setError(e.message))
      .finally(() => setLoading(false));
  }, [id]);

  useEffect(load, [load]);

  // Does the visitor already have a pending request for this team?
  useEffect(() => {
    if (!id || !user || !team || isMember) {
      setMyRequest(null);
      return;
    }
    collaborationRequestsApi
      .getMine()
      .then(rs => setMyRequest(rs.find(r => r.teamId === id && (r.status === 0 || r.status === "Pending")) ?? null))
      .catch(() => setMyRequest(null));
  }, [id, user, team, isMember]);

  const loadRequests = useCallback(() => {
    if (!id || !canManageMembers) return;
    setRequestsError(null);
    collaborationRequestsApi
      .getByTeam(id)
      .then(rs => setRequests(rs.filter(r => r.status === 0 || r.status === "Pending")))
      .catch(e => setRequestsError(e?.message ?? "Помилка завантаження запитів"));
  }, [id, canManageMembers]);

  useEffect(loadRequests, [loadRequests]);

  async function handleSave(e: FormEvent) {
    e.preventDefault();
    if (!team || !user) return;
    setSaving(true);
    setSaveError(null);
    try {
      const tags = editTags
        .split(",")
        .map(t => t.trim())
        .filter(Boolean);
      const updated = await teamsApi.update(team.id, { teamId: team.id, name: editName, description: editDesc, category: editCategory, tags });
      if (editStatus !== statusNum(team.status)) {
        await teamsApi.setStatus(team.id, { teamId: team.id, status: editStatus });
      }
      setTeam({ ...updated, status: editStatus });
      toast.success("Зміни збережено");
    } catch (err: unknown) {
      setSaveError(err instanceof Error ? err.message : "Помилка збереження");
    } finally {
      setSaving(false);
    }
  }

  async function handleAvatarChange(e: React.ChangeEvent<HTMLInputElement>) {
    const file = e.target.files?.[0];
    if (!file || !team || !user) return;
    setAvatarUploading(true);
    try {
      const { url } = await authApi.uploadFile(file);
      const updated = await teamsApi.setAvatarUrl(team.id, { teamId: team.id, avatarUrl: url });
      setTeam(updated);
      toast.success("Аватар команди оновлено");
    } catch (err: unknown) {
      toast.fromError(err, "Помилка завантаження фото");
    } finally {
      setAvatarUploading(false);
      if (avatarInputRef.current) avatarInputRef.current.value = "";
    }
  }

  async function handleDeleteTeam() {
    if (!team) return;
    const ok = await confirm({ title: `Видалити «${team.name}»?`, message: "Цю дію не можна скасувати.", confirmLabel: "Видалити команду", danger: true });
    if (!ok) return;
    try {
      await teamsApi.deleteTeam(team.id);
      toast.success("Команду видалено");
      navigate("/team/new", { replace: true });
    } catch (err: unknown) {
      toast.fromError(err, "Помилка видалення");
    }
  }

  async function handleRemoveMember(memberId: string) {
    if (!team || !user) return;
    const member = team.members.find(m => m.user.userId === memberId);
    const ok = await confirm({ title: "Видалити учасника?", message: `${member?.user.username ?? "Учасник"} більше не матиме доступу до команди та її чату.`, confirmLabel: "Видалити", danger: true });
    if (!ok) return;
    try {
      setTeam(await teamsApi.removeMember(team.id, memberId));
      toast.success("Учасника видалено");
    } catch (err: unknown) {
      toast.fromError(err, "Помилка");
    }
  }

  async function handleAddRole(role: string) {
    if (!team || !user) return;
    setTeam(await teamsApi.addRequiredRole(team.id, { teamId: team.id, role }));
    // The server opens a vacancy for the new role; pull it in so the tab shows it.
    const vacs = await vacanciesApi.getByTeam(team.id, 1, 50).catch(() => null);
    if (vacs) setVacancies(vacs.items);
    toast.success("Роль додано — вакансію відкрито, опис можна додати у вкладці «Вакансії»");
  }

  async function handleCreateVacancy(v: { role: string; description?: string; requirements?: string }) {
    if (!team || !user) return;
    await vacanciesApi.create({ teamId: team.id, ...v });
    const vacs = await vacanciesApi.getByTeam(team.id, 1, 50);
    setVacancies(vacs.items);
    setTeam(await teamsApi.getById(team.id));
    toast.success("Вакансію створено");
  }

  async function handleUpdateVacancy(vacancyId: string, v: { role: string; description?: string; requirements?: string }) {
    if (!team || !user) return;
    await vacanciesApi.update({ vacancyId, ...v });
    const vacs = await vacanciesApi.getByTeam(team.id, 1, 50);
    setVacancies(vacs.items);
    // A renamed vacancy also renames the team's required role.
    setTeam(await teamsApi.getById(team.id));
    toast.success("Вакансію оновлено");
  }

  async function handleCloseVacancy(vacancyId: string) {
    if (!user) return;
    const ok = await confirm({ title: "Закрити вакансію?", message: "Нові відгуки на неї прийматися не будуть.", confirmLabel: "Закрити" });
    if (!ok) return;
    try {
      await vacanciesApi.close({ vacancyId });
      if (team) setVacancies((await vacanciesApi.getByTeam(team.id, 1, 50)).items);
      toast.success("Вакансію закрито");
    } catch (err: unknown) {
      toast.fromError(err, "Помилка");
    }
  }

  async function handleAcceptRequest(reqId: string) {
    if (!user || !id) return;
    try {
      await collaborationRequestsApi.accept(reqId, { requestId: reqId });
      setRequests(rs => rs.filter(r => r.id !== reqId));
      setTeam(await teamsApi.getById(id));
      toast.success("Учасника додано");
    } catch (err: unknown) {
      toast.fromError(err, "Помилка");
    }
  }

  async function handleRejectRequest(reqId: string) {
    if (!user) return;
    try {
      await collaborationRequestsApi.reject(reqId, { requestId: reqId });
      setRequests(rs => rs.filter(r => r.id !== reqId));
      toast.success("Запит відхилено");
    } catch (err: unknown) {
      toast.fromError(err, "Помилка");
    }
  }

  async function handleSavePermissions(targetMemberId: string, permissions: string[]) {
    if (!team || !user) return;
    try {
      setTeam(await teamsApi.setMemberPermissions(team.id, { teamId: team.id, targetMemberId, permissions }));
    } catch (err: unknown) {
      toast.fromError(err, "Не вдалося змінити права");
    }
  }

  if (loading) {
    return (
      <Page>
        <Loading />
      </Page>
    );
  }

  if (error || !team) {
    return (
      <Page width="narrow">
        <Link to="/teams" className={sh.backLink}>
          <IconArrowLeft size={16} /> До команд
        </Link>
        {error ? <ErrorState title="Команду не знайдено" message={error} onRetry={load} /> : <EmptyState icon={<IconUsers />} title="Команду не знайдено" />}
      </Page>
    );
  }

  const openVacancies = vacancies.filter(v => v.status === 0 || v.status === "Open").length;
  const canApply = isAuthenticated && !isMember && !myRequest && teamStatusLabel(team.status) !== "Завершена";

  return (
    <Page>
      <header className={sh.profile}>
        <div className={cx(sh.profileAvatar, sh.profileAvatarSquare)}>
          <Avatar name={team.name} src={team.avatarUrl} seed={team.id} size={128} square />
          {canEditTeam && (
            <>
              <button type="button" className={s.avatarBtn} onClick={() => avatarInputRef.current?.click()} disabled={avatarUploading} aria-label="Змінити аватар команди">
                <IconUpload size={15} />
              </button>
              <input ref={avatarInputRef} type="file" accept="image/jpeg,image/png,image/webp,image/gif" className="visually-hidden" tabIndex={-1} onChange={handleAvatarChange} />
            </>
          )}
        </div>
        <div className={sh.profileBody}>
          <div className={sh.profileName}>
            <h1>{team.name}</h1>
            <Status tone={teamStatusTone(team.status)}>{teamStatusLabel(team.status).toLowerCase()}</Status>
            {team.category && <Badge>{team.category}</Badge>}
            {isMember && <Badge tone="teal">{isOwner ? "ви лідер" : "ви учасник"}</Badge>}
          </div>
          <div className={sh.profileActions}>
            {canApply && (
              <ButtonLink to={`/team/join-request?teamId=${team.id}`} variant="primary">
                Подати заявку
              </ButtonLink>
            )}
            {!isMember && myRequest && (
              <Badge tone="teal" icon={<IconCheck size={13} />}>
                запит надіслано
              </Badge>
            )}
            {!isAuthenticated && (
              <ButtonLink to="/login" variant="primary">
                Увійдіть, щоб приєднатися
              </ButtonLink>
            )}
            {canPublishPosts && <ButtonLink to="/posts/new">Опублікувати пост</ButtonLink>}
            {canEditTeam && (
              <Button variant="secondary" onClick={() => setTab("settings")}>
                Налаштування
              </Button>
            )}
          </div>
          <div className={sh.profileNums}>
            <span>
              <b>{posts.length}</b>
              {plural(posts.length, ["публікація", "публікації", "публікацій"])}
            </span>
            <span>
              <b>{team.members.length}</b>
              {plural(team.members.length, ["учасник", "учасники", "учасників"])}
            </span>
            <span>
              <b>{openVacancies}</b>
              {plural(openVacancies, ["відкрита вакансія", "відкриті вакансії", "відкритих вакансій"])}
            </span>
          </div>
          {team.description && <p className={sh.profileBio}>{team.description}</p>}
        </div>
      </header>

      <div className={sh.people} aria-label="Учасники команди">
        {team.members.map(m => (
          <Link key={m.user.userId} to={`/users/${m.user.userId}`} className={sh.personBubble}>
            <Avatar name={m.user.username} src={m.user.avatarUrl} seed={m.user.userId} size={72} />
            <span>
              {m.user.username}
              {m.role === "Owner" ? " · лідер" : m.position ? ` · ${m.position}` : ""}
            </span>
          </Link>
        ))}
        {(team.requiredRoles ?? []).map(r =>
          canApply ? (
            <Link key={r.role} to={`/team/join-request?teamId=${team.id}`} className={sh.personBubble} title={`Шукаємо: ${r.role}`}>
              <span className={sh.bubbleOpen}>
                <IconPlus size={20} />
              </span>
              <span>{r.role}?</span>
            </Link>
          ) : (
            <span key={r.role} className={sh.personBubble} title={`Шукаємо: ${r.role}`}>
              <span className={sh.bubbleOpen}>
                <IconPlus size={20} />
              </span>
              <span>{r.role}?</span>
            </span>
          ),
        )}
      </div>

      <Tabs
        label="Розділи команди"
        value={tab}
        onChange={setTab}
        items={[
          { value: "portfolio", label: "Портфоліо", count: posts.length },
          { value: "overview", label: "Про команду" },
          { value: "vacancies", label: "Вакансії", count: openVacancies },
          { value: "members", label: "Учасники", count: team.members.length },
          { value: "requests", label: "Запити", count: requests.length, hidden: !canManageMembers },
          { value: "settings", label: "Налаштування", hidden: !canEditTeam },
        ]}
      />

      <div className={s.tabBody}>
        {tab === "overview" && (
          <div className={sh.cols}>
            <TeamAbout team={team} leaderEmail={leaderUser?.email} />
            <div className={sh.stack}>
              <TeamRoles team={team} canEdit={canEditTeam} canApply={canApply} onAdd={handleAddRole} />
              <section className={sh.formSection}>
                <div className={sh.between}>
                  <h2 className={s.blockTitle}>Учасники</h2>
                  <Button size="sm" variant="ghost" onClick={() => setTab("members")}>
                    Усі
                  </Button>
                </div>
                <ul className={s.miniMembers}>
                  {team.members.slice(0, 6).map(m => (
                    <li key={m.user.userId}>
                      <Link to={`/users/${m.user.userId}`} className={s.person}>
                        <Avatar name={m.user.username} src={m.user.avatarUrl} seed={m.user.userId} size={28} />
                        <span className={s.personName}>{m.user.username}</span>
                        {m.role === "Owner" && <Badge tone="accent">лідер</Badge>}
                      </Link>
                    </li>
                  ))}
                </ul>
              </section>
            </div>
          </div>
        )}

        {tab === "portfolio" && <TeamPortfolio posts={posts} teamName={team.name} canPublish={canPublishPosts} />}

        {tab === "vacancies" && <TeamVacancies vacancies={vacancies} isOwner={isOwner} onCreate={handleCreateVacancy} onUpdate={handleUpdateVacancy} onClose={id => void handleCloseVacancy(id)} />}

        {tab === "members" && (
          <TeamMembers
            team={team}
            isOwner={isOwner}
            canRemove={canManageMembers}
            currentUserId={user?.id}
            onRemove={memberId => void handleRemoveMember(memberId)}
            onPermissions={(memberId, perms) => void handleSavePermissions(memberId, perms)}
          />
        )}

        {tab === "requests" && canManageMembers && (
          <TeamRequests
            teamId={team.id}
            requests={requests}
            error={requestsError}
            onRefresh={loadRequests}
            onAccept={reqId => void handleAcceptRequest(reqId)}
            onReject={reqId => void handleRejectRequest(reqId)}
          />
        )}

        {tab === "settings" && canEditTeam && (
          <div className={s.settings}>
            <form className={sh.formSection} onSubmit={e => void handleSave(e)}>
              <h2 className={s.blockTitle}>Інформація про команду</h2>
              <Field label="Назва" htmlFor="ts-name">
                <Input id="ts-name" value={editName} onChange={e => setEditName(e.target.value)} />
              </Field>
              <Field label="Опис" htmlFor="ts-desc">
                <Textarea id="ts-desc" rows={4} value={editDesc} onChange={e => setEditDesc(e.target.value)} />
              </Field>
              <div className={sh.formRow}>
                <Field label="Категорія" htmlFor="ts-cat">
                  <Input id="ts-cat" value={editCategory} onChange={e => setEditCategory(e.target.value)} />
                </Field>
                <Field label="Статус" htmlFor="ts-status">
                  <Select id="ts-status" value={editStatus} onChange={e => setEditStatus(Number(e.target.value))}>
                    {STATUS_OPTIONS.map(o => (
                      <option key={o.value} value={o.value}>
                        {o.label}
                      </option>
                    ))}
                  </Select>
                </Field>
              </div>
              <Field label="Теги" htmlFor="ts-tags" hint="через кому">
                <Input id="ts-tags" value={editTags} onChange={e => setEditTags(e.target.value)} />
              </Field>
              {saveError && <Notice tone="error">{saveError}</Notice>}
              <div className={sh.formActions}>
                <Button variant="ghost" onClick={() => fillForm(team)}>
                  Скинути
                </Button>
                <Button type="submit" variant="primary" loading={saving}>
                  Зберегти зміни
                </Button>
              </div>
            </form>

            {isOwner && (
              <div className={s.danger}>
                <div className={sh.stackSm}>
                  <strong>Видалити команду</strong>
                  <span className={sh.faint}>Команду, її чат і вакансії буде видалено без можливості відновлення.</span>
                </div>
                <Button variant="danger" icon={<IconTrash size={15} />} onClick={() => void handleDeleteTeam()}>
                  Видалити
                </Button>
              </div>
            )}
          </div>
        )}
      </div>
    </Page>
  );
}
