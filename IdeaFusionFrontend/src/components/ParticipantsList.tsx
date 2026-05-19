import { FunctionComponent, useMemo, type CSSProperties } from "react";
import { Typography, Box } from "@mui/material";
import styles from "./ParticipantsList.module.css";

export type ParticipantsListType = {
  className?: string;

  /** Style props */
  participantsListFlex?: CSSProperties["flex"];
  participantsListWidth?: CSSProperties["width"];
};

const ParticipantsList: FunctionComponent<ParticipantsListType> = ({
  className = "",
  participantsListFlex,
  participantsListWidth,
}) => {
  const participantsListStyle: CSSProperties = useMemo(() => {
    return {
      flex: participantsListFlex,
      width: participantsListWidth,
    };
  }, [participantsListFlex, participantsListWidth]);

  return (
    <Box
      className={[styles.participantsList, className].join(" ")}
      style={participantsListStyle}
    >
      <Box className={styles.wrapper}>
        <Typography
          className={styles.h3}
          variant="inherit"
          variantMapping={{ inherit: "h3" }}
          sx={{ fontWeight: "400" }}
        >
          Учасники
        </Typography>
      </Box>
      <Box className={styles.teamMemberIcons}>
        <Box className={styles.teamAvatars}>
          <Box className={styles.avatarIcons} />
          <Typography
            className={styles.h32}
            variant="inherit"
            variantMapping={{ inherit: "h3" }}
            sx={{ fontWeight: "700" }}
          >
            ДМ
          </Typography>
        </Box>
        <Box className={styles.teamAvatars}>
          <Box className={styles.teamAvatarsChild} />
          <Typography
            className={styles.h33}
            variant="inherit"
            variantMapping={{ inherit: "h3" }}
            sx={{ fontWeight: "700" }}
          >
            ЮБ
          </Typography>
        </Box>
        <Box className={styles.teamAvatars}>
          <Box className={styles.teamAvatarsItem} />
          <Typography
            className={styles.h34}
            variant="inherit"
            variantMapping={{ inherit: "h3" }}
            sx={{ fontWeight: "700" }}
          >
            АВ
          </Typography>
        </Box>
        <Box className={styles.missingAvatarWrapper}>
          <Box className={styles.missingAvatar}>
            <Box className={styles.missingAvatarChild} />
            <Typography
              className={styles.h35}
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
  );
};

export default ParticipantsList;
