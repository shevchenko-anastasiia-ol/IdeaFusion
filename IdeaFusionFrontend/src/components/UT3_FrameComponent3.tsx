import { FunctionComponent } from "react";
import styles from "./UT3_FrameComponent3.module.css";

export type FrameComponent3Type = {
  className?: string;
};

const FrameComponent3: FunctionComponent<FrameComponent3Type> = ({
  className = "",
}) => {
  return (
    <div className={[styles.frameParent, className].join(" ")}>
      <div className={styles.rectangleParent}>
        <div className={styles.frameChild} />
        <div className={styles.ellipseParent}>
          <div className={styles.frameItem} />
          <h3 className={styles.h3}>АШ</h3>
        </div>
        <img
          className={styles.frameInner}
          loading="lazy"
          alt=""
          src="/Group-11@2x.png"
        />
      </div>
      <img
        className={styles.groupIcon}
        loading="lazy"
        alt=""
        src="/Group-2.svg"
      />
      <img
        className={styles.frameChild2}
        loading="lazy"
        alt=""
        src="/Group-3.svg"
      />
      <img
        className={styles.frameChild3}
        loading="lazy"
        alt=""
        src="/Group-4.svg"
      />
      <img
        className={styles.userStarIcon}
        loading="lazy"
        alt=""
        src="/Star-Icon.svg"
      />
      <div className={styles.rectangleDiv} />
      <div className={styles.frameChild4} />
      <img
        className={styles.rectangleIcon}
        loading="lazy"
        alt=""
        src="/Rectangle-8.svg"
      />
      <div className={styles.frameChild5} />
    </div>
  );
};

export default FrameComponent3;
