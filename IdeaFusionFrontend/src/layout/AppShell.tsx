import { useEffect, useMemo, useState, type ReactNode } from "react";
import { Link, NavLink, Outlet, useLocation, useNavigate } from "react-router-dom";
import { useAuth } from "../context/AuthContext";
import { useChat } from "../context/ChatContext";
import { Avatar, ButtonLink, IconButton, Menu, cx } from "../ui";
import {
  IconChat,
  IconChevronDown,
  IconClose,
  IconFeed,
  IconLock,
  IconLogin,
  IconLogout,
  IconMenu,
  IconPlus,
  IconSearch,
  IconSidebar,
  IconUser,
  IconUsers,
} from "../ui/icons";
import { NAV_GROUPS, activeNavTo, type NavItem } from "./nav";
import { PageMetaProvider, useCrumbs } from "./PageMeta";
import { ChatPanel, useChatPanel } from "../components/ChatWidget";
import { useDirectChat } from "../context/DirectChatContext";
import s from "./AppShell.module.css";

const COLLAPSE_KEY = "ui:sidebarCollapsed";

function readCollapsed(): boolean {
  try {
    return localStorage.getItem(COLLAPSE_KEY) === "1";
  } catch {
    return false;
  }
}

export function Logo({ compact }: { compact?: boolean }) {
  return (
    <Link to="/home" className={s.logo} aria-label="IdeaFusion — на головну">
      <img className={s.mark} src="/logo.svg" alt="" aria-hidden="true" />
      {!compact && (
        <span className={s.wordmark}>
          Idea <span>Fusion</span>
        </span>
      )}
    </Link>
  );
}

function useVisibleGroups() {
  const { isAuthenticated, isModerator, isAdministrator } = useAuth();
  return useMemo(
    () =>
      NAV_GROUPS.map(g => ({
        ...g,
        items: g.items.filter(i => {
          if (i.requires === "auth") return isAuthenticated;
          if (i.requires === "moderator") return isModerator;
          if (i.requires === "administrator") return isAdministrator;
          return true;
        }),
      })).filter(g => g.items.length > 0),
    [isAuthenticated, isModerator, isAdministrator],
  );
}

function displayName(user: { fullName?: string; userName?: string; email: string } | null): string {
  if (!user) return "";
  return user.fullName || user.userName || user.email.split("@")[0];
}

function roleLabel(roles: string[]): string {
  if (roles.includes("administrator")) return "адміністратор";
  if (roles.includes("moderator")) return "модератор";
  return "автор";
}

function SidebarNav({ collapsed, onNavigate }: { collapsed: boolean; onNavigate?: () => void }) {
  const groups = useVisibleGroups();
  const { pathname } = useLocation();
  const { isAuthenticated } = useAuth();
  const all = groups.flatMap(g => g.items);
  const active = activeNavTo(pathname, all);
  return (
    <nav className={s.nav} aria-label="Основна навігація">
      {groups.map((g, gi) => (
        <div key={g.label || gi} className={cx(s.navGroup, !g.label && s.navGroupPlain)}>
          {/* Kept (invisible) when collapsed, so the items below stay at the same height. */}
          {g.label && <span className={s.navGroupLabel} aria-hidden={collapsed || undefined}>{g.label}</span>}
          <ul>
            {g.items.map(item => {
              return (
                <li key={item.to}>
                  <NavLink
                    to={item.to}
                    onClick={onNavigate}
                    className={cx(s.navItem, active === item.to && s.navItemActive)}
                    aria-current={active === item.to ? "page" : undefined}
                    title={collapsed ? item.label : undefined}
                    end
                  >
                    {/* The same icon open or collapsed, so the two states are recognisably one menu. */}
                    <span className={s.navIcon} aria-hidden="true">
                      {item.icon}
                    </span>
                    {!collapsed && <span>{item.label}</span>}
                  </NavLink>
                </li>
              );
            })}
          </ul>
        </div>
      ))}
      {isAuthenticated && (
        <Link
          to="/posts/new"
          onClick={onNavigate}
          className={cx(s.newPost, collapsed && s.newPostCompact)}
          aria-label="Новий пост"
          title={collapsed ? "Новий пост" : undefined}
        >
          <IconPlus size={18} />
          {!collapsed && <span>Новий пост</span>}
        </Link>
      )}
    </nav>
  );
}

function UserBlock({ collapsed }: { collapsed: boolean }) {
  const { user, isAuthenticated, roles, logout, loading } = useAuth();
  const navigate = useNavigate();

  if (loading) return <div className={s.userBlock} />;

  if (!isAuthenticated || !user) {
    return (
      <div className={s.userBlock}>
        {collapsed ? (
          <Link to="/login" className={cx(s.navItem)} title="Увійти" aria-label="Увійти">
            <IconLogin />
          </Link>
        ) : (
          <ButtonLink to="/login" variant="primary" block icon={<IconLogin size={16} />}>
            Увійти
          </ButtonLink>
        )}
      </div>
    );
  }

  const name = displayName(user);
  return (
    <div className={s.userBlock}>
      <Menu
        placement="up"
        items={[
          { label: "Особиста сторінка", icon: <IconUser size={16} />, onSelect: () => navigate("/personal") },
          { label: "Публічний профіль", icon: <IconUsers size={16} />, onSelect: () => navigate(`/users/${user.id}`) },
          { label: "Безпека", icon: <IconLock size={16} />, onSelect: () => navigate("/security") },
          { separator: true },
          { label: "Вийти", icon: <IconLogout size={16} />, danger: true, onSelect: () => void logout() },
        ]}
        trigger={props => (
          <button type="button" className={cx(s.userButton, collapsed && s.userButtonCollapsed)} {...props} aria-label="Меню акаунта">
            <Avatar name={name} src={user.avatarUrl} seed={user.id} size={30} />
            {!collapsed && (
              <>
                <span className={s.userText}>
                  <span className={s.userName}>{name}</span>
                  <span className={s.userRole}>{roleLabel(roles)}</span>
                </span>
                <IconChevronDown size={16} />
              </>
            )}
          </button>
        )}
      />
    </div>
  );
}

function Contacts() {
  return (
    <div className={s.contacts}>
      <p>
        <span>Підтримка</span>
        <a href="mailto:support@ideafusion.com">support@ideafusion.com</a>
        <span className={s.mono}>+380 66 444 3322</span>
      </p>
      <p>
        <span>Модерація</span>
        <a href="mailto:moderators@ideafusion.com">moderators@ideafusion.com</a>
        <span className={s.mono}>+380 66 757 4343</span>
      </p>
    </div>
  );
}

function SiteFooter() {
  return (
    <footer className={s.footer}>
      <div className={s.footerBrand}>
        <span className={s.footerName}>
          <img src="/logo.svg" alt="" width={18} height={18} />
          <span>IdeaFusion</span>
        </span>
        <span className={s.footerTag}>платформа творчих колаборацій</span>
      </div>
      <div className={s.footerCols}>
        <p>
          <span>Служба підтримки</span>
          <a href="mailto:support@ideafusion.com">support@ideafusion.com</a>
          <span className={s.mono}>+380 66 444 3322</span>
        </p>
        <p>
          <span>З питань модерації</span>
          <a href="mailto:moderators@ideafusion.com">moderators@ideafusion.com</a>
          <span className={s.mono}>+380 66 757 4343</span>
        </p>
        <p>
          <span>Власник платформи</span>
          <span>Шевченко Анастасія Олександрівна</span>
          <span className={s.mono}>Instagram: _.anasteishen_</span>
        </p>
      </div>
    </footer>
  );
}

function TopBar({ onOpenDrawer }: { onOpenDrawer: () => void }) {
  const crumbs = useCrumbs();
  const { isAuthenticated } = useAuth();
  const { chats, isUnread } = useChat();
  const panel = useChatPanel();
  const navigate = useNavigate();
  const direct = useDirectChat();
  // Team chats with new messages, plus direct chats with new messages or a request to answer.
  const unread =
    chats.filter(c => isUnread(c)).length + direct.conversations.filter(c => c.canAccept || direct.isUnread(c)).length;

  return (
    <header className={s.topbar}>
      <div className={s.topbarLeft}>
        <IconButton label="Відкрити меню" className={s.menuBtn} onClick={onOpenDrawer}>
          <IconMenu />
        </IconButton>
        <span className={s.mobileLogo}>
          <Logo compact />
        </span>
        {crumbs.length > 1 && (
        <nav className={s.crumbs} aria-label="Шлях">
          {crumbs.map((c, i) => (
            <span key={i} className={s.crumb}>
              {i > 0 && <span className={s.crumbSep} aria-hidden="true">/</span>}
              {c.to && i < crumbs.length - 1 ? <Link to={c.to}>{c.label}</Link> : <span aria-current={i === crumbs.length - 1 ? "page" : undefined}>{c.label}</span>}
            </span>
          ))}
        </nav>
        )}
      </div>
      <div className={s.topbarRight}>
        <button
          type="button"
          className={s.searchTrigger}
          onClick={() => navigate("/search", { state: { focus: Date.now() } })}
          title="Відкрити пошук (Ctrl+K)"
        >
          <IconSearch size={16} />
          Пошук
          <kbd>Ctrl K</kbd>
        </button>
        <IconButton label="Пошук" className={s.searchIconBtn} onClick={() => navigate("/search")}>
          <IconSearch />
        </IconButton>
        {isAuthenticated && (
          <>
            <span className={s.chatBtnWrap}>
              <IconButton label="Чати команд" onClick={panel.toggle} aria-pressed={panel.open}>
                <IconChat />
              </IconButton>
              {unread > 0 && <span className={s.unread}>{unread > 9 ? "9+" : unread}</span>}
            </span>
          </>
        )}
      </div>
    </header>
  );
}

function BottomTabs() {
  const { pathname } = useLocation();
  const { isAuthenticated, user } = useAuth();
  const tabs: (NavItem & { primary?: boolean })[] = [
    { label: "Стрічка", to: "/home", icon: <IconFeed />, match: ["/home", "/posts"] },
    { label: "Пошук", to: "/search", icon: <IconSearch />, match: ["/search"] },
    ...(isAuthenticated ? [{ label: "Створити", to: "/posts/new", icon: <IconPlus />, match: ["/posts/new"], primary: true }] : []),
    { label: "Команди", to: "/collaborations", icon: <IconUsers />, match: ["/collaborations", "/team", "/teams"] },
    isAuthenticated
      ? { label: "Профіль", to: "/personal", icon: <Avatar name={user ? displayName(user) : ""} src={user?.avatarUrl} seed={user?.id} size={22} />, match: ["/personal", "/security"] }
      : { label: "Увійти", to: "/login", icon: <IconLogin />, match: ["/login"] },
  ];
  const active = activeNavTo(pathname, tabs);
  return (
    <nav className={s.bottomTabs} aria-label="Швидка навігація">
      {tabs.map(t => (
        <Link
          key={t.to}
          to={t.to}
          className={cx(s.tab, active === t.to && s.tabActive, t.primary && s.tabPrimary)}
          aria-current={active === t.to ? "page" : undefined}
        >
          <span className={s.tabIcon}>{t.icon}</span>
          <span className={s.tabLabel}>{t.label}</span>
        </Link>
      ))}
    </nav>
  );
}

function ShellInner({ children }: { children?: ReactNode }) {
  const [collapsed, setCollapsed] = useState(readCollapsed);
  const [drawer, setDrawer] = useState(false);
  const { pathname } = useLocation();
  const navigate = useNavigate();

  useEffect(() => setDrawer(false), [pathname]);

  useEffect(() => {
    // Already on the search page: keep the current query and only move the cursor there.
    const goSearch = () =>
      navigate(
        { pathname: "/search", search: window.location.pathname === "/search" ? window.location.search : "" },
        { state: { focus: Date.now() } },
      );
    function onKey(e: KeyboardEvent) {
      const target = e.target as HTMLElement | null;
      const typing = target && (target.tagName === "INPUT" || target.tagName === "TEXTAREA" || target.isContentEditable);
      // e.code is the physical key, so the shortcuts work in any layout: with the Ukrainian
      // one e.key is "л" for K and "." for /, the check never matched and Chrome ran its own
      // Ctrl+K (search the web from the address bar).
      if ((e.ctrlKey || e.metaKey) && !e.altKey && !e.shiftKey && (e.code === "KeyK" || e.key.toLowerCase() === "k")) {
        e.preventDefault();
        goSearch();
      } else if ((e.code === "Slash" || e.key === "/") && !typing && !e.ctrlKey && !e.metaKey && !e.altKey) {
        e.preventDefault();
        goSearch();
      }
    }
    window.addEventListener("keydown", onKey);
    return () => window.removeEventListener("keydown", onKey);
  }, [navigate]);

  function toggleCollapsed() {
    setCollapsed(c => {
      try {
        localStorage.setItem(COLLAPSE_KEY, c ? "0" : "1");
      } catch {
        /* storage unavailable: the choice just isn't remembered */
      }
      return !c;
    });
  }

  return (
    <div className={cx(s.shell, collapsed && s.collapsed)}>
      <a href="#main" className={s.skip}>
        Перейти до вмісту
      </a>
      <aside className={s.sidebar}>
        <div className={s.sidebarTop}>
          <Logo compact={collapsed} />
          <IconButton label={collapsed ? "Розгорнути панель" : "Згорнути панель"} size="sm" onClick={toggleCollapsed} className={s.collapseBtn}>
            <IconSidebar size={16} />
          </IconButton>
        </div>
        <SidebarNav collapsed={collapsed} />
        <div className={s.sidebarBottom}>
          <UserBlock collapsed={collapsed} />
        </div>
      </aside>

      {drawer && (
        <div className={s.drawerOverlay} onMouseDown={e => e.target === e.currentTarget && setDrawer(false)}>
          <aside className={s.drawer} role="dialog" aria-modal="true" aria-label="Меню">
            <div className={s.sidebarTop}>
              <Logo />
              <IconButton label="Закрити меню" onClick={() => setDrawer(false)}>
                <IconClose />
              </IconButton>
            </div>
            <SidebarNav collapsed={false} onNavigate={() => setDrawer(false)} />
            <div className={s.sidebarBottom}>
              <UserBlock collapsed={false} />
              <Contacts />
            </div>
          </aside>
        </div>
      )}

      <div className={s.main}>
        <TopBar onOpenDrawer={() => setDrawer(true)} />
        <main id="main" className={s.content} tabIndex={-1}>
          {children ?? <Outlet />}
        </main>
        <SiteFooter />
      </div>
      <BottomTabs />
      <ChatPanel />
    </div>
  );
}

export default function AppShell({ children }: { children?: ReactNode }) {
  return (
    <PageMetaProvider>
      <ShellInner>{children}</ShellInner>
    </PageMetaProvider>
  );
}

/** Content width wrapper used by every page inside the shell. */
export function Page({ children, width = "default" }: { children: ReactNode; width?: "narrow" | "default" | "wide" | "feed" }) {
  return <div className={cx(s.page, s[`page-${width}`])}>{children}</div>;
}
