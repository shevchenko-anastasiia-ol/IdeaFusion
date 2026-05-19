import { FunctionComponent, useMemo, type CSSProperties } from "react";
import styles from "./UT_MemberProfiles.module.css";

export type MemberProfilesType = {
  className?: string;
  prop?: string;
  prop1?: string;
  prop2?: string;

  /** Style props */
  memberProfilesAlignItems?: CSSProperties["alignItems"];
  memberProfilesAlignContent?: CSSProperties["alignContent"];
  memberProfilesPadding?: CSSProperties["padding"];
  memberProfilesGap?: CSSProperties["gap"];
  frameDivFlex?: CSSProperties["flex"];
  frameDivMinWidth?: CSSProperties["minWidth"];
  ellipseDivBackgroundColor?: CSSProperties["backgroundColor"];
  h3Left?: CSSProperties["left"];
  h3MinWidth?: CSSProperties["minWidth"];
  frameDivJustifyContent?: CSSProperties["justifyContent"];
  frameDivPadding?: CSSProperties["padding"];
};

const MemberProfiles: FunctionComponent<MemberProfilesType> = ({
  className = "",
  memberProfilesAlignItems,
  memberProfilesAlignContent,
  memberProfilesPadding,
  memberProfilesGap,
  frameDivFlex,
  frameDivMinWidth,
  ellipseDivBackgroundColor,
  prop,
  h3Left,
  h3MinWidth,
  prop1,
  prop2,
  frameDivJustifyContent,
  frameDivPadding,
}) => {
  const memberProfilesStyle: CSSProperties = useMemo(() => {
    return {
      alignItems: memberProfilesAlignItems,
      alignContent: memberProfilesAlignContent,
      padding: memberProfilesPadding,
      gap: memberProfilesGap,
    };
  }, [
    memberProfilesAlignItems,
    memberProfilesAlignContent,
    memberProfilesPadding,
    memberProfilesGap,
  ]);

  const frameDivStyle: CSSProperties = useMemo(() => {
    return {
      flex: frameDivFlex,
      minWidth: frameDivMinWidth,
    };
  }, [frameDivFlex, frameDivMinWidth]);

  const ellipseDivStyle: CSSProperties = useMemo(() => {
    return {
      backgroundColor: ellipseDivBackgroundColor,
    };
  }, [ellipseDivBackgroundColor]);

  const h31Style: CSSProperties = useMemo(() => {
    return {
      left: h3Left,
      minWidth: h3MinWidth,
    };
  }, [h3Left, h3MinWidth]);

  const frameDiv1Style: CSSProperties = useMemo(() => {
    return {
      justifyContent: frameDivJustifyContent,
      padding: frameDivPadding,
    };
  }, [frameDivJustifyContent, frameDivPadding]);

  return (
    <div
      className={[styles.memberProfiles, className].join(" ")}
      style={memberProfilesStyle}
    >
      <div className={styles.memberProfilesChild} />
      <div className={styles.frameParent} style={frameDivStyle}>
        <div className={styles.ellipseParent}>
          <div className={styles.frameChild} style={ellipseDivStyle} />
          <h3 className={styles.h3} style={h31Style}>
            {prop}
          </h3>
        </div>
        <div className={styles.frameWrapper}>
          <div className={styles.frameGroup}>
            <div className={styles.wrapper}>
              <h3 className={styles.h32}>{prop1}</h3>
            </div>
            <div className={styles.div}>{prop2}</div>
          </div>
        </div>
      </div>
      <div className={styles.memberProfilesInner} style={frameDiv1Style}>
        <div className={styles.deletionControlsParent}>
          <div className={styles.deletionControls}>
            <div className={styles.deletionControlsChild} />
            <div className={styles.div2}>Змінити роль</div>
          </div>
          <div className={styles.deletionControls2}>
            <div className={styles.deletionControlsItem} />
            <div className={styles.div2}>Видалити</div>
          </div>
        </div>
      </div>
    </div>
  );
};

export default MemberProfiles;
