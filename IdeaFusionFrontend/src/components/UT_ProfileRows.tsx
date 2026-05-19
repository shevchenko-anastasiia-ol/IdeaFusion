import { FunctionComponent, useMemo, type CSSProperties } from "react";
import styles from "./UT_ProfileRows.module.css";

export type ProfileRowsType = {
  className?: string;
  prop?: string;
  prop1?: string;
  react?: string;

  /** Style props */
  profileRowsFlex?: CSSProperties["flex"];
  profileRowsMinWidth?: CSSProperties["minWidth"];
  avatarInitialsBackgroundColor?: CSSProperties["backgroundColor"];
  h3Width?: CSSProperties["width"];
  h3Display?: CSSProperties["display"];
};

const ProfileRows: FunctionComponent<ProfileRowsType> = ({
  className = "",
  profileRowsFlex,
  profileRowsMinWidth,
  avatarInitialsBackgroundColor,
  prop,
  prop1,
  h3Width,
  h3Display,
  react,
}) => {
  const profileRowsStyle: CSSProperties = useMemo(() => {
    return {
      flex: profileRowsFlex,
      minWidth: profileRowsMinWidth,
    };
  }, [profileRowsFlex, profileRowsMinWidth]);

  const avatarInitialsStyle: CSSProperties = useMemo(() => {
    return {
      backgroundColor: avatarInitialsBackgroundColor,
    };
  }, [avatarInitialsBackgroundColor]);

  const h3Style: CSSProperties = useMemo(() => {
    return {
      width: h3Width,
      display: h3Display,
    };
  }, [h3Width, h3Display]);

  return (
    <div
      className={[styles.profileRows, className].join(" ")}
      style={profileRowsStyle}
    >
      <div className={styles.avatarInitialsParent}>
        <div className={styles.avatarInitials} style={avatarInitialsStyle} />
        <h3 className={styles.h3}>{prop}</h3>
      </div>
      <div className={styles.roleDescriptions}>
        <div className={styles.namesRole}>
          <div className={styles.fullNames}>
            <h3 className={styles.h32} style={h3Style}>
              {prop1}
            </h3>
          </div>
          <div className={styles.react}>{react}</div>
        </div>
      </div>
    </div>
  );
};

export default ProfileRows;
