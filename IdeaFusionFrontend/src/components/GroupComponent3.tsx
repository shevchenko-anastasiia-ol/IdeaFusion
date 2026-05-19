import { FunctionComponent } from "react";
import { Box, Typography } from "@mui/material";
import styles from "./GroupComponent3.module.css";

export type GroupComponent3Type = {
  className?: string;
};

const GroupComponent3: FunctionComponent<GroupComponent3Type> = ({
  className = "",
}) => {
  return (
    <section className={[styles.rectangleParent, className].join(" ")}>
      <Box className={styles.frameChild} />
      <Box className={styles.searchDivider} />
      <Box className={styles.searchContent}>
        <Box className={styles.searchItem}>
          <Box className={styles.searchInfo}>
            <Box className={styles.teamNameAndTags}>
              <Typography className={styles.h2} variant="inherit" variantMapping={{ inherit: "h2" }} sx={{ fontWeight: "700" }}>
                Арт майбутнього
              </Typography>
              <Box className={styles.lookingDesign}>
                <Box className={styles.rectangleGroup}>
                  <Box className={styles.frameItem} />
                  <Typography className={styles.h3} variant="inherit" variantMapping={{ inherit: "h3" }} sx={{ fontWeight: "400" }}>
                    У пошуку
                  </Typography>
                </Box>
              </Box>
            </Box>
            <Box className={styles.teamNameAndTags2}>
              <Typography className={styles.illustrator} variant="inherit" variantMapping={{ inherit: "h3" }} sx={{ fontWeight: "400" }}>
                Графічний дизайн · Illustrator · Арт
              </Typography>
              <Box className={styles.teamNameAndTagsInner}>
                <Box className={styles.rectangleContainer}>
                  <Box className={styles.frameInner} />
                  <div className={styles.div}>Дизайн</div>
                </Box>
              </Box>
            </Box>
          </Box>
          <Box className={styles.participantsBlock}>
            <Box className={styles.wrapper}>
              <Typography className={styles.h32} variant="inherit" variantMapping={{ inherit: "h3" }} sx={{ fontWeight: "400" }}>
                Учасники
              </Typography>
            </Box>
            <Box className={styles.participantsAvatars}>
              <Box className={styles.firstParticipant}>
                <Box className={styles.dmAvatar} />
                <Typography className={styles.h33} variant="inherit" variantMapping={{ inherit: "h3" }} sx={{ fontWeight: "700" }}>
                  ДМ
                </Typography>
              </Box>
              <Box className={styles.secondParticipant}>
                <Box className={styles.unknownAvatar}>
                  <Box className={styles.unknownAvatarChild} />
                  <Typography className={styles.unknownInitials} variant="inherit" variantMapping={{ inherit: "h3" }} sx={{ fontWeight: "300" }}>
                    +
                  </Typography>
                </Box>
              </Box>
            </Box>
            <Box className={styles.lookingForDesigner}>
              <div className={styles.div2}>Потрібен графічний дизайнер</div>
            </Box>
          </Box>
        </Box>
      </Box>
      <Box className={styles.countPublicationsBlock}>
        <Typography className={styles.h34} variant="inherit" variantMapping={{ inherit: "h3" }} sx={{ fontWeight: "400" }}>
          Кількість публікацій: 0
        </Typography>
      </Box>
    </section>
  );
};

export default GroupComponent3;