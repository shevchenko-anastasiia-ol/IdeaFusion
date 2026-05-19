import { FunctionComponent, useMemo, type CSSProperties } from "react";
import styles from "./IF2_GroupComponent4.module.css";

export type GroupComponent4Type = {
  className?: string;
  prop?: string;

  /** Style props */
  rectangleDivBackgroundColor?: CSSProperties["backgroundColor"];
  frameDivGap?: CSSProperties["gap"];
  frameDivGap1?: CSSProperties["gap"];
};

const GroupComponent4: FunctionComponent<GroupComponent4Type> = ({
  className = "",
  rectangleDivBackgroundColor,
  frameDivGap,
  frameDivGap1,
  prop,
}) => {
  const rectangleDiv3Style: CSSProperties = useMemo(() => {
    return {
      backgroundColor: rectangleDivBackgroundColor,
    };
  }, [rectangleDivBackgroundColor]);

  const frameDiv5Style: CSSProperties = useMemo(() => {
    return {
      gap: frameDivGap,
    };
  }, [frameDivGap]);

  const frameDiv6Style: CSSProperties = useMemo(() => {
    return {
      gap: frameDivGap1,
    };
  }, [frameDivGap1]);

  return (
    <div className={[styles.rectangleParent, className].join(" ")}>
      <div className={styles.frameChild} />
      <div className={styles.frameItem} style={rectangleDiv3Style} />
      <div className={styles.frameParent} style={frameDiv5Style}>
        <div className={styles.parent} style={frameDiv6Style}>
          <b className={styles.b}>{prop}</b>
          <div className={styles.frameWrapper}>
            <div className={styles.rectangleGroup}>
              <div className={styles.frameInner} />
              <div className={styles.div}>Арт</div>
            </div>
          </div>
        </div>
        <div className={styles.div2}>Анастасія Шевченко</div>
      </div>
    </div>
  );
};

export default GroupComponent4;
