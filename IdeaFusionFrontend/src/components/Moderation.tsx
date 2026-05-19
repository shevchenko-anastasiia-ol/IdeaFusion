import { FunctionComponent, useMemo, type CSSProperties } from "react";
import { Box, Typography } from "@mui/material";
import styles from "./Moderation.module.css";

export type ModerationType = {
  className?: string;

  /** Style props */
  moderationMarginTop?: CSSProperties["marginTop"];
  moderationPosition?: CSSProperties["position"];
  moderationTop?: CSSProperties["top"];
  moderationLeft?: CSSProperties["left"];
};

const Moderation: FunctionComponent<ModerationType> = ({
  className = "",
  moderationMarginTop,
  moderationPosition,
  moderationTop,
  moderationLeft,
}) => {
  const moderationStyle: CSSProperties = useMemo(() => {
    return {
      marginTop: moderationMarginTop,
      position: moderationPosition,
      top: moderationTop,
      left: moderationLeft,
    };
  }, [moderationMarginTop, moderationPosition, moderationTop, moderationLeft]);

  return (
    <section
      className={[styles.moderation, className].join(" ")}
      style={moderationStyle}
    >
      <Box className={styles.moderationChild} />
      <Box className={styles.moderatorDetails}>
        <Box className={styles.moderatorContactParent}>
          <Box className={styles.moderatorContact}>
            <Box className={styles.moderatorInfo}>
              <div className={styles.div}>
                З питань модерації звертайтесь сюди:
              </div>
              <Box className={styles.moderatorEmail}>
                <div className={styles.instagramAnasteishen}>
                  moderators@ideafusion.com
                  <br />
                  +380 66 757 4343
                </div>
              </Box>
            </Box>
            <Box className={styles.separator} />
            <Box className={styles.platformOwner}>
              <div className={styles.div2}>Власник платформи</div>
              <Box className={styles.ownerDetails}>
                <div className={styles.instagramAnasteishen}>
                  {`Шевченко Анастасія Олександрівна`}
                  <br />
                  {`Instagram: _.anasteishen_ `}
                </div>
              </Box>
            </Box>
          </Box>
          <Box className={styles.separator} />
        </Box>
      </Box>
      <Box className={styles.support}>
        <Box className={styles.supportContactWrapper}>
          <Box className={styles.supportContact}>
            <div className={styles.div2}>Служба підтримки</div>
            <Box className={styles.supportDetails}>
              <div className={styles.instagramAnasteishen}>
                {`support@ideafusion.com`}
                <br />
                {`+380 66 444 3322 `}
              </div>
            </Box>
          </Box>
        </Box>
        <Box className={styles.separatorThree} />
      </Box>
      <Box className={styles.footer}>
        <Box className={styles.footerContent}>
          <img className={styles.footerContentChild} loading="lazy" alt="" />
          <Box className={styles.fusionLogo}>
            <Box className={styles.fusionBranding}>
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
              <img className={styles.fusionIcon} alt="" src="/Logo-Icon.svg" />
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

export default Moderation;
