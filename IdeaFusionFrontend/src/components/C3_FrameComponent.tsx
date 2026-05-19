import { FunctionComponent } from "react";
import styles from "./C3_FrameComponent.module.css";

export type FrameComponentType = {
  className?: string;
};

const FrameComponent: FunctionComponent<FrameComponentType> = ({
  className = "",
}) => {
  return (
    <section className={[styles.frameParent, className].join(" ")}>
      <div className={styles.rectangleParent}>
        <div className={styles.frameChild} />
        <div className={styles.avatarOuter}>
          <div className={styles.avatarMini} />
          <h3 className={styles.h3}>АШ</h3>
        </div>
        <img
          className={styles.frameItem}
          loading="lazy"
          alt=""
          src="/Group-34@2x.png"
        />
      </div>
      <img
        className={styles.frameInner}
        loading="lazy"
        alt=""
        src="/Group-2.svg"
      />
      <img
        className={styles.groupIcon}
        loading="lazy"
        alt=""
        src="/Group-3.svg"
      />
      <img
        className={styles.frameChild2}
        loading="lazy"
        alt=""
        src="/Group-4.svg"
      />
      <img
        className={styles.decorationStarIcon}
        loading="lazy"
        alt=""
        src="/Star-1.svg"
      />
      <div className={styles.adBackground} />
      <div className={styles.adBackground2} />
      <img
        className={styles.adBackgroundIcon}
        loading="lazy"
        alt=""
        src="/Rectangle-8.svg"
      />
      <div className={styles.adBackground3} />
    </section>
  );
};

export default FrameComponent;
