import { FunctionComponent, useMemo, type CSSProperties } from "react";
import styles from "./UT_GroupComponent.module.css";

export type GroupComponentType = {
  className?: string;
  prop?: string;
  prop1?: string;

  /** Style props */
  overlapLayerBackgroundColor?: CSSProperties["backgroundColor"];
  groupDivBackgroundColor?: CSSProperties["backgroundColor"];
  groupDivBorder?: CSSProperties["border"];
  rectangleDivBackgroundColor?: CSSProperties["backgroundColor"];
  rectangleDivBorder?: CSSProperties["border"];
  divColor?: CSSProperties["color"];
};

const GroupComponent: FunctionComponent<GroupComponentType> = ({
  className = "",
  overlapLayerBackgroundColor,
  prop,
  groupDivBackgroundColor,
  groupDivBorder,
  rectangleDivBackgroundColor,
  rectangleDivBorder,
  prop1,
  divColor,
}) => {
  const overlapLayerStyle: CSSProperties = useMemo(() => {
    return {
      backgroundColor: overlapLayerBackgroundColor,
    };
  }, [overlapLayerBackgroundColor]);

  const groupDivStyle: CSSProperties = useMemo(() => {
    return {
      backgroundColor: groupDivBackgroundColor,
      border: groupDivBorder,
    };
  }, [groupDivBackgroundColor, groupDivBorder]);

  const rectangleDivStyle: CSSProperties = useMemo(() => {
    return {
      backgroundColor: rectangleDivBackgroundColor,
      border: rectangleDivBorder,
    };
  }, [rectangleDivBackgroundColor, rectangleDivBorder]);

  const divStyle: CSSProperties = useMemo(() => {
    return {
      color: divColor,
    };
  }, [divColor]);

  return (
    <div className={[styles.rectangleParent, className].join(" ")}>
      <div className={styles.frameChild} />
      <div className={styles.overlapLayer} style={overlapLayerStyle} />
      <div className={styles.frameWrapper}>
        <div className={styles.innerLayerParent}>
          <div className={styles.innerLayer}>
            <b className={styles.b}>{prop}</b>
            <div className={styles.styleBanner}>
              <div className={styles.rectangleGroup} style={groupDivStyle}>
                <div className={styles.frameItem} style={rectangleDivStyle} />
                <div className={styles.div} style={divStyle}>
                  {prop1}
                </div>
              </div>
            </div>
          </div>
          <div className={styles.div2}>Команда «Арт майбутнього»</div>
        </div>
      </div>
    </div>
  );
};

export default GroupComponent;
