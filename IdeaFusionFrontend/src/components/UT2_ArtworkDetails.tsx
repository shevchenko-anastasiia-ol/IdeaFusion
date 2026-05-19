import { FunctionComponent } from "react";
import styles from "./UT2_ArtworkDetails.module.css";

export type ArtworkDetailsType = {
  className?: string;
};

const ArtworkDetails: FunctionComponent<ArtworkDetailsType> = ({
  className = "",
}) => {
  return (
    <section className={[styles.artworkDetails, className].join(" ")}>
      <div className={styles.artworkDetailsChild} />
      <div className={styles.mainDetails}>
        <h3 className={styles.h3}>Основне</h3>
      </div>
      <div className={styles.detailContainers}>
        <div className={styles.wrapper}>
          <div className={styles.div}>Назва</div>
        </div>
        <input
          className={styles.detailValues}
          placeholder="«Кадри, що дихають»"
          type="text"
        />
      </div>
      <div className={styles.detailContainers2}>
        <div className={styles.container}>
          <div className={styles.div}>Опис</div>
        </div>
        <div className={styles.rectangleParent}>
          <div className={styles.frameChild} />
          <div className={styles.div3}>
            Серія анімованих ілюстрацій, де кожен кадр — окрема емоція. Проєкт
            досліджує межу між статичним і живим.
          </div>
        </div>
      </div>
      <div className={styles.detailContainers}>
        <div className={styles.frame}>
          <div className={styles.div4}>Тег</div>
        </div>
        <input
          className={styles.detailContainersChild}
          placeholder="Анімація · Concept Art"
          type="text"
        />
      </div>
    </section>
  );
};

export default ArtworkDetails;
