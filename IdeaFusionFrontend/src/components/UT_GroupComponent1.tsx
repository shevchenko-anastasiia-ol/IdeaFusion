import { FunctionComponent, useMemo, type CSSProperties } from "react";
import styles from "./UT_GroupComponent1.module.css";

export type GroupComponent1Type = {
  className?: string;
  prop?: string;

  /** Style props */
  groupDivFlex?: CSSProperties["flex"];
  groupDivWidth?: CSSProperties["width"];
  rectangleDivBackgroundColor?: CSSProperties["backgroundColor"];
  frameDivGap?: CSSProperties["gap"];
  frameDivGap1?: CSSProperties["gap"];
  bHeight?: CSSProperties["height"];
  bDisplay?: CSSProperties["display"];
};

const GroupComponent1: FunctionComponent<GroupComponent1Type> = ({
  className = "",
  groupDivFlex,
  groupDivWidth,
  rectangleDivBackgroundColor,
  frameDivGap,
  frameDivGap1,
  prop,
  bHeight,
  bDisplay,
}) => {
  const groupDiv1Style: CSSProperties = useMemo(() => {
    return {
      flex: groupDivFlex,
      width: groupDivWidth,
    };
  }, [groupDivFlex, groupDivWidth]);

  const rectangleDiv1Style: CSSProperties = useMemo(() => {
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

  const bStyle: CSSProperties = useMemo(() => {
    return {
      height: bHeight,
      display: bDisplay,
    };
  }, [bHeight, bDisplay]);

  return (
    <div
      className={[styles.rectangleParent, className].join(" ")}
      style={groupDiv1Style}
    >
      <div className={styles.frameChild} />
      <div className={styles.frameItem} style={rectangleDiv1Style} />
      <div className={styles.frameParent} style={frameDiv2Style}>
        <div className={styles.parent} style={frameDiv3Style}>
          <b className={styles.b} style={bStyle}>
            {prop}
          </b>
          <div className={styles.innerImageWrapper}>
            <div className={styles.innerImage}>
              <div className={styles.maskLayer} />
              <div className={styles.div}>Арт</div>
            </div>
          </div>
        </div>
        <div className={styles.div2}>Команда «Арт майбутнього»</div>
      </div>
    </div>
  );
};

export default GroupComponent1;
