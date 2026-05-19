import { FunctionComponent, useMemo, type CSSProperties } from "react";
import { Box, Typography } from "@mui/material";
import { useNavigate } from "react-router-dom";
import styles from "./FrameComponent.module.css";

export type FrameComponentType = {
  className?: string;
  prop?: string;
  prop1?: string;
  react?: string;
  avatarUrl?: string;
  userId?: string;

  /** Style props */
  ellipseBoxBackgroundColor?: CSSProperties["backgroundColor"];
  typographyLeft?: CSSProperties["left"];
  typographyMinWidth?: CSSProperties["minWidth"];
};

const FrameComponent: FunctionComponent<FrameComponentType> = ({
  className = "",
  ellipseBoxBackgroundColor,
  prop,
  typographyLeft,
  typographyMinWidth,
  prop1,
  react,
  avatarUrl,
  userId,
}) => {
  const navigate = useNavigate();

  const ellipseBoxStyle: CSSProperties = useMemo(() => {
    return {
      backgroundColor: ellipseBoxBackgroundColor,
    };
  }, [ellipseBoxBackgroundColor]);

  const typographyStyle: CSSProperties = useMemo(() => {
    return {
      left: typographyLeft,
      minWidth: typographyMinWidth,
    };
  }, [typographyLeft, typographyMinWidth]);

  return (
    <Box className={[styles.frameParent, className].join(" ")}
      onClick={() => userId ? navigate(`/users/${userId}`) : navigate("/users")}
      sx={{ cursor: "pointer" }}
    >
      <Box className={styles.ellipseParent}>
        {avatarUrl ? (
          <img
            src={avatarUrl}
            alt={prop1}
            style={{ width: "100%", height: "100%", borderRadius: "50%", objectFit: "cover", position: "absolute", top: 0, left: 0 }}
          />
        ) : (
          <>
            <Box className={styles.frameChild} style={ellipseBoxStyle} />
            <Typography
              className={styles.h3}
              variant="inherit"
              variantMapping={{ inherit: "h3" }}
              sx={{ fontWeight: "700" }}
              style={typographyStyle}
            >
              {prop}
            </Typography>
          </>
        )}
      </Box>
      <Box className={styles.memberDetails}>
        <Box className={styles.memberNamesParent}>
          <Box className={styles.memberNames}>
            <Typography
              className={styles.h32}
              variant="inherit"
              variantMapping={{ inherit: "h3" }}
              sx={{ fontWeight: "600" }}
            >
              {prop1}
            </Typography>
          </Box>
          <div className={styles.react}>{react}</div>
        </Box>
      </Box>
    </Box>
  );
};

export default FrameComponent;
