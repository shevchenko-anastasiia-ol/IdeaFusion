import { useEffect, useState, type FormEvent } from "react";
import { Link, useNavigate, useSearchParams } from "react-router-dom";
import { teamsApi, groupInvitationsApi } from "../api/teams";
import { useAuth } from "../context/AuthContext";
import type { TeamEntity, UserDto } from "../api/types";
import { teamStatusLabel, hasPermission } from "../api/types";
import { api } from "../api/client";
import { Page } from "../layout/AppShell";
import { usePageMeta } from "../layout/PageMeta";
import { Avatar, Button, ButtonLink, EmptyState, Field, Input, Loading, Notice, PageHeader, Select, Status, Textarea } from "../ui";
import { IconCheck, IconUser, IconUsers } from "../ui/icons";
import RadioCard from "../components/RadioCard";
import { teamStatusTone } from "../lib/status";
import { plural } from "../lib/plural";
import sh from "./shared.module.css";
import s from "./TeamForms.module.css";

export default function InviteToTeamPage() {
  usePageMeta("Запросити до команди", [{ label: "Команди", to: "/collaborations" }]);
  const navigate = useNavigate();
  const [params] = useSearchParams();
  const { user } = useAuth();
  const inviteeId = params.get("userId") ?? "";
  const preTeamId = params.get("teamId") ?? "";

  const [invitee, setInvitee] = useState<UserDto | null>(null);
  const [myTeams, setMyTeams] = useState<TeamEntity[]>([]);
  const [selectedTeamId, setSelectedTeamId] = useState(preTeamId);
  const [role, setRole] = useState("");
  const [message, setMessage] = useState("");
  const [submitting, setSubmitting] = useState(false);
  const [done, setDone] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    if (!user || !inviteeId) {
      setLoading(false);
      return;
    }
    setLoading(true);
    Promise.all([api.get<UserDto>(`/api/users/${inviteeId}`).catch(() => null), teamsApi.getMine()])
      .then(([inv, teams]) => {
        setInvitee(inv);
        const allowed = (teams as TeamEntity[]).filter(t => hasPermission(t, user.id, "canInviteMembers"));
        setMyTeams(allowed);
        setSelectedTeamId(current => current || allowed[0]?.id || "");
      })
      .catch(() => setMyTeams([]))
      .finally(() => setLoading(false));
  }, [user, inviteeId]);

  const selectedTeam = myTeams.find(t => t.id === selectedTeamId) ?? null;
  const availableRoles = selectedTeam?.requiredRoles.map(r => r.role) ?? [];

  async function handleSubmit(e: FormEvent) {
    e.preventDefault();
    if (!user) {
      navigate("/login");
      return;
    }
    if (!selectedTeamId) {
      setError("Оберіть команду");
      return;
    }
    if (!role.trim()) {
      setError("Вкажіть роль");
      return;
    }
    if (!inviteeId) {
      setError("Немає отримувача");
      return;
    }
    setSubmitting(true);
    setError(null);
    try {
      await groupInvitationsApi.create({
        teamId: selectedTeamId,
        invitedUserId: inviteeId,
        role: role.trim(),
        message: message.trim() || undefined,
        expirationDays: 14,
      });
      setDone(true);
    } catch (ex: unknown) {
      setError(ex instanceof Error ? ex.message : "Помилка надсилання");
    } finally {
      setSubmitting(false);
    }
  }

  const inviteeName = invitee?.fullName || invitee?.userName || invitee?.email?.split("@")[0] || inviteeId.slice(0, 8);

  if (!inviteeId) {
    return (
      <Page width="narrow">
        <PageHeader title="Запросити до команди" />
        <EmptyState
          icon={<IconUser />}
          title="Кого запрошуємо?"
          text="Знайдіть людину через пошук і відкрийте її профіль — там є кнопка «Запросити до команди»."
          action={<ButtonLink to="/search?kind=user">Шукати людей</ButtonLink>}
        />
      </Page>
    );
  }

  if (loading) {
    return (
      <Page width="narrow">
        <Loading />
      </Page>
    );
  }

  if (done) {
    return (
      <Page width="narrow">
        <div className={s.done}>
          <span className={s.doneIcon}>
            <IconCheck size={22} />
          </span>
          <h1 className={s.doneTitle}>Запрошення надіслано</h1>
          <p className={sh.muted}>
            {inviteeName} отримає запрошення до команди «{selectedTeam?.name}» на роль «{role}». Запрошення діє 14 днів.
          </p>
          <div className={sh.inline}>
            <Button variant="primary" onClick={() => navigate(-1)}>
              Назад
            </Button>
            {selectedTeam && <ButtonLink to={`/teams/${selectedTeam.id}`}>До команди</ButtonLink>}
          </div>
        </div>
      </Page>
    );
  }

  return (
    <Page width="narrow">
      <PageHeader title="Запросити до команди" description="Запрошення надійде користувачу в розділ «Команди»." />
      <form className={sh.form} onSubmit={e => void handleSubmit(e)}>
        <section className={sh.formSection}>
          <span className={sh.mono}>кому</span>
          <Link to={`/users/${inviteeId}`} className={s.partyBody}>
            <Avatar name={inviteeName} src={invitee?.avatarUrl} seed={inviteeId} size={40} />
            <div className={s.partyText}>
              <span className={s.partyName}>{inviteeName}</span>
              {invitee?.specialization && <span className={sh.mono}>{invitee.specialization}</span>}
            </div>
          </Link>
        </section>

        <section className={sh.formSection}>
          <span className={sh.mono}>до команди</span>
          {myTeams.length === 0 ? (
            <EmptyState
              icon={<IconUsers />}
              title="Немає команд, куди ви можете запрошувати"
              text="Запрошувати можуть власники команд та учасники з відповідним правом."
              action={<ButtonLink to="/team/new">Створити команду</ButtonLink>}
            />
          ) : (
            <div className={s.teamOptions} role="radiogroup" aria-label="Команда">
              {myTeams.map(t => (
                <RadioCard
                  key={t.id}
                  name="invite-team"
                  checked={selectedTeamId === t.id}
                  onSelect={() => {
                    setSelectedTeamId(t.id);
                    setRole("");
                  }}
                >
                  <Avatar name={t.name} src={t.avatarUrl} seed={t.id} size={32} square />
                  <span className={s.optText}>
                    <span className={s.optName}>{t.name}</span>
                    <span className={sh.mono}>
                      {t.members.length} {plural(t.members.length, ["учасник", "учасники", "учасників"])} · {t.tags.join(" · ") || t.category}
                    </span>
                  </span>
                  <Status tone={teamStatusTone(t.status)}>{teamStatusLabel(t.status).toLowerCase()}</Status>
                </RadioCard>
              ))}
            </div>
          )}
        </section>

        {myTeams.length > 0 && (
          <section className={sh.formSection}>
            <Field label="Роль" required htmlFor="inv-role">
              {availableRoles.length > 0 ? (
                <Select id="inv-role" value={role} onChange={e => setRole(e.target.value)}>
                  <option value="">Оберіть з відкритих ролей…</option>
                  {availableRoles.map(r => (
                    <option key={r} value={r}>
                      {r}
                    </option>
                  ))}
                </Select>
              ) : (
                <Input id="inv-role" placeholder="Напр.: UI Designer" value={role} onChange={e => setRole(e.target.value)} />
              )}
            </Field>
            <Field label="Повідомлення" htmlFor="inv-msg">
              <Textarea id="inv-msg" rows={5} placeholder="Привіт! Ми хотіли б запросити вас до нашої команди…" value={message} onChange={e => setMessage(e.target.value)} />
            </Field>
          </section>
        )}

        {error && <Notice tone="error">{error}</Notice>}

        <div className={sh.formActions}>
          <Button variant="ghost" onClick={() => navigate(-1)}>
            Скасувати
          </Button>
          <Button type="submit" variant="primary" loading={submitting} disabled={myTeams.length === 0}>
            Надіслати запрошення
          </Button>
        </div>
      </form>
    </Page>
  );
}
