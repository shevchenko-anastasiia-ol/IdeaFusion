import { FunctionComponent } from "react";
import { Box, Button, Typography } from "@mui/material";
import styles from "./FrameComponent11.module.css";

export type FrameComponent11Type = {
  className?: string;
};

const FrameComponent11: FunctionComponent<FrameComponent11Type> = ({
  className = "",
}) => {
  return (
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
      <Box className={styles.frameContainer}>
        <Box className={styles.frameParent}>
          <Box className={styles.rectangleGroup}>
            <Box className={styles.frameInner} />
            <img className={styles.groupIcon} alt="" src="/Group-2.svg" />
          </Box>
          <Box className={styles.frameDiv}>
            <Box className={styles.frameGroup}>
              <Box className={styles.frameParent2}>
                <button className={styles.frameButton}>
                  <img
                    className={styles.frameChild2}
                    loading="lazy"
                    alt=""
                    src="/Group-3.svg"
                  />
                </button>
                <button className={styles.frameWrapper2}>
                  <img
                    className={styles.frameChild3}
                    loading="lazy"
                    alt=""
                    src="/Group-4.svg"
                  />
                </button>
                <Button
                  className={styles.starRating}
                  disableElevation
                  variant="outlined"
                  sx={{
                    borderColor: "#6d6c6c",
                    borderRadius: "2px",
                    "&:hover": { borderColor: "#6d6c6c" },
                    width: 46,
                    height: 45,
                  }}
                />
              </Box>
              <button className={styles.frameWrapper2}>
                <Box className={styles.rectangleContainer}>
                  <Box className={styles.rectangleDiv} />
                  <Box className={styles.frameChild4} />
                  <img
                    className={styles.rectangleIcon}
                    loading="lazy"
                    alt=""
                    src="/Rectangle-8.svg"
                  />
                  <Box className={styles.frameChild5} />
                </Box>
              </button>
            </Box>
          </Box>
        </Box>
      </Box>
      <Box className={styles.ellipseParent}>
        <Box className={styles.ellipseDiv} />
        <Typography
          className={styles.h3}
          variant="inherit"
          variantMapping={{ inherit: "h3" }}
          sx={{ fontWeight: "700" }}
        >
          АШ
        </Typography>
      </Box>
    </section>
  );
};

export default FrameComponent11;
