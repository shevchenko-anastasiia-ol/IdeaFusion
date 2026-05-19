import { FunctionComponent } from "react";
import styles from "./UT2_FrameComponent1.module.css";

export type FrameComponent1Type = {
  className?: string;
};

const FrameComponent1: FunctionComponent<FrameComponent1Type> = ({
  className = "",
}) => {
  return (
    <section className={[styles.rectangleParent, className].join(" ")}>
      <div className={styles.frameChild} />
      <div className={styles.publishSelection}>
        <h3 className={styles.h3}>Оберіть від кого публікувати</h3>
      </div>
      <div className={styles.frameParent}>
        <div className={styles.rectangleGroup}>
          <div className={styles.frameItem} />
          <div className={styles.personalContainer}>
            <button className={styles.button}>Від свого імені</button>
          </div>
          <h3 className={styles.h32}>Анастасія Шевченко</h3>
        </div>
        <div className={styles.rectangleContainer}>
          <div className={styles.frameInner} />
          <button className={styles.button2}>Від імені команди</button>
          <div className={styles.teamSelection}>
            <div className={styles.teamContainer}>
              <div className={styles.div}>Оберіть команду</div>
              <div className={styles.teamContainerInner}>
                <img className={styles.arrowIcon} alt="" src="/Arrow-5.svg" />
              </div>
            </div>
          </div>
        </div>
      </div>
    </section>
  );
};

export default FrameComponent1;
