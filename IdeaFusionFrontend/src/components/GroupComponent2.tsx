import { FunctionComponent } from "react";
import { Box, Typography } from "@mui/material";
import ParticipantsList from "./ParticipantsList";
import styles from "./GroupComponent2.module.css";

export type GroupComponent2Type = {
  className?: string;
};

const GroupComponent2: FunctionComponent<GroupComponent2Type> = ({
  className = "",
}) => {
  return (
    <section className={[styles.rectangleParent, className].join(" ")}>
      <Box className={styles.frameChild} />
      <Box className={styles.dividerOneWrapper}>
        <Box className={styles.dividerOne} />
      </Box>
      <Box className={styles.teamContainerWrapper}>
        <Box className={styles.teamContainer}>
          <Box className={styles.parent}>
            <Typography
              className={styles.h1}
              variant="inherit"
              variantMapping={{ inherit: "h1" }}
              sx={{ fontWeight: "700" }}
            >
              Бюрократи
            </Typography>
            <Box className={styles.frameWrapper}>
              <Box className={styles.rectangleGroup}>
                <Box className={styles.frameItem} />
                <Typography
                  className={styles.h3}
                  variant="inherit"
                  variantMapping={{ inherit: "h3" }}
                  sx={{ fontWeight: "400" }}
                >
                  Активна
                </Typography>
              </Box>
            </Box>
          </Box>
          <Box className={styles.uiuxFigmaParent}>
            <Typography
              className={styles.uiux}
              variant="inherit"
              variantMapping={{ inherit: "h3" }}
              sx={{ fontWeight: "400" }}
            >
              UI/UX дизайн · Figma · Анімації
            </Typography>
            <Box className={styles.frameContainer}>
              <Box className={styles.rectangleContainer}>
                <Box className={styles.frameInner} />
                <div className={styles.div}>Музика</div>
              </Box>
            </Box>
          </Box>
        </Box>
      </Box>
      <Box className={styles.participantsListWrapper}>
        <ParticipantsList />
      </Box>
      <Box className={styles.publicationCount}>
        <Typography
          className={styles.h32}
          variant="inherit"
          variantMapping={{ inherit: "h3" }}
          sx={{ fontWeight: "400" }}
        >
          Кількість публікацій: 2
        </Typography>
      </Box>
    </section>
  );
};

export default GroupComponent2;
