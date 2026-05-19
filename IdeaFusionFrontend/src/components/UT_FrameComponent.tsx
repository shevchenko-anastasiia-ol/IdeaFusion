import { FunctionComponent } from "react";
import styles from "./UT_FrameComponent.module.css";

export type FrameComponentType = {
  className?: string;
};

const FrameComponent: FunctionComponent<FrameComponentType> = ({
  className = "",
}) => {
  return (
    <section className={[styles.rectangleParent, className].join(" ")}>
      <div className={styles.frameChild} />
      <img
        className={styles.frameItem}
        loading="lazy"
        alt=""
        src="/Group-11@2x.png"
      />
      <div className={styles.roleManagement}>
        <div className={styles.roleContainer}>
          <button className={styles.primaryRole}>
            <img
              className={styles.primaryRoleChild}
              loading="lazy"
              alt=""
              src="/Group-2.svg"
            />
          </button>
          <div className={styles.frameParent}>
            <button className={styles.frameWrapper}>
              <img
                className={styles.frameInner}
                loading="lazy"
                alt=""
                src="/Group-3.svg"
              />
            </button>
            <button className={styles.frameContainer}>
              <img
                className={styles.groupIcon}
                loading="lazy"
                alt=""
                src="/Group-4.svg"
              />
            </button>
            <img
              className={styles.starRatingIcon}
              alt=""
              src="/Star-Icon.svg"
            />
          </div>
          <button className={styles.frameContainer}>
            <div className={styles.rectangleGroup}>
              <div className={styles.rectangleDiv} />
              <div className={styles.frameChild2} />
              <img
                className={styles.rectangleIcon}
                loading="lazy"
                alt=""
                src="/Rectangle-8.svg"
              />
              <div className={styles.frameChild3} />
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
