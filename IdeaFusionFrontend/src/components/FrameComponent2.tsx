import { FunctionComponent } from "react";
import { Box, Typography } from "@mui/material";
import { useNavigate, useLocation } from "react-router-dom";
import { useAuth } from "../context/AuthContext";
import styles from "./FrameComponent2.module.css";

export type FrameComponent2Type = {
  className?: string;
};

const FrameComponent2: FunctionComponent<FrameComponent2Type> = ({
  className = "",
}) => {
  const navigate = useNavigate();
  const location = useLocation();
  const path = location.pathname;
  const { user } = useAuth();

  const userInitials = user
    ? user.email.split('@')[0].slice(0, 2).toUpperCase()
    : null;

  const isActive = (routes: string[]) => routes.includes(path);

  const navItems = [
    {
      label: "Стрічка",
      routes: ["/home", "/posts/new", "/posts/edit"],
      onClick: () => navigate("/home"),
      icon: (active: boolean) => (
        <svg className={active ? styles.sidebarIconActive : styles.sidebarIcon} viewBox="0 0 24 24" fill="none" xmlns="http://www.w3.org/2000/svg">
          <path d="M3 9.5L12 3L21 9.5V20C21 20.5523 20.5523 21 20 21H15V15H9V21H4C3.44772 21 3 20.5523 3 20V9.5Z"
            stroke={active ? "white" : "#6D6C6C"} strokeWidth="1.8" strokeLinecap="round" strokeLinejoin="round" />
        </svg>
      ),
    },
    {
      label: "Колаборації",
      routes: ["/collaborations", "/team", "/team/new", "/team/invite", "/team/join-request", "/team/invite-to-team", "/team/join"],
      onClick: () => navigate("/collaborations"),
      icon: (active: boolean) => (
        <svg className={active ? styles.sidebarIconActive : styles.sidebarIcon} viewBox="0 0 24 24" fill="none" xmlns="http://www.w3.org/2000/svg">
          <circle cx="9" cy="8" r="3" stroke={active ? "white" : "#6D6C6C"} strokeWidth="1.8" />
          <circle cx="17" cy="10" r="2.5" stroke={active ? "white" : "#6D6C6C"} strokeWidth="1.8" />
          <path d="M2 19C2 16.2386 5.13401 14 9 14C12.866 14 16 16.2386 16 19" stroke={active ? "white" : "#6D6C6C"} strokeWidth="1.8" strokeLinecap="round" />
          <path d="M16 16C17.0 15.37 18.4 15 20 15C21.657 15 22 15.9 22 17.5" stroke={active ? "white" : "#6D6C6C"} strokeWidth="1.8" strokeLinecap="round" />
        </svg>
      ),
    },
    {
      label: "Збережене",
      routes: ["/saved"],
      onClick: () => navigate("/saved"),
      icon: (active: boolean) => (
        <svg className={active ? styles.sidebarIconActive : styles.sidebarIcon} viewBox="0 0 24 24" fill={active ? "white" : "none"} xmlns="http://www.w3.org/2000/svg">
          <polygon points="12 2 15.09 8.26 22 9.27 17 14.14 18.18 21.02 12 17.77 5.82 21.02 7 14.14 2 9.27 8.91 8.26 12 2"
            stroke={active ? "white" : "#6D6C6C"} strokeWidth="1.8" strokeLinecap="round" strokeLinejoin="round" />
        </svg>
      ),
    },
    {
      label: "Пошук",
      routes: ["/search"],
      onClick: () => navigate("/search"),
      icon: (active: boolean) => (
        <svg className={active ? styles.sidebarIconActive : styles.sidebarIcon} viewBox="0 0 24 24" fill="none" xmlns="http://www.w3.org/2000/svg">
          <circle cx="11" cy="11" r="7" stroke={active ? "white" : "#6D6C6C"} strokeWidth="1.8" />
          <path d="M16.5 16.5L21 21" stroke={active ? "white" : "#6D6C6C"} strokeWidth="1.8" strokeLinecap="round" />
        </svg>
      ),
    },
    {
      label: "Дашборд",
      routes: ["/dashboard", "/profile", "/portfolio"],
      onClick: () => navigate("/dashboard"),
      icon: (active: boolean) => (
        <svg className={active ? styles.sidebarIconActive : styles.sidebarIcon} viewBox="0 0 24 24" fill="none" xmlns="http://www.w3.org/2000/svg">
          <rect x="3" y="3" width="8" height="8" rx="1.5" stroke={active ? "white" : "#6D6C6C"} strokeWidth="1.8" />
          <rect x="13" y="3" width="8" height="8" rx="1.5" stroke={active ? "white" : "#6D6C6C"} strokeWidth="1.8" />
          <rect x="3" y="13" width="8" height="8" rx="1.5" stroke={active ? "white" : "#6D6C6C"} strokeWidth="1.8" />
          <rect x="13" y="13" width="8" height="8" rx="1.5" stroke={active ? "white" : "#6D6C6C"} strokeWidth="1.8" />
        </svg>
      ),
    },
  ];

  return (
    <>
      <section className={[styles.rectangleParent, className].join(" ")}>
        <Box className={styles.frameChild} />
        <Box className={styles.frameWrapper}>
          <img
            className={styles.frameItem}
            loading="lazy"
            alt=""
            src="/Group-1@2x.png"
          />
        </Box>
        <Box className={styles.frameParent}>
          {navItems.map((item) => {
            const active = isActive(item.routes);
            return active ? (
              <Box key={item.label} className={styles.activeNavItem} title={item.label}>
                {item.icon(true)}
              </Box>
            ) : (
              <button
                key={item.label}
                className={styles.frameButton}
                onClick={item.onClick}
                title={item.label}
              >
                {item.icon(false)}
              </button>
            );
          })}
        </Box>
        <Box
          className={styles.profileInfo}
          onClick={() => navigate(user ? "/personal" : "/login")}
          sx={{ cursor: "pointer" }}
          title={user ? "Профіль" : "Увійти"}
        >
          {user?.avatarUrl ? (
            <img
              src={user.avatarUrl}
              alt="avatar"
              style={{ position: "absolute", top: 0, left: 0, width: "100%", height: "100%", borderRadius: "50%", objectFit: "cover", zIndex: 3 }}
            />
          ) : user ? (
            <>
              <Box className={styles.profileAvatar} />
              <Typography
                className={styles.h3}
                variant="inherit"
                variantMapping={{ inherit: "h3" }}
                sx={{ fontWeight: "700" }}
              >
                {userInitials}
              </Typography>
            </>
          ) : (
            <>
              <Box className={styles.profileAvatar} />
              <svg
                style={{ position: "absolute", top: "50%", left: "50%", transform: "translate(-50%, -50%)", zIndex: 3 }}
                width="28" height="28" viewBox="0 0 24 24" fill="none"
                xmlns="http://www.w3.org/2000/svg"
              >
                <path d="M15 3h4a2 2 0 0 1 2 2v14a2 2 0 0 1-2 2h-4" stroke="white" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round"/>
                <polyline points="10 17 15 12 10 7" stroke="white" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round"/>
                <line x1="15" y1="12" x2="3" y2="12" stroke="white" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round"/>
              </svg>
            </>
          )}
        </Box>
      </section>

      {/* ── Bottom navigation (mobile only) ── */}
      <nav className={styles.bottomNav}>
        {navItems.map((item) => {
          const active = isActive(item.routes);
          return (
            <button
              key={item.label}
              className={`${styles.bottomNavItem} ${active ? styles.bottomNavItemActive : ""}`}
              onClick={item.onClick}
            >
              {item.icon(active)}
              <span className={`${styles.bottomNavLabel} ${active ? styles.bottomNavLabelActive : ""}`}>
                {item.label}
              </span>
            </button>
          );
        })}
        <button
          className={styles.bottomNavItem}
          onClick={() => navigate(user ? "/personal" : "/login")}
        >
          <div className={styles.bottomNavAvatar}>
            {user?.avatarUrl ? (
              <img src={user.avatarUrl} alt="" className={styles.bottomNavAvatarImg} />
            ) : (
              <span>{userInitials || "?"}</span>
            )}
          </div>
          <span className={styles.bottomNavLabel}>Профіль</span>
        </button>
      </nav>
    </>
  );
};

export default FrameComponent2;
