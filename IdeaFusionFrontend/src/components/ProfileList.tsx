import { FunctionComponent } from "react";
import { Box, Typography } from "@mui/material";
import styles from "./ProfileList.module.css";

export type ProfileListType = {
  className?: string;
};

const ProfileList: FunctionComponent<ProfileListType> = ({
  className = "",
}) => {
  return (
    <Box className={[styles.profileList, className].join(" ")}>
      <Box className={styles.profileListInner}>
        <Box className={styles.rectangleParent}>
          <Box className={styles.frameChild} />
          <Box className={styles.profilePictureParent}>
            <Box className={styles.profilePicture} />
            <Typography
              className={styles.h3}
              variant="inherit"
              variantMapping={{ inherit: "h3" }}
              sx={{ fontWeight: "700" }}
            >
              КГ
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
                  Катерина Гончар
                </Typography>
              </Box>
              <div className={styles.uiux}>UI/UX дизайнер</div>
            </Box>
          </Box>
          <Box className={styles.wrapper}>
            <div className={styles.div}>3 проєкти</div>
          </Box>
        </Box>
      </Box>
      <Box className={styles.profileListChild}>
        <Box className={styles.rectangleGroup}>
          <Box className={styles.frameChild} />
          <Box className={styles.frameParent}>
            <Box className={styles.ellipseParent}>
              <Box className={styles.frameInner} />
              <Typography
                className={styles.h3}
                variant="inherit"
                variantMapping={{ inherit: "h3" }}
                sx={{ fontWeight: "700" }}
              >
                КГ
              </Typography>
            </Box>
            <Box className={styles.frameWrapper}>
              <Box className={styles.captionPanel}>
                <Box className={styles.fullNamePlate}>
                  <Typography
                    className={styles.h32}
                    variant="inherit"
                    variantMapping={{ inherit: "h3" }}
                    sx={{ fontWeight: "600" }}
                  >
                    Андрій Власенко
                  </Typography>
                </Box>
                <div className={styles.uiux}>3D артист</div>
              </Box>
            </Box>
          </Box>
          <Box className={styles.wrapper}>
            <div className={styles.div}>23 проєкти</div>
          </Box>
        </Box>
      </Box>
      <Box className={styles.rectangleContainer}>
        <Box className={styles.frameChild} />
        <Box className={styles.avatarContainerParent}>
          <Box className={styles.ellipseParent}>
            <Box className={styles.avatarContainerChild} />
            <Typography
              className={styles.h3}
              variant="inherit"
              variantMapping={{ inherit: "h3" }}
              sx={{ fontWeight: "700" }}
            >
              КГ
            </Typography>
          </Box>
          <Box className={styles.frameWrapper}>
            <Box className={styles.captionPanel}>
              <Box className={styles.fullNamePlate}>
                <Typography
                  className={styles.h32}
                  variant="inherit"
                  variantMapping={{ inherit: "h3" }}
                  sx={{ fontWeight: "600" }}
                >
                  Юлія Бондар
                </Typography>
              </Box>
              <div className={styles.uiux}>Композитор</div>
            </Box>
          </Box>
        </Box>
        <Box className={styles.wrapper}>
          <div className={styles.div}>19 проєкти</div>
        </Box>
      </Box>
    </Box>
  );
};

export default ProfileList;
