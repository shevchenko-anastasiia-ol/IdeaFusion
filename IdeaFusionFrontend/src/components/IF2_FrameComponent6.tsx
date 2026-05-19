import { FunctionComponent, useMemo, type CSSProperties } from "react";
import styles from "./IF2_FrameComponent6.module.css";

export type FrameComponent6Type = {
  className?: string;
  prop?: string;
  soundCrafters?: string;

  /** Style props */
  frameDivFlex?: CSSProperties["flex"];
  frameDivPadding?: CSSProperties["padding"];
  frameDivJustifyContent?: CSSProperties["justifyContent"];
  frameDivBackgroundColor?: CSSProperties["backgroundColor"];
  rectangleDivBackgroundColor?: CSSProperties["backgroundColor"];
  frameDivGap?: CSSProperties["gap"];
  frameDivGap1?: CSSProperties["gap"];
  frameDivMarginLeft?: CSSProperties["marginLeft"];
};

const FrameComponent6: FunctionComponent<FrameComponent6Type> = ({
  className = "",
  frameDivFlex,
  frameDivPadding,
  frameDivJustifyContent,
  frameDivBackgroundColor,
  rectangleDivBackgroundColor,
  frameDivGap,
  frameDivGap1,
  prop,
  frameDivMarginLeft,
  soundCrafters,
}) => {
  const frameDivStyle: CSSProperties = useMemo(() => {
    return {
      flex: frameDivFlex,
      padding: frameDivPadding,
      justifyContent: frameDivJustifyContent,
    };
  }, [frameDivFlex, frameDivPadding, frameDivJustifyContent]);

  const frameDiv1Style: CSSProperties = useMemo(() => {
    return {
      backgroundColor: frameDivBackgroundColor,
    };
  }, [frameDivBackgroundColor]);

  const rectangleDiv2Style: CSSProperties = useMemo(() => {
    return {
      backgroundColor: rectangleDivBackgroundColor,
    };
  }, [rectangleDivBackgroundColor]);

  const frameDiv2Style: CSSProperties = useMemo(() => {
    return {
      gap: frameDivGap,
    };
  }, [frameDivGap]);

  const frameDiv3Style: CSSProperties = useMemo(() => {
    return {
      gap: frameDivGap1,
    };
  }, [frameDivGap1]);

  const frameDiv4Style: CSSProperties = useMemo(() => {
    return {
      marginLeft: frameDivMarginLeft,
    };
  }, [frameDivMarginLeft]);

  return (
    <div
      className={[styles.frameWrapper, className].join(" ")}
      style={frameDivStyle}
    >
      <div className={styles.rectangleParent}>
        <div className={styles.frameChild} />
        <div className={styles.rectangleGroup} style={frameDiv1Style}>
          <div className={styles.frameItem} style={rectangleDiv2Style} />
          <div className={styles.rectangleContainer}>
            <div className={styles.frameInner} />
            <div className={styles.div}>Колаборація</div>
          </div>
        </div>
        <div className={styles.frameContainer}>
          <div className={styles.frameParent} style={frameDiv2Style}>
            <div className={styles.parent} style={frameDiv3Style}>
              <b className={styles.b}>{prop}</b>
              <div className={styles.frameDiv} style={frameDiv4Style}>
                <div className={styles.groupDiv}>
                  <div className={styles.rectangleDiv} />
                  <div className={styles.div2}>Анімація</div>
                </div>
              </div>
            </div>
            <div className={styles.soundCrafters}>{soundCrafters}</div>
          </div>
        </div>
      </div>
    </div>
  );
};

export default FrameComponent6;
