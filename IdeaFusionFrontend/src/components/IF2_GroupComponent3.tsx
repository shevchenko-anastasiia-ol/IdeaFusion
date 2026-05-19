import { FunctionComponent, useMemo, type CSSProperties } from "react";
import styles from "./IF2_GroupComponent3.module.css";

export type GroupComponent3Type = {
  className?: string;
  prop?: string;
  prop1?: string;

  /** Style props */
  rectangleDivBackgroundColor?: CSSProperties["backgroundColor"];
  groupDivBackgroundColor?: CSSProperties["backgroundColor"];
  groupDivBorder?: CSSProperties["border"];
  rectangleDivBackgroundColor1?: CSSProperties["backgroundColor"];
  rectangleDivBorder?: CSSProperties["border"];
  divColor?: CSSProperties["color"];
};

const GroupComponent3: FunctionComponent<GroupComponent3Type> = ({
  className = "",
  rectangleDivBackgroundColor,
  prop,
  groupDivBackgroundColor,
  groupDivBorder,
  rectangleDivBackgroundColor1,
  rectangleDivBorder,
  prop1,
  divColor,
}) => {
  const rectangleDivStyle: CSSProperties = useMemo(() => {
    return {
      backgroundColor: rectangleDivBackgroundColor,
    };
  }, [rectangleDivBackgroundColor]);

  const groupDivStyle: CSSProperties = useMemo(() => {
    return {
      backgroundColor: groupDivBackgroundColor,
      border: groupDivBorder,
    };
  }, [groupDivBackgroundColor, groupDivBorder]);

  const rectangleDiv1Style: CSSProperties = useMemo(() => {
    return {
      backgroundColor: rectangleDivBackgroundColor1,
      border: rectangleDivBorder,
    };
  }, [rectangleDivBackgroundColor1, rectangleDivBorder]);

  const divStyle: CSSProperties = useMemo(() => {
    return {
      color: divColor,
    };
  }, [divColor]);

  return (
    <div className={[styles.rectangleParent, className].join(" ")}>
      <div className={styles.frameChild} />
      <div className={styles.frameItem} style={rectangleDivStyle} />
      <div className={styles.frameWrapper}>
        <div className={styles.innerWorldParent}>
          <div className={styles.innerWorld}>
            <b className={styles.b}>{prop}</b>
            <div className={styles.innerWorldInner}>
              <div className={styles.rectangleGroup} style={groupDivStyle}>
                <div className={styles.frameInner} style={rectangleDiv1Style} />
                <div className={styles.div} style={divStyle}>
                  {prop1}
                </div>
              </div>
            </div>
          </div>
          <div className={styles.div2}>Анастасія Шевченко</div>
        </div>
      </div>
    </div>
  );
};

export default GroupComponent3;
