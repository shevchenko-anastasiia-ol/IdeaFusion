import { FunctionComponent, useMemo, type CSSProperties } from "react";
import styles from "./UT3_GroupComponent1.module.css";

export type GroupComponent1Type = {
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
  careerBackgroundColor?: CSSProperties["backgroundColor"];
  careerBorder?: CSSProperties["border"];
  combinedProposalWidth?: CSSProperties["width"];
  divColor?: CSSProperties["color"];
};

const GroupComponent1: FunctionComponent<GroupComponent1Type> = ({
  className = "",
  groupDivAlignSelf,
  groupDivPadding,
  groupDivGap,
  groupDivWidth,
  prop,
  h3Left,
  h3MinWidth,
  prop1,
  prop2,
  frameDivPadding,
  careerBackgroundColor,
  careerBorder,
  prop3,
  combinedProposalWidth,
  prop4,
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

  const frameDiv1Style: CSSProperties = useMemo(() => {
    return {
      padding: frameDivPadding,
    };
  }, [frameDivPadding]);

  const careerStyle: CSSProperties = useMemo(() => {
    return {
      backgroundColor: careerBackgroundColor,
      border: careerBorder,
    };
  }, [careerBackgroundColor, careerBorder]);

  const combinedProposalStyle: CSSProperties = useMemo(() => {
    return {
      width: combinedProposalWidth,
    };
  }, [combinedProposalWidth]);

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
            <div className={styles.moreInfoParent}>
              <div className={styles.moreInfo} />
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
          <div className={styles.frameWrapper2} style={frameDiv1Style}>
            <div className={styles.careerParent}>
              <div className={styles.career} style={careerStyle} />
              <div className={styles.div2}>{prop3}</div>
            </div>
          </div>
        </div>
        <div className={styles.combinedProposal} style={combinedProposalStyle}>
          <div
            className={styles.div3}
          >{`Пропоную об’єднатись, я пишу музику, ти малюєш арт для заставки пісні `}</div>
        </div>
      </div>
      <div className={styles.container}>
        <div className={styles.div4} style={divStyle}>
          {prop4}
        </div>
      </div>
    </div>
  );
};

export default GroupComponent1;
