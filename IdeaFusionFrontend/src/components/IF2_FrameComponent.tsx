import { FunctionComponent } from "react";
import styles from "./IF2_FrameComponent.module.css";

export type FrameComponentType = {
  className?: string;
};

const FrameComponent: FunctionComponent<FrameComponentType> = ({
  className = "",
}) => {
  return (
    <section className={[styles.rectangleParent, className].join(" ")}>
      <div className={styles.frameChild} />
      <div className={styles.frameWrapper}>
        <img
          className={styles.frameItem}
          loading="lazy"
          alt=""
          src="/Group-1@2x.png"
        />
      </div>
      <button className={styles.frameContainer}>
        <img
          className={styles.frameInner}
          loading="lazy"
          alt=""
          src="/Group-2.svg"
        />
      </button>
      <div className={styles.frameDiv}>
        <div className={styles.frameParent}>
          <div className={styles.frameGroup}>
            <button className={styles.frameButton}>
              <img
                className={styles.groupIcon}
                loading="lazy"
                alt=""
                src="/Group-3.svg"
              />
            </button>
            <button className={styles.frameWrapper2}>
              <img
                className={styles.frameChild2}
                loading="lazy"
                alt=""
                src="/Group-4.svg"
              />
            </button>
            <img
              className={styles.starElementIcon}
              alt=""
              src="/Star-Element.svg"
            />
          </div>
          <button className={styles.frameWrapper2}>
            <div className={styles.rectangleGroup}>
              <div className={styles.rectangleDiv} />
              <div className={styles.frameChild3} />
              <img
                className={styles.rectangleIcon}
                loading="lazy"
                alt=""
                src="/Rectangle-8.svg"
              />
              <div className={styles.frameChild4} />
            </div>
          </button>
        </div>
      </div>
      <div className={styles.ellipseParent}>
        <div className={styles.ellipseDiv} />
        <h3 className={styles.h3}>АШ</h3>
      </div>
    </section>
  );
};

export default FrameComponent;
