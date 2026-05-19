import { FunctionComponent } from "react";
import styles from "./UT2_FrameComponent.module.css";

export type FrameComponentType = {
  className?: string;
};

const FrameComponent: FunctionComponent<FrameComponentType> = ({
  className = "",
}) => {
  return (
    <section className={[styles.rectangleParent, className].join(" ")}>
      <div className={styles.frameChild} />
      <div className={styles.wrapper}>
        <h3 className={styles.h3}>Основне</h3>
      </div>
      <div className={styles.titleDescription}>
        <input className={styles.titleDescriptionChild} type="text" />
        <div className={styles.div}>Назва</div>
      </div>
      <div className={styles.titleDescription2}>
        <div className={styles.titleDescriptionItem} />
        <div className={styles.div2}>Опис</div>
      </div>
      <div className={styles.tagContainerParent}>
        <div className={styles.tagContainer}>
          <div className={styles.div3}>Тег</div>
        </div>
        <input className={styles.frameItem} type="text" />
      </div>
    </section>
  );
};

export default FrameComponent;
