import { useEffect, useState } from "react";
import { Link, useNavigate, useParams } from "react-router-dom";
import { vacanciesApi } from "../api/vacancies";
import { teamsApi } from "../api/teams";
import type { TeamEntity, TeamVacancyEntity, VacancyApplicationEntity } from "../api/types";
import { applicationStatusKey, applicationStatusLabel, timeAgo, vacancyStatusLabel } from "../api/types";
import { useAuth } from "../context/AuthContext";
import { Page } from "../layout/AppShell";
import { usePageMeta } from "../layout/PageMeta";
import { Avatar, Badge, Button, ButtonLink, EmptyState, ErrorState, Field, Loading, Notice, SectionTitle, Status, Textarea, useDialogs, useToast } from "../ui";
import { IconArrowLeft, IconBriefcase } from "../ui/icons";
import { applicationTone, vacancyTone } from "../lib/status";
import VacancyFormDialog from "../components/VacancyFormDialog";
import sh from "./shared.module.css";
import s from "./VacancyDetailPage.module.css";

export default function VacancyDetailPage() {
  const navigate = useNavigate();
  const { id } = useParams();
  const { user, isAuthenticated } = useAuth();
  const { confirm } = useDialogs();
  const toast = useToast();
  const [vacancy, setVacancy] = useState<TeamVacancyEntity | null>(null);
  const [editOpen, setEditOpen] = useState(false);
  const [team, setTeam] = useState<TeamEntity | null>(null);
  const [applications, setApplications] = useState<VacancyApplicationEntity[]>([]);
  const [myApplications, setMyApplications] = useState<VacancyApplicationEntity[]>([]);
  const [message, setMessage] = useState("Доброго дня, хочу долучитися до вашої команди!");
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [loadError, setLoadError] = useState<string | null>(null);
  const [loading, setLoading] = useState(true);

  usePageMeta(vacancy?.role ?? "Вакансія", [{ label: "Вакансії", to: "/vacancies" }]);

  useEffect(() => {
    if (!id) navigate("/vacancies", { replace: true });
  }, [id, navigate]);

  useEffect(() => {
    if (!id) {
      setLoading(false);
      return;
    }
    let cancelled = false;
    setLoading(true);
    Promise.all([vacanciesApi.getById(id), user ? vacanciesApi.getMyApplications() : Promise.resolve([])])
      .then(([v, mine]) => {
        if (cancelled) return;
        setVacancy(v);
        setMyApplications(mine);
        return teamsApi
          .getById(v.teamId)
          .then(t => {
            if (!cancelled) setTeam(t);
          })
          .catch(() => undefined);
      })
      .catch((e: Error) => {
        if (!cancelled) setLoadError(e.message);
      })
      .finally(() => {
        if (!cancelled) setLoading(false);
      });
    return () => {
      cancelled = true;
    };
  }, [id, user]);

  const isOwner = !!team && !!user && team.members.some(m => m.user.userId === user.id && m.role === "Owner");
  const isMember = !!team && !!user && team.members.some(m => m.user.userId === user.id);
  const isOpen = vacancyStatusLabel(vacancy?.status ?? 0) === "Відкрита";
  const existingMine = myApplications.find(a => a.vacancyId === vacancy?.id);
  const myStatus = existingMine ? applicationStatusKey(existingMine.status) : null;
  const canApply = isOpen && !isMember && !existingMine;

  useEffect(() => {
    if (!isOwner || !vacancy || !user) return;
    vacanciesApi
      .getApplications(vacancy.id)
      .then(setApplications)
      .catch(() => setApplications([]));
  }, [isOwner, vacancy, user]);

  async function handleApply() {
    if (!user || !vacancy) return;
    setBusy(true);
    setError(null);
    try {
      await vacanciesApi.apply({ vacancyId: vacancy.id, message: message.trim() || undefined });
      setMyApplications(await vacanciesApi.getMyApplications());
      toast.success("Відгук надіслано");
    } catch (ex: unknown) {
      setError(ex instanceof Error ? ex.message : "Помилка надсилання");
    } finally {
      setBusy(false);
    }
  }

  async function handleDecision(app: VacancyApplicationEntity, accept: boolean) {
    if (!user) return;
    setBusy(true);
    setError(null);
    try {
      if (accept) {
        await vacanciesApi.accept({ applicationId: app.id });
        if (vacancy) setVacancy(await vacanciesApi.getById(vacancy.id));
      } else {
        await vacanciesApi.reject({ applicationId: app.id });
      }
      setApplications(await vacanciesApi.getApplications(app.vacancyId));
      toast.success(accept ? `${app.applicantUsername} тепер у команді` : "Відгук відхилено");
    } catch (ex: unknown) {
      setError(ex instanceof Error ? ex.message : "Помилка");
    } finally {
      setBusy(false);
    }
  }

  async function handleUpdate(values: { role: string; description?: string; requirements?: string }) {
    if (!vacancy) return;
    setVacancy(await vacanciesApi.update({ vacancyId: vacancy.id, ...values }));
    toast.success("Вакансію оновлено");
  }

  async function handleClose() {
    if (!user || !vacancy) return;
    const ok = await confirm({ title: "Закрити вакансію?", message: "Нові відгуки на неї прийматися не будуть.", confirmLabel: "Закрити" });
    if (!ok) return;
    setBusy(true);
    try {
      setVacancy(await vacanciesApi.close({ vacancyId: vacancy.id }));
      toast.success("Вакансію закрито");
    } catch (ex: unknown) {
      setError(ex instanceof Error ? ex.message : "Помилка");
    } finally {
      setBusy(false);
    }
  }

  if (loading) {
    return (
      <Page>
        <Loading />
      </Page>
    );
  }

  if (!vacancy) {
    return (
      <Page width="narrow">
        <Link to="/vacancies" className={sh.backLink}>
          <IconArrowLeft size={16} /> До вакансій
        </Link>
        {loadError ? <ErrorState title="Не вдалося відкрити вакансію" message={loadError} /> : <EmptyState icon={<IconBriefcase />} title="Вакансію не знайдено" />}
      </Page>
    );
  }

  return (
    <Page>
      <Link to="/vacancies" className={sh.backLink}>
        <IconArrowLeft size={16} /> До вакансій
      </Link>

      <header className={sh.hero}>
        <div className={sh.heroText}>
          <span className={sh.mono}>вакансія · {timeAgo(vacancy.createdAt)}</span>
          <h1 className={sh.heroTitle}>{vacancy.role}</h1>
          <span className={sh.inline}>
            <Status tone={vacancyTone(vacancy.status)}>{vacancyStatusLabel(vacancy.status).toLowerCase()}</Status>
            {vacancy.category && <Badge>{vacancy.category}</Badge>}
            {vacancy.tags.map(t => (
              <span key={t} className={sh.mono}>
                #{t}
              </span>
            ))}
          </span>
        </div>
        {isOwner && isOpen && (
          <div className={sh.heroActions}>
            <Button variant="ghost" onClick={() => setEditOpen(true)}>
              Редагувати
            </Button>
            <Button variant="danger" loading={busy} onClick={() => void handleClose()}>
              Закрити вакансію
            </Button>
          </div>
        )}
      </header>

      <VacancyFormDialog
        open={editOpen}
        initial={{ role: vacancy.role, description: vacancy.description, requirements: vacancy.requirements }}
        onClose={() => setEditOpen(false)}
        onSubmit={handleUpdate}
      />

      <div className={sh.withRail}>
        <div className={sh.stackLg}>
          {vacancy.description && (
            <section className={s.section}>
              <SectionTitle>опис задачі</SectionTitle>
              <p className={s.text}>{vacancy.description}</p>
            </section>
          )}
          {vacancy.requirements && (
            <section className={s.section}>
              <SectionTitle>вимоги до кандидата</SectionTitle>
              <p className={s.text}>{vacancy.requirements}</p>
            </section>
          )}
          {!vacancy.description && !vacancy.requirements && <p className={sh.faint}>Команда не додала опису до цієї ролі.</p>}

          {error && <Notice tone="error">{error}</Notice>}

          {isOwner && (
            <section className={s.section}>
              <SectionTitle>відгуки · {applications.length}</SectionTitle>
              {applications.length === 0 ? (
                <p className={sh.faint}>Поки що немає відгуків.</p>
              ) : (
                <ul className={sh.list}>
                  {applications.map(app => {
                    const pending = applicationStatusKey(app.status) === "pending";
                    return (
                      <li key={app.id} className={sh.row}>
                        <Avatar name={app.applicantUsername} src={app.applicantAvatarUrl} seed={app.applicantUserId} size={36} />
                        <div className={sh.rowMain}>
                          <Link to={`/users/${app.applicantUserId}`} className={sh.rowTitle}>
                            {app.applicantUsername}
                          </Link>
                          {app.message && <span className={sh.rowSub}>{app.message}</span>}
                          <span className={sh.mono}>{timeAgo(app.createdAt)}</span>
                        </div>
                        <div className={sh.rowActions}>
                          {pending ? (
                            <>
                              <Button size="sm" variant="primary" disabled={busy} onClick={() => void handleDecision(app, true)}>
                                Прийняти
                              </Button>
                              <Button size="sm" variant="ghost" disabled={busy} onClick={() => void handleDecision(app, false)}>
                                Відхилити
                              </Button>
                            </>
                          ) : (
                            <Status tone={applicationTone(app.status)}>{applicationStatusLabel(app.status).toLowerCase()}</Status>
                          )}
                        </div>
                      </li>
                    );
                  })}
                </ul>
              )}
            </section>
          )}

          {!isOwner && (
            <section className={s.applyBox}>
              {isMember && <p className={sh.muted}>Ви вже є учасником цієї команди.</p>}
              {!isMember && existingMine && (
                <div className={sh.between}>
                  <span>Ваш відгук</span>
                  <Status tone={applicationTone(existingMine.status)}>{applicationStatusLabel(existingMine.status).toLowerCase()}</Status>
                </div>
              )}
              {!isMember && !myStatus && !isOpen && <p className={sh.muted}>Вакансію закрито — відгуки більше не приймаються.</p>}
              {canApply && !isAuthenticated && (
                <div className={sh.between}>
                  <span className={sh.muted}>Увійдіть, щоб відгукнутися на вакансію.</span>
                  <ButtonLink to="/login" variant="primary">
                    Увійти
                  </ButtonLink>
                </div>
              )}
              {canApply && isAuthenticated && (
                <>
                  <Field label="Текст відгуку" htmlFor="apply-msg">
                    <Textarea id="apply-msg" rows={5} value={message} onChange={e => setMessage(e.target.value)} />
                  </Field>
                  <div className={sh.formActions}>
                    <Button variant="primary" loading={busy} onClick={() => void handleApply()}>
                      Подати відгук
                    </Button>
                  </div>
                </>
              )}
            </section>
          )}
        </div>

        <aside className={sh.rail}>
          {team ? (
            <Link to={`/teams/${team.id}`} className={s.teamCard}>
              <Avatar name={team.name} src={team.avatarUrl} seed={team.id} size={44} square />
              <span className={s.teamText}>
                <span className={s.teamName}>{team.name}</span>
                <span className={sh.mono}>
                  {team.members.length} учасн. · {team.category}
                </span>
              </span>
            </Link>
          ) : (
            <div className={s.teamCard}>
              <Avatar name={vacancy.teamName} src={vacancy.teamAvatarUrl} seed={vacancy.teamId} size={44} square />
              <span className={s.teamText}>
                <span className={s.teamName}>{vacancy.teamName}</span>
              </span>
            </div>
          )}
          {team?.description && <p className={`${sh.small} ${sh.muted}`}>{team.description}</p>}
        </aside>
      </div>
    </Page>
  );
}
