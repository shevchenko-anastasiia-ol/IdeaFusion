import { FunctionComponent } from "react";
import NavigationNestedFour from "./UT2_NavigationNestedFour";
import styles from "./UT2_FrameComponent2.module.css";

export type FrameComponent2Type = {
  className?: string;
};

const FrameComponent2: FunctionComponent<FrameComponent2Type> = ({
  className = "",
}) => {
  return (
    <div className={[styles.frameParent, className].join(" ")}>
      <div className={styles.frameGroup}>
        <section className={styles.rectangleParent}>
          <div className={styles.frameChild} />
          <div className={styles.navigationNestedOne}>
            <div className={styles.specificationContainer}>
              <h3 className={styles.h3}>Специфікація команди</h3>
            </div>
            <div className={styles.existingInnerParent}>
              <div className={styles.existingInner}>
                <div className={styles.div}>Оберіть з наявних</div>
              </div>
              <div className={styles.rectangleGroup}>
                <div className={styles.frameItem} />
                <img className={styles.frameInner} alt="" src="/Arrow-5.svg" />
              </div>
            </div>
          </div>
          <div className={styles.div2}>Створіть власну</div>
          <input className={styles.rectangleInput} type="text" />
        </section>
        <NavigationNestedFour prop="Аватар команди" />
        <div className={styles.rectangleContainer}>
          <div className={styles.rectangleDiv} />
          <div className={styles.rolesInner}>
            <h3 className={styles.h3}>Необхідні ролі</h3>
          </div>
          <div className={styles.frameDiv}>
            <div className={styles.frameChild2} />
            <img className={styles.arrowIcon} alt="" src="/Arrow-5.svg" />
          </div>
          <div className={styles.wrapper}>
            <div className={styles.div}>
              Вкажіть, які спеціалісти потрібні у команду
            </div>
          </div>
        </div>
      </div>
      <div className={styles.frameWrapper}>
        <button className={styles.frameButton}>
          <div className={styles.frameChild3} />
          <h3 className={styles.h33}>Створити команду</h3>
        </button>
      </div>
    </div>
  );
};

export default FrameComponent2;
