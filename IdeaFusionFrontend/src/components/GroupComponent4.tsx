import { FunctionComponent } from "react";
import { Box, Typography } from "@mui/material";
import ParticipantsList from "./ParticipantsList";
import styles from "./GroupComponent4.module.css";

export type GroupComponent4Type = {
  className?: string;
};

const GroupComponent4: FunctionComponent<GroupComponent4Type> = ({
  className = "",
}) => {
  return (
    <section className={[styles.rectangleParent, className].join(" ")}>
      <Box className={styles.frameChild} />
      <Box className={styles.completedDivider} />
      <Box className={styles.completedProjectDetails}>
        <Box className={styles.completedProjectInfo}>
          <Box className={styles.timeTravelersLabel}>
            <Typography
              className={styles.h2}
              variant="inherit"
              variantMapping={{ inherit: "h2" }}
              sx={{ fontWeight: "700" }}
            >
              Мандрівники часом
            </Typography>
            <Box className={styles.statusBlock}>
              <Box className={styles.rectangleGroup}>
                <Box className={styles.frameItem} />
                <Typography
                  className={styles.h3}
                  variant="inherit"
                  variantMapping={{ inherit: "h3" }}
                  sx={{ fontWeight: "400" }}
                >
                  Завершена
                </Typography>
              </Box>
            </Box>
          </Box>
          <Box className={styles.skillsDescription}>
            <Typography
              className={styles.ost}
              variant="inherit"
              variantMapping={{ inherit: "h3" }}
              sx={{ fontWeight: "400" }}
            >
              Музика · Звукорежисура · OST
            </Typography>
            <Box className={styles.artBlock}>
              <Box className={styles.rectangleContainer}>
                <Box className={styles.frameInner} />
                <div className={styles.div}>Арт</div>
              </Box>
            </Box>
          </Box>
          <ParticipantsList
            participantsListFlex="unset"
            participantsListWidth="396px"
          />
        </Box>
      </Box>
      <Box className={styles.publicationNumbers}>
        <Typography
          className={styles.h32}
          variant="inherit"
          variantMapping={{ inherit: "h3" }}
          sx={{ fontWeight: "400" }}
        >
          Кількість публікацій: 8
        </Typography>
      </Box>
    </section>
  );
};

export default GroupComponent4;
