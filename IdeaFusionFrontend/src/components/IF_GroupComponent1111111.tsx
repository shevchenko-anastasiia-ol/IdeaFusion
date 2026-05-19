import { FunctionComponent } from "react";
import { Box, Typography } from "@mui/material";
import styles from "./IF_GroupComponent1111111.module.css";

export type GroupComponent1111111Type = {
  className?: string;
};

const GroupComponent1111111: FunctionComponent<GroupComponent1111111Type> = ({
  className = "",
}) => {
  return (
    <section className={[styles.rectangleParent, className].join(" ")}>
      <Box className={styles.frameChild} />
      <Box className={styles.frameWrapper}>
        <Box className={styles.parent}>
          <div className={styles.div}>З питань модерації звертайтесь сюди:</div>
          <Box className={styles.moderatorsideafusioncom380Wrapper}>
            <div className={styles.instagramAnasteishen}>
              moderators@ideafusion.com
              <br />
              +380 66 757 4343
            </div>
          </Box>
        </Box>
      </Box>
      <Box className={styles.lineWrapper}>
        <Box className={styles.frameItem} />
      </Box>
      <Box className={styles.frameParent}>
        <Box className={styles.group}>
          <div className={styles.div}>Власник платформи</div>
          <Box className={styles.ownerDetails}>
            <div className={styles.instagramAnasteishen}>
              {`Шевченко Анастасія Олександрівна`}
              <br />
              {`Instagram: _.anasteishen_ `}
            </div>
          </Box>
        </Box>
        <Box className={styles.frameInner} />
      </Box>
      <Box className={styles.frameContainer}>
        <Box className={styles.container}>
          <div className={styles.div}>Служба підтримки</div>
          <Box className={styles.supportideafusioncom38066Wrapper}>
            <div className={styles.instagramAnasteishen}>
              {`support@ideafusion.com`}
              <br />
              {`+380 66 444 3322 `}
            </div>
          </Box>
        </Box>
      </Box>
      <Box className={styles.lineContainer}>
        <Box className={styles.frameItem} />
      </Box>
      <Box className={styles.platformInfo}>
        <Box className={styles.frameGroup}>
          <img
            className={styles.groupIcon}
            loading="lazy"
            alt=""
            src="/Group-13@2x.png"
          />
          <Box className={styles.logoDetailsWrapper}>
            <Box className={styles.logoDetails}>
              <Typography
                className={styles.ideafusion}
                variant="inherit"
                variantMapping={{ inherit: "h3" }}
                sx={{ fontWeight: "700" }}
              >
                <Typography
                  variant="inherit"
                  variantMapping={{ inherit: "span" }}
                >
                  Idea
                </Typography>
                <Typography
                  className={styles.fusion}
                  variant="inherit"
                  variantMapping={{ inherit: "span" }}
                >
                  Fusion
                </Typography>
              </Typography>
              <img className={styles.vectorIcon} alt="" src="/Vector.svg" />
              <div className={styles.creativeCollaborationPlatfor}>
                CREATIVE COLLABORATION PLATFORM
              </div>
            </Box>
          </Box>
        </Box>
      </Box>
    </section>
  );
};

export default GroupComponent1111111;
