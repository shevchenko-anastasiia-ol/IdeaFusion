import { useEffect, useState } from "react";
import { useParams } from "react-router-dom";
import { teamsApi } from "../api/teams";
import { useAuth } from "../context/AuthContext";
import { aggregatorApi } from "../api/aggregator";
import { postauthorApi } from "../api/posts";
import type { TeamEntity, UserDto, PostWithEngagementDto } from "../api/types";
import { api } from "../api/client";
import { Page } from "../layout/AppShell";
import { usePageMeta } from "../layout/PageMeta";
import { Avatar, Badge, Button, ButtonLink, EmptyState, Loading, Tabs, useToast } from "../ui";
import { IconChat, IconEdit, IconLock, IconUser, IconUsers } from "../ui/icons";
import { useChatPanel } from "../components/ChatWidget";
import { directApi } from "../api/direct";
import { useDirectChat } from "../context/DirectChatContext";
import type { DirectConversationDto } from "../api/types";
import Portfolio from "../components/Portfolio";
import UserTeams from "../components/UserTeams";
import { plural } from "../lib/plural";
import sh from "./shared.module.css";

const ROLE_LABEL: Record<string, string> = { author: "автор", moderator: "модератор", administrator: "адміністратор" };

export default function UserPublicPage() {
  const { id } = useParams<{ id: string }>();
  const { user: me, isAuthenticated } = useAuth();
  const [profile, setProfile] = useState<UserDto | null>(null);
  const [teams, setTeams] = useState<TeamEntity[]>([]);
  const [posts, setPosts] = useState<PostWithEngagementDto[]>([]);
  const [loading, setLoading] = useState(true);
  const [notFound, setNotFound] = useState(false);
  const [tab, setTab] = useState<"portfolio" | "teams">("portfolio");
  const isMe = me?.id === id;
  const panel = useChatPanel();
  const direct = useDirectChat();
  const toast = useToast();
  const [dm, setDm] = useState<DirectConversationDto | null>(null);
  const [dmBusy, setDmBusy] = useState(false);

  // Where the two of us stand: no chat yet, a request, an open chat or a block.
  useEffect(() => {
    setDm(null);
    if (!id || isMe || !isAuthenticated) return;
    directApi.withUser(id).then(setDm).catch(() => setDm(null));
  }, [id, isMe, isAuthenticated]);
  // Keep it in step with live updates from the chat panel.
  useEffect(() => {
    const live = direct.conversations.find(c => c.otherUserId === id);
    if (live) setDm(live);
  }, [direct.conversations, id]);

  async function toggleBlock() {
    if (!id) return;
    setDmBusy(true);
    try {
      const updated = dm?.canUnblock ? await directApi.act(dm.id, "unblock") : await directApi.blockUser(id);
      setDm(updated);
      void direct.refresh();
      toast.success(updated.status === "blocked" ? "Користувача заблоковано" : "Користувача розблоковано");
    } catch (err: unknown) {
      toast.fromError(err, "Не вдалося змінити блокування");
    } finally {
      setDmBusy(false);
    }
  }

  const displayName = profile ? profile.fullName ?? profile.userName ?? profile.email.split("@")[0] : "Профіль";
  usePageMeta(displayName, [{ label: "Люди" }]);

  useEffect(() => {
    if (!id) return;
    setLoading(true);
    setNotFound(false);
    setProfile(null);
    setTeams([]);
    setPosts([]);
    Promise.all([
      api.get<UserDto>(`/api/users/${id}`).catch(() => null),
      // Anyone's teams are public (team pages list their members), so every profile shows them.
      (isMe ? teamsApi.getMine() : teamsApi.getByMember(id)).catch(() => [] as TeamEntity[]),
    ])
      .then(async ([u, t]) => {
        setTeams(t as TeamEntity[]);
        if (!u) {
          setNotFound(true);
          return;
        }
        setProfile(u);
        const userName = u.userName ?? u.email.split("@")[0];
        // No personal author record just means no personal posts (404): the portfolio still
        // holds the work of this person's teams, so it is loaded either way.
        let contentUserId = 0;
        try {
          contentUserId = (await postauthorApi.getByUserName(userName)).userId;
        } catch {
          /* never published personally */
        }
        try {
          setPosts(await aggregatorApi.getPortfolio(contentUserId, u.id));
        } catch {
          setPosts([]);
        }
      })
      .finally(() => setLoading(false));
  }, [id, isMe]);

  if (loading) {
    return (
      <Page>
        <Loading />
      </Page>
    );
  }

  if (notFound || !profile) {
    return (
      <Page width="narrow">
        <EmptyState
          icon={<IconUser />}
          title="Користувача не знайдено"
          text={isAuthenticated ? "Можливо, акаунт видалено або посилання неточне." : "Увійдіть, щоб переглядати профілі користувачів."}
          action={isAuthenticated ? <ButtonLink to="/home">До стрічки</ButtonLink> : <ButtonLink to="/login" variant="primary">Увійти</ButtonLink>}
        />
      </Page>
    );
  }

  const totalLikes = posts.reduce((acc, p) => acc + p.likesCount, 0);
  const totalComments = posts.reduce((acc, p) => acc + p.commentsCount, 0);

  return (
    <Page>
      <header className={sh.profile}>
        <div className={sh.profileAvatar}>
          <Avatar name={displayName} src={profile.avatarUrl} seed={profile.id} size={128} />
        </div>
        <div className={sh.profileBody}>
          <div className={sh.profileName}>
            <h1>{displayName}</h1>
            {profile.roles.map(r => (
              <Badge key={r} tone={r === "administrator" ? "red" : r === "moderator" ? "amber" : "accent"}>
                {ROLE_LABEL[r] ?? r}
              </Badge>
            ))}
          </div>
          <div className={sh.profileActions}>
            {isMe ? (
              <ButtonLink to="/personal?tab=profile" icon={<IconEdit size={15} />}>
                Редагувати профіль
              </ButtonLink>
            ) : (
              isAuthenticated && (
                <>
                  {dm?.status === "blocked" && !dm.blockedByMe ? (
                    <Button disabled icon={<IconLock size={15} />} title={`${displayName} заблокував(-ла) листування з вами`}>
                      Листування заблоковано
                    </Button>
                  ) : (
                    <Button
                      icon={<IconChat size={15} />}
                      onClick={() => panel.openDirect({ userId: profile.id, username: displayName, avatarUrl: profile.avatarUrl })}
                    >
                      {dm?.canAccept ? "Відповісти на запит" : "Написати"}
                    </Button>
                  )}
                  <ButtonLink to={`/team/invite-to-team?userId=${profile.id}`} variant="primary" icon={<IconUsers size={15} />}>
                    Запросити до команди
                  </ButtonLink>
                  {(!dm || dm.status !== "blocked" || dm.canUnblock) && (
                    <Button variant="ghost" loading={dmBusy} onClick={() => void toggleBlock()}>
                      {dm?.canUnblock ? "Розблокувати" : "Заблокувати"}
                    </Button>
                  )}
                </>
              )
            )}
          </div>
          <div className={sh.profileNums}>
            <span>
              <b>{posts.length}</b>
              {plural(posts.length, ["робота", "роботи", "робіт"])}
            </span>
            <span>
              <b>{totalLikes}</b>
              {plural(totalLikes, ["вподобання", "вподобання", "вподобань"])}
            </span>
            <span>
              <b>{totalComments}</b>
              {plural(totalComments, ["коментар", "коментарі", "коментарів"])}
            </span>
            <span>
              <b>{teams.length}</b>
              {plural(teams.length, ["команда", "команди", "команд"])}
            </span>
          </div>
          <p className={sh.profileBio}>
            {profile.specialization && (
              <>
                {profile.specialization}
                <br />
              </>
            )}
            <span className={sh.mono}>
              {profile.userName ? `@${profile.userName}` : profile.email}
              {profile.createdAt ? ` · на платформі з ${new Date(profile.createdAt).toLocaleDateString("uk-UA")}` : ""}
            </span>
          </p>
        </div>
      </header>

      <div className={sh.centerTabs}>
        <Tabs
          label="Розділи профілю"
          value={tab}
          onChange={setTab}
          items={[
            { value: "portfolio", label: "Портфоліо", count: posts.length },
            { value: "teams", label: "Команди", count: teams.length },
          ]}
        />
      </div>
      <div style={{ paddingTop: "var(--sp-5)" }}>
        {tab === "portfolio" && (
          <Portfolio
            posts={posts}
            empty={{
              title: isMe ? "Ваше портфоліо поки порожнє" : "Портфоліо поки порожнє",
              text: isMe ? "Опублікуйте першу роботу — вона з'явиться тут." : "Ця людина ще не публікувала робіт.",
              action: isMe ? <ButtonLink to="/posts/new">Створити пост</ButtonLink> : undefined,
            }}
          />
        )}
        {tab === "teams" && (
          <UserTeams
            teams={teams}
            userId={profile.id}
            empty={{
              title: isMe ? "Ви ще не в жодній команді" : "Ця людина поки не в командах",
              action: isMe ? <ButtonLink to="/teams">Знайти команду</ButtonLink> : undefined,
            }}
          />
        )}
      </div>
    </Page>
  );
}
