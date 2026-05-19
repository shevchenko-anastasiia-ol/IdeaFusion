import { FunctionComponent } from "react";
import styles from "./UT_FrameComponent1.module.css";

export type FrameComponent1Type = {
  className?: string;
};

const FrameComponent1: FunctionComponent<FrameComponent1Type> = ({
  className = "",
}) => {
  return (
    <section className={[styles.rectangleParent, className].join(" ")}>
      <div className={styles.frameChild} />
      <div className={styles.infoContainer}>
        <div className={styles.ellipseParent}>
          <div className={styles.frameItem} />
          <h1 className={styles.h1}>АМ</h1>
        </div>
        <div className={styles.descriptionContainer}>
          <div className={styles.projectInfo}>
            <div className={styles.parent}>
              <h2 className={styles.h2}>Арт майбутнього</h2>
              <div className={styles.designColumn}>
                <div className={styles.rectangleGroup}>
                  <div className={styles.frameInner} />
                  <div className={styles.div}>Дизайн</div>
                </div>
              </div>
            </div>
            <h3 className={styles.illustrator}>
              Графічний дизайн · Illustrator · Арт
            </h3>
          </div>
        </div>
      </div>
      <div className={styles.avatarControl}>
        <button className={styles.changePhoto}>
          <div className={styles.changePhotoChild} />
          <h3 className={styles.h3}>Редагувати фото профілю</h3>
        </button>
      </div>
    </section>
  );
};

export default FrameComponent1;
