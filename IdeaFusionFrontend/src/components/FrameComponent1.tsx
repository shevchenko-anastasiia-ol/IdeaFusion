import { FunctionComponent, useEffect, useState } from "react";
import { Box, Typography } from "@mui/material";
import FrameComponent from "./FrameComponent";
import styles from "./FrameComponent1.module.css";
import { postsApi } from "../api/posts";
import { teamsApi } from "../api/teams";
import { api } from "../api/client";
import type { UserDto } from "../api/types";
import { useAuth } from "../context/AuthContext";

const AVATAR_PALETTE = ["#4003aa","#ffb347","#00c9a7","#ff6b6b","#29b6f6","#e040fb","#7c5cfc","#ef5350"];

function avatarColor(seed: string): string {
  let h = 0;
  for (let i = 0; i < seed.length; i++) h = (h * 31 + seed.charCodeAt(i)) >>> 0;
  return AVATAR_PALETTE[h % AVATAR_PALETTE.length];
}

type TopUser = { identityId: string; userName: string; avatarUrl?: string; count: number };

export type FrameComponent1Type = {
  className?: string;
};

const FrameComponent1: FunctionComponent<FrameComponent1Type> = ({ className = "" }) => {
  const { user } = useAuth();
  const currentUserName = user?.userName ?? user?.email?.split('@')[0];
  const [topUsers, setTopUsers] = useState<TopUser[]>([]);

  useEffect(() => {
    // Users are essential — fetch them first. Posts/teams are best-effort for ranking only.
    api.get<UserDto[]>('/api/users').then(users => {
      const userById = new Map<string, UserDto>(users.map(u => [u.id, u]));
      const userByName = new Map<string, UserDto>(
        users.map(u => [(u.userName ?? u.email.split('@')[0]).toLowerCase(), u])
      );

      const counts = new Map<string, TopUser>();
      for (const u of users) {
        const uName = u.userName ?? u.email.split('@')[0];
        if (uName === currentUserName) continue;
        counts.set(u.id, { identityId: u.id, userName: uName, avatarUrl: u.avatarUrl, count: 0 });
      }

      function addCount(identityUser: UserDto) {
        const uName = identityUser.userName ?? identityUser.email.split('@')[0];
        if (uName === currentUserName) return;
        const existing = counts.get(identityUser.id);
        if (existing) existing.count++;
      }

      Promise.allSettled([
        postsApi.getAll(),
        teamsApi.getPaged(1, 200),
      ]).then(([postsRes, teamsRes]) => {
        const posts = postsRes.status === 'fulfilled' ? postsRes.value : [];
        const teams = teamsRes.status === 'fulfilled' ? (teamsRes.value?.items ?? teamsRes.value ?? []) : [];

        const teamMembersMap = new Map<string, Set<string>>();
        for (const team of teams) {
          const memberIds = new Set<string>(
            team.members.map(m => m.user.userId).filter(uid => userById.has(uid))
          );
          if (memberIds.size > 0) teamMembersMap.set(team.name, memberIds);
        }

        for (const post of posts) {
          if (post.author) {
            let u = userByName.get(post.author.userName.toLowerCase());
            if (!u && post.createdBy) u = userById.get(post.createdBy);
            if (u) addCount(u);
          } else if (post.collaboration?.name) {
            const memberIds = teamMembersMap.get(post.collaboration.name);
            if (memberIds) {
              for (const uid of memberIds) {
                const u = userById.get(uid);
                if (u) addCount(u);
              }
            }
          }
        }

        setTopUsers([...counts.values()].sort((a, b) => b.count - a.count).slice(0, 5));
      });
    }).catch(() => {});
  }, [currentUserName]);

  return (
    <section className={[styles.rectangleParent, className].join(" ")}>
      <Box className={styles.frameChild} />
      <Box className={styles.wrapper}>
        <Typography
          className={styles.h1}
          variant="inherit"
          variantMapping={{ inherit: "h1" }}
          sx={{ fontWeight: "600" }}
        >
          Рекомендовані
        </Typography>
      </Box>
      <Box className={styles.frameWrapper}>
        <Box className={styles.frameParent}>
          {topUsers.length === 0 ? (
            <Typography sx={{ color: "#666", fontSize: 14, padding: "8px 0" }}>
              Поки немає рекомендацій
            </Typography>
          ) : topUsers.map((u) => (
            <FrameComponent
              key={u.identityId}
              ellipseBoxBackgroundColor={avatarColor(u.identityId)}
              prop={u.userName.slice(0, 2).toUpperCase()}
              prop1={u.userName}
              react={`${u.count} ${u.count === 1 ? 'пост' : u.count < 5 ? 'пости' : 'постів'}`}
              avatarUrl={u.avatarUrl}
              userId={u.identityId}
            />
          ))}
        </Box>
      </Box>
      <Box className={styles.divider} />
      <Box className={styles.frameGroup}>
        <Box className={styles.container}>
          <Typography
            className={styles.h1}
            variant="inherit"
            variantMapping={{ inherit: "h2" }}
            sx={{ fontWeight: "600" }}
          >
            Популярні теги
          </Typography>
        </Box>
        <Box className={styles.frameContainer}>
          <Box className={styles.uiWrapper}>
            <Typography className={styles.ui} variant="inherit" variantMapping={{ inherit: "h3" }} sx={{ fontWeight: "400" }}>
              #design
            </Typography>
          </Box>
          <Box className={styles.frame}>
            <Typography className={styles.ui} variant="inherit" variantMapping={{ inherit: "h3" }} sx={{ fontWeight: "400" }}>
              #music
            </Typography>
          </Box>
          <Typography className={styles.ui} variant="inherit" variantMapping={{ inherit: "h3" }} sx={{ fontWeight: "400" }}>
            #animation
          </Typography>
          <Typography className={styles.ui} variant="inherit" variantMapping={{ inherit: "h3" }} sx={{ fontWeight: "400" }}>
            #art
          </Typography>
          <Typography className={styles.ui} variant="inherit" variantMapping={{ inherit: "h3" }} sx={{ fontWeight: "400" }}>
            #gamedev
          </Typography>
        </Box>
      </Box>
    </section>
  );
};

export default FrameComponent1;