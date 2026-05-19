import { FunctionComponent, useMemo, type CSSProperties } from "react";
import { Box, Typography } from "@mui/material";
import styles from "./IF_FrameComponent111111111.module.css";

export type FrameComponent111111111Type = {
  className?: string;
  prop?: string;
  prop1?: string;

  /** Style props */
  frameSectionLeft?: CSSProperties["left"];
  frameSectionAlignItems?: CSSProperties["alignItems"];
  lineBoxBorderTop?: CSSProperties["borderTop"];
  frameBoxJustifyContent?: CSSProperties["justifyContent"];
  frameBoxPadding?: CSSProperties["padding"];
  activitySegmentsPadding?: CSSProperties["padding"];
};

const FrameComponent111111111: FunctionComponent<
  FrameComponent111111111Type
> = ({
  className = "",
  frameSectionLeft,
  frameSectionAlignItems,
  lineBoxBorderTop,
  frameBoxJustifyContent,
  frameBoxPadding,
  activitySegmentsPadding,
  prop,
  prop1,
}) => {
  const frameSectionStyle: CSSProperties = useMemo(() => {
    return {
      left: frameSectionLeft,
      alignItems: frameSectionAlignItems,
    };
  }, [frameSectionLeft, frameSectionAlignItems]);

  const lineBoxStyle: CSSProperties = useMemo(() => {
    return {
      borderTop: lineBoxBorderTop,
    };
  }, [lineBoxBorderTop]);

  const frameBoxStyle: CSSProperties = useMemo(() => {
    return {
      justifyContent: frameBoxJustifyContent,
      padding: frameBoxPadding,
    };
  }, [frameBoxJustifyContent, frameBoxPadding]);

  const activitySegmentsStyle: CSSProperties = useMemo(() => {
    return {
      padding: activitySegmentsPadding,
    };
  }, [activitySegmentsPadding]);

  return (
    <section
      className={[styles.rectangleParent, className].join(" ")}
      style={frameSectionStyle}
    >
      <Box className={styles.frameChild} />
      <Box className={styles.frameItem} style={lineBoxStyle} />
      <Box className={styles.activityLevelsWrapper} style={frameBoxStyle}>
        <Box className={styles.activityLevels}>
          <Box
            className={styles.activitySegments}
            style={activitySegmentsStyle}
          >
            <Typography
              className={styles.h1}
              variant="inherit"
              variantMapping={{ inherit: "h1" }}
              sx={{ fontWeight: "700" }}
            >
              {prop}
            </Typography>
          </Box>
          <Typography
            className={styles.h2}
            variant="inherit"
            variantMapping={{ inherit: "h2" }}
            sx={{ fontWeight: "400", fontSize: "var(--fs-36)" }}
          >
            {prop1}
          </Typography>
        </Box>
      </Box>
    </section>
  );
};

export default FrameComponent111111111;
