import { useEffect, useRef, useState, type FormEvent } from "react";
import { Link, useNavigate, useSearchParams } from "react-router-dom";
import { useAuth } from "../context/AuthContext";
import { aggregatorApi } from "../api/aggregator";
import { vacanciesApi } from "../api/vacancies";
import type { PostWithEngagementDto, TeamEntity, VacancyApplicationEntity } from "../api/types";
import { applicationStatusLabel, timeAgo } from "../api/types";
import { Page } from "../layout/AppShell";
import { usePageMeta } from "../layout/PageMeta";
import { Avatar, Badge, Button, ButtonLink, EmptyState, Field, Input, Loading, Notice, Status, Tabs, useToast, cx } from "../ui";
import { IconBriefcase, IconExternal, IconLock, IconLogout, IconUpload } from "../ui/icons";
import Portfolio from "../components/Portfolio";
import UserTeams from "../components/UserTeams";
import { teamsApi } from "../api/teams";
import NotificationSettingsPanel from "../components/NotificationSettingsPanel";
import { plural } from "../lib/plural";
import { applicationTone } from "../lib/status";
import sh from "./shared.module.css";
import s from "./PersonalPage.module.css";

type Tab = "profile" | "portfolio" | "teams" | "applications" | "notifications";
const TABS: Tab[] = ["profile", "portfolio", "teams", "applications", "notifications"];

const ROLE_LABEL: Record<string, string> = { author: "автор", moderator: "модератор", administrator: "адміністратор" };

export default function PersonalPage() {
  usePageMeta("Особиста сторінка");
  const navigate = useNavigate();
  const toast = useToast();
  const [searchParams, setSearchParams] = useSearchParams();
  const { user, logout, updateAvatar, updateProfile, contentUserId } = useAuth();

  const tabParam = searchParams.get("tab") as Tab | null;
  const tab: Tab = tabParam && TABS.includes(tabParam) ? tabParam : "portfolio";
  const setTab = (t: Tab) => setSearchParams(t === "portfolio" ? {} : { tab: t }, { replace: true });

  const [teams, setTeams] = useState<TeamEntity[]>([]);
  const [teamsLoading, setTeamsLoading] = useState(true);
  const [posts, setPosts] = useState<PostWithEngagementDto[]>([]);
  const [postsLoading, setPostsLoading] = useState(true);
  const [applications, setApplications] = useState<VacancyApplicationEntity[]>([]);
  const [appsLoading, setAppsLoading] = useState(true);
  const fileInputRef = useRef<HTMLInputElement>(null);
  const [uploading, setUploading] = useState(false);
  const [fullName, setFullName] = useState(user?.fullName ?? "");
  const [specialization, setSpecialization] = useState(user?.specialization ?? "");
  const [profileSaving, setProfileSaving] = useState(false);
  const [profileError, setProfileError] = useState<string | null>(null);

  const userName = user?.userName ?? user?.email?.split("@")[0] ?? "";

  useEffect(() => {
    setFullName(user?.fullName ?? "");
    setSpecialization(user?.specialization ?? "");
  }, [user?.fullName, user?.specialization]);

  useEffect(() => {
    if (!user) {
      setPostsLoading(false);
      return;
    }
    setPostsLoading(true);
    // Team work belongs in the portfolio even before (or without) any personal post,
    // so a missing content id only means "no personal posts".
    aggregatorApi
      .getPortfolio(contentUserId ?? 0, user.id, contentUserId)
      .then(setPosts)
      .catch(() => setPosts([]))
      .finally(() => setPostsLoading(false));
  }, [user, contentUserId]);

  useEffect(() => {
    if (!user) {
      setAppsLoading(false);
      return;
    }
    setAppsLoading(true);
    vacanciesApi
      .getMyApplications()
      .then(setApplications)
      .catch(() => setApplications([]))
      .finally(() => setAppsLoading(false));
  }, [user]);

  useEffect(() => {
    if (!user) {
      setTeamsLoading(false);
      return;
    }
    setTeamsLoading(true);
    teamsApi
      .getMine()
      .then(setTeams)
      .catch(() => setTeams([]))
      .finally(() => setTeamsLoading(false));
  }, [user]);

  async function handleLogout() {
    await logout();
    navigate("/login");
  }

  async function handleAvatarFile(e: React.ChangeEvent<HTMLInputElement>) {
    const file = e.target.files?.[0];
    if (!file) return;
    setUploading(true);
    try {
      await updateAvatar(file);
      toast.success("Фото профілю оновлено");
    } catch (err) {
      toast.fromError(err, "Не вдалось завантажити фото");
    } finally {
      setUploading(false);
      if (fileInputRef.current) fileInputRef.current.value = "";
    }
  }

  async function handleSaveProfile(e: FormEvent) {
    e.preventDefault();
    setProfileSaving(true);
    setProfileError(null);
    try {
      await updateProfile({ fullName: fullName.trim() || undefined, specialization: specialization.trim() || undefined });
      toast.success("Зміни збережено");
    } catch (err) {
      setProfileError((err as Error).message ?? "Помилка збереження");
    } finally {
      setProfileSaving(false);
    }
  }

  if (!user) return <Page><Loading /></Page>;

  const displayName = user.fullName || userName;

  return (
    <Page>
      <header className={sh.profile}>
        <div className={sh.profileAvatar}>
          <Avatar name={displayName} src={user.avatarUrl} seed={user.id} size={128} />
          <button type="button" className={s.avatarBtn} onClick={() => fileInputRef.current?.click()} disabled={uploading} aria-label="Змінити фото профілю">
            <IconUpload size={15} />
          </button>
          <input ref={fileInputRef} type="file" accept="image/jpeg,image/png,image/webp,image/gif" className="visually-hidden" tabIndex={-1} onChange={handleAvatarFile} />
        </div>
        <div className={sh.profileBody}>
          <div className={sh.profileName}>
            <h1>{displayName}</h1>
            {user.roles.map(r => (
              <Badge key={r} tone={r === "administrator" ? "red" : r === "moderator" ? "amber" : "accent"}>
                {ROLE_LABEL[r] ?? r}
              </Badge>
            ))}
          </div>
          <div className={sh.profileActions}>
            <Button variant="secondary" onClick={() => setTab("profile")}>
              Редагувати профіль
            </Button>
            <ButtonLink to={`/users/${user.id}`} variant="ghost" icon={<IconExternal size={15} />}>
              Публічний профіль
            </ButtonLink>
            <Button variant="ghost" icon={<IconLogout size={15} />} onClick={() => void handleLogout()}>
              Вийти
            </Button>
          </div>
          <div className={sh.profileNums}>
            <span>
              <b>{postsLoading ? "…" : posts.length}</b>
              {plural(posts.length, ["робота", "роботи", "робіт"])}
            </span>
            <span>
              <b>{teamsLoading ? "…" : teams.length}</b>
              {plural(teams.length, ["команда", "команди", "команд"])}
            </span>
            <span>
              <b>{appsLoading ? "…" : applications.length}</b>
              {plural(applications.length, ["відгук", "відгуки", "відгуків"])}
            </span>
          </div>
          <p className={sh.profileBio}>
            {user.specialization && (
              <>
                {user.specialization}
                <br />
              </>
            )}
            <span className={sh.mono}>
              @{userName} · {user.email}
            </span>
          </p>
        </div>
      </header>

      <div className={sh.centerTabs}>
        <Tabs
          label="Розділи"
          value={tab}
          onChange={setTab}
          items={[
            { value: "portfolio", label: "Портфоліо", count: postsLoading ? undefined : posts.length },
            { value: "teams", label: "Команди", count: teamsLoading ? undefined : teams.length },
            { value: "applications", label: "Мої відгуки", count: appsLoading ? undefined : applications.length },
            { value: "profile", label: "Профіль" },
            { value: "notifications", label: "Сповіщення" },
          ]}
        />
      </div>
      <div className={s.tabBody}>
        {tab === "profile" && (
          <div className={sh.cols}>
            <form className={sh.formSection} onSubmit={e => void handleSaveProfile(e)}>
              <h2 className={s.cardTitle}>Особиста інформація</h2>
              <Field label="Email" htmlFor="pf-email" hint="Змінюється в обліковому записі Keycloak">
                <Input id="pf-email" type="email" value={user.email} readOnly />
              </Field>
              <Field label="Повне ім'я" htmlFor="pf-name">
                <Input id="pf-name" placeholder="Введіть повне ім'я" value={fullName} onChange={e => setFullName(e.target.value)} />
              </Field>
              <Field label="Спеціалізація" htmlFor="pf-spec">
                <Input id="pf-spec" placeholder="Напр.: UI/UX Design, Figma, Animation" value={specialization} onChange={e => setSpecialization(e.target.value)} />
              </Field>
              {profileError && <Notice tone="error">{profileError}</Notice>}
              <div className={sh.formActions}>
                <Button type="submit" variant="primary" loading={profileSaving}>
                  Зберегти зміни
                </Button>
              </div>
            </form>

            {/* Passwords belong to Keycloak, so this only points at the security page
                rather than collecting credentials the API no longer accepts. */}
            <section className={sh.formSection}>
              <h2 className={s.cardTitle}>Пароль та безпека</h2>
              <p className={sh.muted}>Зміна пароля, двофакторна автентифікація та активні сесії керуються в обліковому записі Keycloak.</p>
              <div>
                <ButtonLink to="/security" icon={<IconLock size={15} />}>
                  Перейти до безпеки
                </ButtonLink>
              </div>
            </section>
          </div>
        )}

        {tab === "notifications" && <NotificationSettingsPanel />}

        {tab === "portfolio" && (
          <Portfolio
            posts={posts}
            loading={postsLoading}
            actions={
              <ButtonLink to="/posts/new" size="sm" variant="primary">
                Новий пост
              </ButtonLink>
            }
            empty={{
              title: "Портфоліо порожнє",
              text: "Опублікуйте першу роботу — вона з'явиться тут і у вашому публічному профілі. Роботи команд, у яких ви є, теж потрапляють сюди.",
              action: <ButtonLink to="/posts/new">Створити пост</ButtonLink>,
            }}
          />
        )}

        {tab === "teams" && user && (
          <UserTeams
            teams={teams}
            userId={user.id}
            loading={teamsLoading}
            empty={{
              title: "Ви ще не в жодній команді",
              action: (
                <>
                  <ButtonLink to="/teams">Знайти команду</ButtonLink> <ButtonLink to="/team/new" variant="primary">Створити команду</ButtonLink>
                </>
              ),
            }}
          />
        )}

        {tab === "applications" && (
          <>
            {appsLoading && <Loading />}
            {!appsLoading && applications.length === 0 && (
              <EmptyState
                icon={<IconBriefcase />}
                title="Ви ще не подавали відгуки"
                text="Знайдіть відкриту роль у команді та відгукніться."
                action={<ButtonLink to="/vacancies">Переглянути вакансії</ButtonLink>}
              />
            )}
            {!appsLoading && applications.length > 0 && (
              <ul className={sh.list}>
                {applications.map(app => (
                  <li key={app.id}>
                    <Link to={`/vacancies/${app.vacancyId}`} className={cx(sh.row, sh.rowLink)} style={{ color: "inherit", textDecoration: "none" }}>
                      <div className={sh.rowMain}>
                        <span className={sh.rowTitle}>{app.role}</span>
                        <span className={sh.mono}>
                          {app.teamName} · {timeAgo(app.createdAt)}
                        </span>
                      </div>
                      <Status tone={applicationTone(app.status)}>{applicationStatusLabel(app.status).toLowerCase()}</Status>
                    </Link>
                  </li>
                ))}
              </ul>
            )}
          </>
        )}
      </div>
    </Page>
  );
}
