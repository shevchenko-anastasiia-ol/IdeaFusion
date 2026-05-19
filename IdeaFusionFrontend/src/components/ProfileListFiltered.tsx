import { FunctionComponent } from "react";
import { Box, Typography } from "@mui/material";
import styles from "./ProfileList.module.css";
import { useNavigate } from "react-router-dom";

type User = {
  id: number;
  name: string;
  initials: string;
  avatarColor: string;
  role: string;
  projects: number;
};

const ALL_USERS: User[] = [
  { id: 1, name: "Катерина Гончар",  initials: "КГ", avatarColor: "var(--color-salmon)",               role: "UI/UX дизайнер",   projects: 3  },
  { id: 2, name: "Андрій Власенко",  initials: "АВ", avatarColor: "var(--color-mediumaquamarine-100)", role: "3D артист",         projects: 23 },
  { id: 3, name: "Юлія Бондар",      initials: "ЮБ", avatarColor: "var(--color-goldenrod)",            role: "Композитор",        projects: 19 },
  { id: 4, name: "Дмитро Мельник",   initials: "ДМ", avatarColor: "#4002aa",                           role: "Розробник React",   projects: 14 },
  { id: 5, name: "Максим Сидоренко", initials: "МС", avatarColor: "var(--color-mediumaquamarine-100)", role: "Музичний продюсер", projects: 9  },
  { id: 6, name: "Ірина Лисенко",    initials: "ІЛ", avatarColor: "#29b6f6",                           role: "Концепт-художник",  projects: 31 },
];

export type ProfileListFilteredType = {
  className?: string;
  query?: string;
};

const UserCard: FunctionComponent<{ user: User }> = ({ user }) => {
  const navigate = useNavigate();
  return (
    <Box
      sx={{ alignSelf: "stretch", display: "flex", alignItems: "flex-start", maxWidth: "100%" }}
      onClick={() => navigate("/users")}
    >
      <Box
        className={styles.rectangleParent}
        sx={{ cursor: "pointer", flex: 1, "&:hover": { backgroundColor: "var(--color-gray-400, #202020)" } }}
      >
        <Box className={styles.frameChild} />
        <Box className={styles.profilePictureParent}>
          <Box className={styles.profilePicture} style={{ backgroundColor: user.avatarColor }} />
          <Typography
            className={styles.h3}
            variant="inherit"
            variantMapping={{ inherit: "h3" }}
            sx={{ fontWeight: "700" }}
          >
            {user.initials}
          </Typography>
        </Box>
        <Box className={styles.detailLayer}>
          <Box className={styles.captionPanel}>
            <Box className={styles.fullNamePlate}>
              <Typography
                className={styles.h32}
                variant="inherit"
                variantMapping={{ inherit: "h3" }}
                sx={{ fontWeight: "600" }}
              >
                {user.name}
              </Typography>
            </Box>
            <div className={styles.uiux}>{user.role}</div>
          </Box>
        </Box>
        <Box className={styles.wrapper}>
          <div className={styles.div}>{user.projects} проєкти</div>
        </Box>
      </Box>
    </Box>
  );
};

const ProfileListFiltered: FunctionComponent<ProfileListFilteredType> = ({
  className = "",
  query = "",
}) => {
  const q = query.trim().toLowerCase();
  const filtered = q
    ? ALL_USERS.filter(
        (u) =>
          u.name.toLowerCase().includes(q) ||
          u.role.toLowerCase().includes(q)
      )
    : ALL_USERS;

  if (filtered.length === 0) {
    return (
      <Box
        sx={{
          color: "#555",
          fontSize: 15,
          fontFamily: "var(--font-inter)",
          py: "8px",
          pl: "13px",
        }}
      >
        Нічого не знайдено
      </Box>
    );
  }

  return (
    <Box className={[styles.profileList, className].join(" ")} sx={{ gap: "12px !important" }}>
      {filtered.map((user) => (
        <UserCard key={user.id} user={user} />
      ))}
    </Box>
  );
};

export default ProfileListFiltered;