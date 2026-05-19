import { FunctionComponent, useMemo, type CSSProperties } from "react";
import styles from "./IF2_GroupComponent.module.css";

export type GroupComponentType = {
  className?: string;
  prop?: string;
  prop1?: string;

  /** Style props */
  rectangleDivBackgroundColor?: CSSProperties["backgroundColor"];
  frameDivMarginLeft?: CSSProperties["marginLeft"];
  groupDivBackgroundColor?: CSSProperties["backgroundColor"];
  groupDivBorder?: CSSProperties["border"];
  groupDivPadding?: CSSProperties["padding"];
  rectangleDivWidth?: CSSProperties["width"];
  rectangleDivBackgroundColor1?: CSSProperties["backgroundColor"];
  rectangleDivBorder?: CSSProperties["border"];
  divColor?: CSSProperties["color"];
};

const GroupComponent: FunctionComponent<GroupComponentType> = ({
  className = "",
  rectangleDivBackgroundColor,
  prop,
  frameDivMarginLeft,
  groupDivBackgroundColor,
  groupDivBorder,
  groupDivPadding,
  rectangleDivWidth,
  rectangleDivBackgroundColor1,
  rectangleDivBorder,
  prop1,
  divColor,
}) => {
  const rectangleDiv4Style: CSSProperties = useMemo(() => {
    return {
      backgroundColor: rectangleDivBackgroundColor,
    };
  }, [rectangleDivBackgroundColor]);

  const frameDiv10Style: CSSProperties = useMemo(() => {
    return {
      marginLeft: frameDivMarginLeft,
    };
  }, [frameDivMarginLeft]);

  const groupDiv1Style: CSSProperties = useMemo(() => {
    return {
      backgroundColor: groupDivBackgroundColor,
      border: groupDivBorder,
      padding: groupDivPadding,
    };
  }, [groupDivBackgroundColor, groupDivBorder, groupDivPadding]);

  const rectangleDiv5Style: CSSProperties = useMemo(() => {
    return {
      width: rectangleDivWidth,
      backgroundColor: rectangleDivBackgroundColor1,
      border: rectangleDivBorder,
    };
  }, [rectangleDivWidth, rectangleDivBackgroundColor1, rectangleDivBorder]);

  const div1Style: CSSProperties = useMemo(() => {
    return {
      color: divColor,
    };
  }, [divColor]);

  return (
    <div className={[styles.rectangleParent, className].join(" ")}>
      <div className={styles.frameChild} />
      <div className={styles.frameItem} style={rectangleDiv4Style} />
      <div className={styles.informationData}>
        <div className={styles.informationLevel}>
          <div className={styles.informationDetails}>
            <b className={styles.b}>{prop}</b>
            <div
              className={styles.informationDetailsInner}
              style={frameDiv10Style}
            >
              <div className={styles.rectangleGroup} style={groupDiv1Style}>
                <div className={styles.frameInner} style={rectangleDiv5Style} />
                <div className={styles.div} style={div1Style}>
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

export default GroupComponent;
