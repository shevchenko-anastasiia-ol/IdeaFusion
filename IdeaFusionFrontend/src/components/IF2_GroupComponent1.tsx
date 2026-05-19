import { FunctionComponent, useMemo, type CSSProperties } from "react";
import styles from "./IF2_GroupComponent1.module.css";

export type GroupComponent1Type = {
  className?: string;
  prop?: string;

  /** Style props */
  rectangleDivBackgroundColor?: CSSProperties["backgroundColor"];
  frameDivGap?: CSSProperties["gap"];
  frameDivGap1?: CSSProperties["gap"];
};

const GroupComponent1: FunctionComponent<GroupComponent1Type> = ({
  className = "",
  rectangleDivBackgroundColor,
  frameDivGap,
  frameDivGap1,
  prop,
}) => {
  const rectangleDiv6Style: CSSProperties = useMemo(() => {
    return {
      backgroundColor: rectangleDivBackgroundColor,
    };
  }, [rectangleDivBackgroundColor]);

  const frameDiv11Style: CSSProperties = useMemo(() => {
    return {
      gap: frameDivGap,
    };
  }, [frameDivGap]);

  const frameDiv12Style: CSSProperties = useMemo(() => {
    return {
      gap: frameDivGap1,
    };
  }, [frameDivGap1]);

  return (
    <div className={[styles.rectangleParent, className].join(" ")}>
      <div className={styles.frameChild} />
      <div className={styles.frameItem} style={rectangleDiv6Style} />
      <div className={styles.frameParent} style={frameDiv11Style}>
        <div className={styles.parent} style={frameDiv12Style}>
          <b className={styles.b}>{prop}</b>
          <div className={styles.frameWrapper}>
            <div className={styles.artBackgroundParent}>
              <div className={styles.artBackground} />
              <div className={styles.div}>Арт</div>
            </div>
          </div>
        </div>
        <div className={styles.div2}>Анастасія Шевченко</div>
      </div>
    </div>
  );
};

export default GroupComponent1;
