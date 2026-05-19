import { FunctionComponent } from "react";
import styles from "./IF2_FrameComponent3.module.css";

export type FrameComponent3Type = {
  className?: string;
};

const FrameComponent3: FunctionComponent<FrameComponent3Type> = ({
  className = "",
}) => {
  return (
    <section className={[styles.rectangleParent, className].join(" ")}>
      <div className={styles.frameChild} />
      <div className={styles.wrapper}>
        <h3 className={styles.h3}>Команди</h3>
      </div>
      <div className={styles.teamListing}>
        <div className={styles.designElementsParent}>
          <div className={styles.designElements}>
            <div className={styles.designSeparator} />
          </div>
          <div className={styles.rectangleGroup}>
            <div className={styles.frameItem} />
            <div className={styles.graphicsDesign}>
              <h3 className={styles.h32}>Бюрократи</h3>
              <div className={styles.uiux}>UI/UX дизайн · Figma · Анімації</div>
            </div>
            <div className={styles.frameWrapper}>
              <div className={styles.frameParent}>
                <div className={styles.rectangleContainer}>
                  <div className={styles.frameInner} />
                  <div className={styles.div}>Активна</div>
                </div>
                <div className={styles.container}>
                  <div className={styles.ost}>5 учасників</div>
                </div>
              </div>
            </div>
          </div>
        </div>
        <div className={styles.frameGroup}>
          <div className={styles.lineWrapper}>
            <div className={styles.lineDiv} />
          </div>
          <div className={styles.frameDiv}>
            <div className={styles.frameItem} />
            <div className={styles.frameContainer}>
              <div className={styles.parent}>
                <h3 className={styles.h32}>Арт майбутнього</h3>
                <div className={styles.uiux}>
                  Графічний дизайн · Illustrator · Арт
                </div>
              </div>
            </div>
            <div className={styles.frameParent2}>
              <div className={styles.frameWrapper2}>
                <div className={styles.groupDiv}>
                  <div className={styles.frameChild2} />
                  <div className={styles.div}>У пошуку</div>
                </div>
              </div>
              <div className={styles.div4}>потрібен графічний дизайнер</div>
            </div>
          </div>
        </div>
      </div>
      <div className={styles.frameParent3}>
        <div className={styles.designElements}>
          <div className={styles.frameChild3} />
        </div>
        <div className={styles.rectangleParent2}>
          <div className={styles.frameItem} />
          <div className={styles.group}>
            <h3 className={styles.h32}>Мандрівники часом</h3>
            <div className={styles.teamDetails}>
              <div className={styles.ost}>Музика · Звукорежисура · OST</div>
            </div>
          </div>
          <div className={styles.teamStats}>
            <div className={styles.rectangleParent3}>
              <div className={styles.frameChild5} />
              <div className={styles.div5}>Завершена</div>
            </div>
            <div className={styles.frame}>
              <div className={styles.ost}>5 учасників</div>
            </div>
          </div>
        </div>
      </div>
    </section>
  );
};

export default FrameComponent3;
