import { FunctionComponent, useMemo, type CSSProperties } from "react";
import styles from "./C3_GroupComponent.module.css";

export type GroupComponentType = {
  className?: string;
  prop?: string;
  prop1?: string;
  prop2?: string;
  prop3?: string;
  prop4?: string;

  /** Style props */
  groupDivAlignSelf?: CSSProperties["alignSelf"];
  groupDivPadding?: CSSProperties["padding"];
  groupDivGap?: CSSProperties["gap"];
  groupDivWidth?: CSSProperties["width"];
  h3Left?: CSSProperties["left"];
  h3MinWidth?: CSSProperties["minWidth"];
  frameDivPadding?: CSSProperties["padding"];
  statusBackgroundBackgroundColor?: CSSProperties["backgroundColor"];
  statusBackgroundBorder?: CSSProperties["border"];
  frameDivWidth?: CSSProperties["width"];
  divColor?: CSSProperties["color"];
};

const GroupComponent: FunctionComponent<GroupComponentType> = ({
  className = "",
  prop,
  prop1,
  prop2,
  prop3,
  prop4,
  groupDivAlignSelf,
  groupDivPadding,
  groupDivGap,
  groupDivWidth,
  h3Left,
  h3MinWidth,
  frameDivPadding,
  statusBackgroundBackgroundColor,
  statusBackgroundBorder,
  frameDivWidth,
  divColor,
}) => {
  const groupDivStyle: CSSProperties = useMemo(() => {
    return {
      alignSelf: groupDivAlignSelf,
      padding: groupDivPadding,
      gap: groupDivGap,
      width: groupDivWidth,
    };
  }, [groupDivAlignSelf, groupDivPadding, groupDivGap, groupDivWidth]);

  const h3Style: CSSProperties = useMemo(() => {
    return {
      left: h3Left,
      minWidth: h3MinWidth,
    };
  }, [h3Left, h3MinWidth]);

  const frameDivStyle: CSSProperties = useMemo(() => {
    return {
      padding: frameDivPadding,
    };
  }, [frameDivPadding]);

  const statusBackgroundStyle: CSSProperties = useMemo(() => {
    return {
      backgroundColor: statusBackgroundBackgroundColor,
      border: statusBackgroundBorder,
    };
  }, [statusBackgroundBackgroundColor, statusBackgroundBorder]);

  const frameDiv1Style: CSSProperties = useMemo(() => {
    return {
      width: frameDivWidth,
    };
  }, [frameDivWidth]);

  const divStyle: CSSProperties = useMemo(() => {
    return {
      color: divColor,
    };
  }, [divColor]);

  return (
    <div
      className={[styles.rectangleParent, className].join(" ")}
      style={groupDivStyle}
    >
      <div className={styles.frameChild} />
      <div className={styles.frameParent}>
        <div className={styles.frameGroup}>
          <div className={styles.frameContainer}>
            <div className={styles.avatarMultipleParent}>
              <div className={styles.avatarMultiple} />
              <h3 className={styles.h3} style={h3Style}>
                {prop}
              </h3>
            </div>
            <div className={styles.frameWrapper}>
              <div className={styles.frameDiv}>
                <div className={styles.wrapper}>
                  <h3 className={styles.h32}>{prop1}</h3>
                </div>
                <div className={styles.div}>{prop2}</div>
              </div>
            </div>
          </div>
          <div className={styles.frameWrapper2} style={frameDivStyle}>
            <div className={styles.statusBackgroundParent}>
              <div
                className={styles.statusBackground}
                style={statusBackgroundStyle}
              />
              <div className={styles.div2}>{prop3}</div>
            </div>
          </div>
        </div>
        <div className={styles.container} style={frameDiv1Style}>
          <div
            className={styles.div3}
          >{`Пропоную об’єднатись, я пишу музику, ти малюєш арт для заставки пісні `}</div>
        </div>
      </div>
      <div className={styles.decisionStatus}>
        <div className={styles.div4} style={divStyle}>
          {prop4}
        </div>
      </div>
    </div>
  );
};

export default GroupComponent;
