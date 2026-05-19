import { FunctionComponent } from "react";
import { Box, Typography } from "@mui/material";
import styles from "./IF_FrameComponent1111111111.module.css";

export type FrameComponent1111111111Type = {
  className?: string;
};

const FrameComponent1111111111: FunctionComponent<
  FrameComponent1111111111Type
> = ({ className = "" }) => {
  return (
    <Box className={[styles.rectangleParent, className].join(" ")}>
      <Box className={styles.frameChild} />
      <Box className={styles.frameWrapper}>
        <Box className={styles.parent}>
          <Typography
            className={styles.h2}
            variant="inherit"
            variantMapping={{ inherit: "h2" }}
            sx={{ fontWeight: "700" }}
          >
            Арт майбутнього
          </Typography>
          <Typography
            className={styles.illustrator}
            variant="inherit"
            variantMapping={{ inherit: "h3" }}
            sx={{ fontWeight: "400", fontSize: "var(--fs-24)" }}
          >
            Графічний дизайн · Illustrator · Арт
          </Typography>
        </Box>
      </Box>
      <Box className={styles.frameParent}>
        <Box className={styles.frameContainer}>
          <Box className={styles.designHeaderParent}>
            <Box className={styles.designHeader}>
              <Box className={styles.rectangleGroup}>
                <Box className={styles.frameItem} />
                <div className={styles.div}>Дизайн</div>
              </Box>
            </Box>
            <Box className={styles.rectangleContainer}>
              <Box className={styles.frameInner} />
              <Typography
                className={styles.h3}
                variant="inherit"
                variantMapping={{ inherit: "h3" }}
                sx={{ fontWeight: "400" }}
              >
                У пошуку
              </Typography>
            </Box>
          </Box>
        </Box>
        <Typography
          className={styles.h32}
          variant="inherit"
          variantMapping={{ inherit: "h3" }}
          sx={{ fontWeight: "400", fontSize: "var(--fs-24)" }}
        >
          потрібен графічний дизайнер
        </Typography>
      </Box>
    </Box>
  );
};

export default FrameComponent1111111111;
