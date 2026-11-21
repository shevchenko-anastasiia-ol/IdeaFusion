import { useEffect, useState, type FormEvent } from "react";
import { Link, useNavigate, useSearchParams } from "react-router-dom";
import { teamsApi, collaborationRequestsApi } from "../api/teams";
import type { TeamEntity } from "../api/types";
import { teamStatusLabel } from "../api/types";
import { useAuth } from "../context/AuthContext";
import { Page } from "../layout/AppShell";
import { usePageMeta } from "../layout/PageMeta";
import { Avatar, Button, ButtonLink, EmptyState, Field, Input, Loading, Notice, PageHeader, Select, Status, Textarea } from "../ui";
import { IconCheck, IconUsers } from "../ui/icons";
import { teamStatusTone } from "../lib/status";
import sh from "./shared.module.css";
import s from "./TeamForms.module.css";

export default function JoinRequestPage() {
  const navigate = useNavigate();
  const [params] = useSearchParams();
  const { user } = useAuth();
  const preTeamId = params.get("teamId") ?? "";

  const [team, setTeam] = useState<TeamEntity | null>(null);
  const [loading, setLoading] = useState(!!preTeamId);
  const [role, setRole] = useState("");
  const [message, setMessage] = useState("Доброго дня, прошу розглянути мою кандидатуру на учасника вашої команди.");
  const [submitting, setSubmitting] = useState(false);
  const [done, setDone] = useState(false);
  const [error, setError] = useState<string | null>(null);

  usePageMeta("Запит на вступ", [
    { label: "Команди", to: "/collaborations" },
    ...(team ? [{ label: team.name, to: `/teams/${team.id}` }] : []),
  ]);

  useEffect(() => {
    if (!preTeamId) return;
    setLoading(true);
    teamsApi
      .getById(preTeamId)
      .then(setTeam)
      .catch(() => setTeam(null))
      .finally(() => setLoading(false));
  }, [preTeamId]);

  async function handleSubmit(e: FormEvent) {
    e.preventDefault();
    if (!user || !team || !role.trim()) {
      setError("Оберіть роль");
      return;
    }
    setSubmitting(true);
    setError(null);
    try {
      await collaborationRequestsApi.create({ teamId: team.id, role: role.trim(), message: message.trim() || undefined });
      setDone(true);
    } catch (ex: unknown) {
      setError(ex instanceof Error ? ex.message : "Помилка надсилання");
    } finally {
      setSubmitting(false);
    }
  }

  if (loading) {
    return (
      <Page width="narrow">
        <Loading />
      </Page>
    );
  }

  if (!team) {
    return (
      <Page width="narrow">
        <PageHeader title="Запит на вступ" />
        <EmptyState
          icon={<IconUsers />}
          title={preTeamId ? "Команду не знайдено" : "Команду не обрано"}
          text="Відкрийте сторінку команди в каталозі та натисніть «Подати заявку»."
          action={<ButtonLink to="/teams">До каталогу команд</ButtonLink>}
        />
      </Page>
    );
  }

  const roles = team.requiredRoles.map(r => r.role);
  const myName = user?.fullName ?? user?.userName ?? user?.email ?? "Ви";

  if (done) {
    return (
      <Page width="narrow">
        <div className={s.done}>
          <span className={s.doneIcon}>
            <IconCheck size={22} />
          </span>
          <h1 className={s.doneTitle}>Заявку надіслано</h1>
          <p className={sh.muted}>
            Лідер команди «{team.name}» розгляне ваш запит на роль «{role}». Статус з'явиться на сторінці команди.
          </p>
          <div className={sh.inline}>
            <ButtonLink to={`/teams/${team.id}`} variant="primary">
              До команди
            </ButtonLink>
            <ButtonLink to="/teams">Інші команди</ButtonLink>
          </div>
        </div>
      </Page>
    );
  }

  return (
    <Page width="narrow">
      <PageHeader title="Запит на вступ" description="Розкажіть команді, чим можете бути корисні." />
      <form className={sh.form} onSubmit={e => void handleSubmit(e)}>
        <section className={sh.formSection}>
          <div className={s.route}>
            <div className={s.party}>
              <span className={sh.mono}>від</span>
              <div className={s.partyBody}>
                <Avatar name={myName} src={user?.avatarUrl} seed={user?.id} size={36} />
                <div className={s.partyText}>
                  <span className={s.partyName}>{myName}</span>
                  {user?.specialization && <span className={sh.mono}>{user.specialization}</span>}
                </div>
              </div>
            </div>
            <span className={s.arrow} aria-hidden="true">→</span>
            <div className={s.party}>
              <span className={sh.mono}>до команди</span>
              <Link to={`/teams/${team.id}`} className={s.partyBody}>
                <Avatar name={team.name} src={team.avatarUrl} seed={team.id} size={36} square />
                <div className={s.partyText}>
                  <span className={s.partyName}>{team.name}</span>
                  <Status tone={teamStatusTone(team.status)}>{teamStatusLabel(team.status).toLowerCase()}</Status>
                </div>
              </Link>
            </div>
          </div>
        </section>

        <section className={sh.formSection}>
          <Field label="Роль" required htmlFor="jr-role" hint={roles.length === 0 ? "Команда не вказала відкритих ролей — опишіть свою" : undefined}>
            {roles.length > 0 ? (
              <Select id="jr-role" value={role} onChange={e => setRole(e.target.value)}>
                <option value="">Оберіть з відкритих ролей…</option>
                {roles.map(r => (
                  <option key={r} value={r}>
                    {r}
                  </option>
                ))}
              </Select>
            ) : (
              <Input id="jr-role" placeholder="Напр.: 3D художник" value={role} onChange={e => setRole(e.target.value)} />
            )}
          </Field>
          <Field label="Повідомлення" htmlFor="jr-msg">
            <Textarea id="jr-msg" rows={6} value={message} onChange={e => setMessage(e.target.value)} />
          </Field>
        </section>

        {error && <Notice tone="error">{error}</Notice>}

        <div className={sh.formActions}>
          <Button variant="ghost" onClick={() => navigate(-1)}>
            Скасувати
          </Button>
          <Button type="submit" variant="primary" loading={submitting}>
            Надіслати запит
          </Button>
        </div>
      </form>
    </Page>
  );
}
