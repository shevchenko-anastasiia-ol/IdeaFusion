import { FunctionComponent } from "react";
import { Typography, Box } from "@mui/material";
import styles from "./FrameComponent1111.module.css";
import { useNavigate } from "react-router-dom";

type Team = {
  id: string;
  name: string;
  status: "Активна" | "У пошуку";
  statusColor: string;
  statusBg: string;
  statusBorder: string;
  accentColor: string;
  tags: string;
  category: string;
  categoryStyle: string;
  members: { initials: string; color: string }[];
  extra?: string;
  need?: string;
  publications: number;
  dividerClass: string;
};

const ALL_TEAMS: Team[] = [
  {
    id: "bureaucrats",
    name: "Бюрократи",
    status: "Активна",
    statusColor: "var(--color-mediumslateblue-200)",
    statusBg: "var(--color-mediumslateblue-300)",
    statusBorder: "var(--color-mediumslateblue-200)",
    accentColor: "var(--color-mediumslateblue-200)",
    tags: "UI/UX дизайн · Figma · Анімації",
    category: "Музика",
    categoryStyle: "darkslategray",
    members: [
      { initials: "ДМ", color: "var(--color-darkblue)" },
      { initials: "ЮБ", color: "var(--color-salmon)" },
      { initials: "АВ", color: "var(--color-goldenrod)" },
    ],
    extra: "+2",
    publications: 2,
    dividerClass: styles.frameInner,
  },
  {
    id: "art-future",
    name: "Арт майбутнього",
    status: "У пошуку",
    statusColor: "var(--color-mediumaquamarine-100)",
    statusBg: "var(--color-mediumaquamarine-200)",
    statusBorder: "var(--color-mediumaquamarine-100)",
    accentColor: "var(--color-mediumaquamarine-100)",
    tags: "Графічний дизайн · Illustrator · Арт",
    category: "Дизайн",
    categoryStyle: "purple",
    members: [{ initials: "ДМ", color: "var(--color-darkblue)" }],
    need: "Потрібен графічний дизайнер",
    publications: 0,
    dividerClass: styles.teamDivisionChild,
  },
];

export type FrameComponent1111FilteredType = {
  className?: string;
  query?: string;
};

const FrameComponent1111Filtered: FunctionComponent<FrameComponent1111FilteredType> = ({
  className = "",
  query = "",
}) => {
  const navigate = useNavigate();
  const q = query.trim().toLowerCase();
  const filtered = q
    ? ALL_TEAMS.filter(
        (t) =>
          t.name.toLowerCase().includes(q) ||
          t.tags.toLowerCase().includes(q)
      )
    : ALL_TEAMS;

  if (filtered.length === 0) {
    return (
      <Box
        sx={{
          color: "#555",
          fontSize: 15,
          fontFamily: "var(--font-inter)",
          py: "8px",
        }}
      >
        Нічого не знайдено
      </Box>
    );
  }

  return (
    <section className={[styles.teamsHeadingParent, className].join(" ")}>
      <Box className={styles.teamsHeading}>
        <Typography
          className={styles.h3}
          variant="inherit"
          variantMapping={{ inherit: "h3" }}
          sx={{ fontWeight: "400" }}
        >
          Команди
        </Typography>
      </Box>
      <footer className={styles.teamsContentParent}>
        <Box className={styles.teamsContent}>
          <Box className={styles.parent}>
            <Typography
              className={styles.h32}
              variant="inherit"
              variantMapping={{ inherit: "h3" }}
              sx={{ fontWeight: "400", cursor: "pointer" }}
              onClick={(e) => { e.stopPropagation(); navigate("/teams"); }}
            >
              Всі команди →
            </Typography>
            <Box className={styles.frameChild} />
          </Box>
        </Box>

        <Box className={styles.teamDescriptions}>
          <Box className={styles.teamElements}>
            {filtered.map((team, idx) => {
              const isFirst = team.id === "bureaucrats";
              return isFirst ? (
                <Box
                  key={team.id}
                  className={styles.rectangleParent}
                  onClick={() => navigate("/team/public")}
                  sx={{ cursor: "pointer" }}
                >
                  <Box className={styles.frameItem} />
                  <Box className={team.dividerClass} />
                  <Box className={styles.teamInfo}>
                    <Box className={styles.teamSummary}>
                      <Box className={styles.teamVisuals}>
                        <Typography
                          className={styles.h1}
                          variant="inherit"
                          variantMapping={{ inherit: "h1" }}
                          sx={{ fontWeight: "700" }}
                        >
                          {team.name}
                        </Typography>
                        <Box className={styles.teamType}>
                          <Box
                            className={styles.rectangleGroup}
                            sx={{ backgroundColor: `${team.statusBg} !important`, borderColor: `${team.statusBorder} !important` }}
                          >
                            <Box className={styles.rectangleDiv} />
                            <Typography
                              className={styles.h33}
                              variant="inherit"
                              variantMapping={{ inherit: "h3" }}
                              sx={{ fontWeight: "400", color: team.statusColor }}
                            >
                              {team.status}
                            </Typography>
                          </Box>
                        </Box>
                      </Box>
                      <Box className={styles.skillDiversity}>
                        <Typography
                          className={styles.uiux}
                          variant="inherit"
                          variantMapping={{ inherit: "h3" }}
                          sx={{ fontWeight: "400" }}
                        >
                          {team.tags}
                        </Typography>
                        <Box className={styles.teamVisualsInner}>
                          <Box className={styles.rectangleContainer}>
                            <Box className={styles.frameChild2} />
                            <div className={styles.div}>{team.category}</div>
                          </Box>
                        </Box>
                      </Box>
                    </Box>
                  </Box>
                  <Box className={styles.teamParticipants}>
                    <Box className={styles.avatarListingParent}>
                      <Box className={styles.avatarListing}>
                        <Box className={styles.avatarsContainer}>
                          <Box className={styles.wrapper}>
                            <Typography
                              className={styles.h34}
                              variant="inherit"
                              variantMapping={{ inherit: "h3" }}
                              sx={{ fontWeight: "400" }}
                            >
                              Учасники
                            </Typography>
                          </Box>
                          <Box className={styles.avatarsInitials}>
                            {team.members.map((m) => (
                              <Box key={m.initials} className={styles.initialsVisual}>
                                <Box className={styles.avatarCircles} style={{ backgroundColor: m.color }} />
                                <Typography className={styles.h35} variant="inherit" variantMapping={{ inherit: "h3" }} sx={{ fontWeight: "700" }}>{m.initials}</Typography>
                              </Box>
                            ))}
                            {team.extra && (
                              <Box className={styles.creatorAvatar}>
                                <Box className={styles.creatorProfile}>
                                  <Box className={styles.ownerCircle} />
                                  <Typography className={styles.noAvatar} variant="inherit" variantMapping={{ inherit: "h3" }} sx={{ fontWeight: "300" }}>{team.extra}</Typography>
                                </Box>
                              </Box>
                            )}
                          </Box>
                        </Box>
                      </Box>
                      <Box className={styles.publicationCounts}>
                        <Box className={styles.contentCounters}>
                          <Typography className={styles.h38} variant="inherit" variantMapping={{ inherit: "h3" }} sx={{ fontWeight: "400" }}>
                            Кількість публікацій: {team.publications}
                          </Typography>
                        </Box>
                        <button className={styles.groupButton}>
                          <Box className={styles.frameChild3} />
                          <Typography className={styles.h39} variantMapping={{ inherit: "h3" }} sx={{ fontFamily: "var(--font-inter)", fontSize: "var(--fs-24)" }}>
                            Подати заявку
                          </Typography>
                        </button>
                      </Box>
                    </Box>
                  </Box>
                </Box>
              ) : (
                <Box
                  key={team.id}
                  className={styles.groupDiv}
                  onClick={() => navigate("/team/public")}
                  sx={{ cursor: "pointer" }}
                >
                  <Box className={styles.frameItem} />
                  <Box className={styles.teamDivision}>
                    <Box className={team.dividerClass} />
                  </Box>
                  <Box className={styles.futureVision}>
                    <Box className={styles.descriptionDetails}>
                      <Typography
                        className={styles.h1}
                        variant="inherit"
                        variantMapping={{ inherit: "h2" }}
                        sx={{ fontWeight: "700" }}
                      >
                        {team.name}
                      </Typography>
                      <Box
                        className={styles.rectangleParent2}
                        sx={{ backgroundColor: `${team.statusBg} !important`, borderColor: `${team.statusBorder} !important` }}
                      >
                        <Box className={styles.frameChild5} />
                        <Typography className={styles.h310} variant="inherit" variantMapping={{ inherit: "h3" }} sx={{ fontWeight: "400", color: team.statusColor }}>
                          {team.status}
                        </Typography>
                      </Box>
                    </Box>
                    <Box className={styles.skillDiversity}>
                      <Typography className={styles.uiux} variant="inherit" variantMapping={{ inherit: "h3" }} sx={{ fontWeight: "400" }}>
                        {team.tags}
                      </Typography>
                      <Box className={styles.domainExpertise}>
                        <Box className={styles.rectangleParent3}>
                          <Box className={styles.frameChild6} />
                          <div className={styles.div}>{team.category}</div>
                        </Box>
                      </Box>
                    </Box>
                  </Box>
                  <Box className={styles.communityEngagement}>
                    <Box className={styles.memberComposition}>
                      <Box className={styles.listingDetails}>
                        <Box className={styles.teamMemberOverview}>
                          <Typography className={styles.h34} variant="inherit" variantMapping={{ inherit: "h3" }} sx={{ fontWeight: "400" }}>
                            Учасники
                          </Typography>
                        </Box>
                        <Box className={styles.teammatePortraits}>
                          {team.members.map((m) => (
                            <Box key={m.initials} className={styles.initialsVisual}>
                              <Box className={styles.avatarCircles} style={{ backgroundColor: m.color }} />
                              <Typography className={styles.h35} variant="inherit" variantMapping={{ inherit: "h3" }} sx={{ fontWeight: "700" }}>{m.initials}</Typography>
                            </Box>
                          ))}
                          <Box className={styles.supportingRole}>
                            <Box className={styles.creatorProfile}>
                              <Box className={styles.defaultAvatar} />
                              <Typography className={styles.placeholderPic} variant="inherit" variantMapping={{ inherit: "h3" }} sx={{ fontWeight: "300" }}>+</Typography>
                            </Box>
                          </Box>
                        </Box>
                        {team.need && (
                          <Box className={styles.expertInvitation}>
                            <div className={styles.div3}>{team.need}</div>
                          </Box>
                        )}
                      </Box>
                    </Box>
                    <Box className={styles.publicationCounts}>
                      <Box className={styles.counterElements}>
                        <Typography className={styles.h38} variant="inherit" variantMapping={{ inherit: "h3" }} sx={{ fontWeight: "400" }}>
                          Кількість публікацій: {team.publications}
                        </Typography>
                      </Box>
                      <button className={styles.rectangleParent4}>
                        <Box className={styles.frameChild3} />
                        <div className={styles.div4}>Подати заявку</div>
                      </button>
                    </Box>
                  </Box>
                </Box>
              );
            })}
          </Box>
        </Box>
      </footer>
    </section>
  );
};

export default FrameComponent1111Filtered;