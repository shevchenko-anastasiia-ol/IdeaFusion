import { FunctionComponent, useMemo, type CSSProperties } from "react";
import styles from "./UT3_FrameComponent2.module.css";

export type FrameComponent2Type = {
  className?: string;
  prop?: string;
  prop1?: string;
  prop2?: string;

  /** Style props */
  userAvatarTwoBackgroundColor?: CSSProperties["backgroundColor"];
};

const FrameComponent2: FunctionComponent<FrameComponent2Type> = ({
  className = "",
  userAvatarTwoBackgroundColor,
  prop,
  prop1,
  prop2,
}) => {
  const userAvatarTwoStyle: CSSProperties = useMemo(() => {
    return {
      backgroundColor: userAvatarTwoBackgroundColor,
    };
  }, [userAvatarTwoBackgroundColor]);

  return (
    <div className={[styles.frameParent, className].join(" ")}>
      <div className={styles.userAvatarTwoParent}>
        <div className={styles.userAvatarTwo} style={userAvatarTwoStyle} />
        <h3 className={styles.h3}>{prop1}</h3>
      </div>
      <div className={styles.userNameContainerTwoWrapper}>
        <div className={styles.userNameContainerTwo}>
          <div className={styles.userNameTwo}>
            <h3 className={styles.h32}>{prop2}</h3>
          </div>
          <div className={styles.div}>{prop}</div>
        </div>
      </div>
    </div>
  );
};

export default FrameComponent2;
