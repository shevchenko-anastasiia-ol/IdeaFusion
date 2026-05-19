import { FunctionComponent } from "react";
import { Typography, Box } from "@mui/material";
import styles from "./FrameComponent1111.module.css";
import { useNavigate } from "react-router-dom";

export type FrameComponent1111Type = {
  className?: string;
};

const FrameComponent1111: FunctionComponent<FrameComponent1111Type> = ({
  className = "",
}) => {
  const navigate = useNavigate();
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
          <Box className={styles.rectangleParent} onClick={() => navigate("/team/public")} sx={{ cursor: "pointer" }}>
              <Box className={styles.frameItem} />
              <Box className={styles.frameInner} />
              <Box className={styles.teamInfo}>
                <Box className={styles.teamSummary}>
                  <Box className={styles.teamVisuals}>
                    <Typography
                      className={styles.h1}
                      variant="inherit"
                      variantMapping={{ inherit: "h1" }}
                      sx={{ fontWeight: "700" }}
                    >
                      Бюрократи
                    </Typography>
                    <Box className={styles.teamType}>
                      <Box className={styles.rectangleGroup}>
                        <Box className={styles.rectangleDiv} />
                        <Typography
                          className={styles.h33}
                          variant="inherit"
                          variantMapping={{ inherit: "h3" }}
                          sx={{ fontWeight: "400" }}
                        >
                          Активна
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
                      UI/UX дизайн · Figma · Анімації
                    </Typography>
                    <Box className={styles.teamVisualsInner}>
                      <Box className={styles.rectangleContainer}>
                        <Box className={styles.frameChild2} />
                        <div className={styles.div}>Музика</div>
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
                        <Box className={styles.initialsVisual}>
                          <Box className={styles.avatarCircles} />
                          <Typography
                            className={styles.h35}
                            variant="inherit"
                            variantMapping={{ inherit: "h3" }}
                            sx={{ fontWeight: "700" }}
                          >
                            ДМ
                          </Typography>
                        </Box>
                        <Box className={styles.initialsVisual}>
                          <Box className={styles.initialsVisualChild} />
                          <Typography
                            className={styles.h36}
                            variant="inherit"
                            variantMapping={{ inherit: "h3" }}
                            sx={{ fontWeight: "700" }}
                          >
                            ЮБ
                          </Typography>
                        </Box>
                        <Box className={styles.initialsVisual}>
                          <Box className={styles.initialsVisualItem} />
                          <Typography
                            className={styles.h37}
                            variant="inherit"
                            variantMapping={{ inherit: "h3" }}
                            sx={{ fontWeight: "700" }}
                          >
                            АВ
                          </Typography>
                        </Box>
                        <Box className={styles.creatorAvatar}>
                          <Box className={styles.creatorProfile}>
                            <Box className={styles.ownerCircle} />
                            <Typography
                              className={styles.noAvatar}
                              variant="inherit"
                              variantMapping={{ inherit: "h3" }}
                              sx={{ fontWeight: "300" }}
                            >
                              +2
                            </Typography>
                          </Box>
                        </Box>
                      </Box>
                    </Box>
                  </Box>
                  <Box className={styles.publicationCounts}>
                    <Box className={styles.contentCounters}>
                      <Typography
                        className={styles.h38}
                        variant="inherit"
                        variantMapping={{ inherit: "h3" }}
                        sx={{ fontWeight: "400" }}
                      >
                        Кількість публікацій: 2
                      </Typography>
                    </Box>
                    <button className={styles.groupButton}>
                      <Box className={styles.frameChild3} />
                      <Typography
                        className={styles.h39}
                        variantMapping={{ inherit: "h3" }}
                        sx={{
                          fontFamily: "var(--font-inter)",
                          fontSize: "var(--fs-24)",
                        }}
                      >
                        Подати заявку
                      </Typography>
                    </button>
                  </Box>
                </Box>
              </Box>
            </Box>
          </Box>
          <Box className={styles.groupDiv} onClick={() => navigate("/team/public")} sx={{ cursor: "pointer" }}>
            <Box className={styles.frameItem} />
            <Box className={styles.teamDivision}>
              <Box className={styles.teamDivisionChild} />
            </Box>
            <Box className={styles.futureVision}>
              <Box className={styles.descriptionDetails}>
                <Typography
                  className={styles.h1}
                  variant="inherit"
                  variantMapping={{ inherit: "h2" }}
                  sx={{ fontWeight: "700" }}
                >
                  Арт майбутнього
                </Typography>
                <Box className={styles.rectangleParent2}>
                  <Box className={styles.frameChild5} />
                  <Typography
                    className={styles.h310}
                    variant="inherit"
                    variantMapping={{ inherit: "h3" }}
                    sx={{ fontWeight: "400" }}
                  >
                    У пошуку
                  </Typography>
                </Box>
              </Box>
              <Box className={styles.skillDiversity}>
                <Typography
                  className={styles.uiux}
                  variant="inherit"
                  variantMapping={{ inherit: "h3" }}
                  sx={{ fontWeight: "400" }}
                >
                  Графічний дизайн · Illustrator · Арт
                </Typography>
                <Box className={styles.domainExpertise}>
                  <Box className={styles.rectangleParent3}>
                    <Box className={styles.frameChild6} />
                    <div className={styles.div}>Дизайн</div>
                  </Box>
                </Box>
              </Box>
            </Box>
            <Box className={styles.communityEngagement}>
              <Box className={styles.memberComposition}>
                <Box className={styles.listingDetails}>
                  <Box className={styles.teamMemberOverview}>
                    <Typography
                      className={styles.h34}
                      variant="inherit"
                      variantMapping={{ inherit: "h3" }}
                      sx={{ fontWeight: "400" }}
                    >
                      Учасники
                    </Typography>
                  </Box>
                  <Box className={styles.teammatePortraits}>
                    <Box className={styles.initialsVisual}>
                      <Box className={styles.avatarCircles} />
                      <Typography
                        className={styles.h35}
                        variant="inherit"
                        variantMapping={{ inherit: "h3" }}
                        sx={{ fontWeight: "700" }}
                      >
                        ДМ
                      </Typography>
                    </Box>
                    <Box className={styles.supportingRole}>
                      <Box className={styles.creatorProfile}>
                        <Box className={styles.defaultAvatar} />
                        <Typography
                          className={styles.placeholderPic}
                          variant="inherit"
                          variantMapping={{ inherit: "h3" }}
                          sx={{ fontWeight: "300" }}
                        >
                          +
                        </Typography>
                      </Box>
                    </Box>
                  </Box>
                  <Box className={styles.expertInvitation}>
                    <div className={styles.div3}>
                      Потрібен графічний дизайнер
                    </div>
                  </Box>
                </Box>
              </Box>
              <Box className={styles.publicationCounts}>
                <Box className={styles.counterElements}>
                  <Typography
                    className={styles.h38}
                    variant="inherit"
                    variantMapping={{ inherit: "h3" }}
                    sx={{ fontWeight: "400" }}
                  >
                    Кількість публікацій: 0
                  </Typography>
                </Box>
                <button className={styles.rectangleParent4}>
                  <Box className={styles.frameChild3} />
                  <div className={styles.div4}>Подати заявку</div>
                </button>
              </Box>
            </Box>
          </Box>
        </Box>
      </footer>
    </section>
  );
};

export default FrameComponent1111;
