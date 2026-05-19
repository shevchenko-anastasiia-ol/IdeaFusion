import { FunctionComponent } from "react";
import styles from "./UT3_FrameComponent.module.css";

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
        <div className={styles.ellipseParent}>
          <div className={styles.frameItem} />
          <h3 className={styles.h3}>АШ</h3>
        </div>
      </div>
      <img
        className={styles.frameInner}
        loading="lazy"
        alt=""
        src="/Group-11@2x.png"
      />
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
        className={styles.starShapeIcon}
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
    </section>
  );
};

export default FrameComponent;
