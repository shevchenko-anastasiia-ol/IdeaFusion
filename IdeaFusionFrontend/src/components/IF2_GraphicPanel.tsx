import { FunctionComponent } from "react";
import styles from "./IF2_GraphicPanel.module.css";

export type GraphicPanelType = {
  className?: string;
};

const GraphicPanel: FunctionComponent<GraphicPanelType> = ({
  className = "",
}) => {
  return (
    <section className={[styles.graphicPanel, className].join(" ")}>
      <div className={styles.graphicPanelChild} />
      <div className={styles.segmentPanel}>
        <img
          className={styles.segmentPanelChild}
          loading="lazy"
          alt=""
          src="/Group-1@2x.png"
        />
      </div>
      <div className={styles.previewZone}>
        <button className={styles.controlBlock}>
          <img
            className={styles.controlBlockChild}
            loading="lazy"
            alt=""
            src="/Group-2.svg"
          />
        </button>
        <div className={styles.starRating}>
          <button className={styles.profileDetails}>
            <img
              className={styles.profileDetailsChild}
              loading="lazy"
              alt=""
              src="/Group-3.svg"
            />
          </button>
          <button className={styles.starRatingInner}>
            <img
              className={styles.frameChild}
              loading="lazy"
              alt=""
              src="/Group-4.svg"
            />
          </button>
          <img
            className={styles.starRatingChild}
            alt=""
            src="/Star-Element.svg"
          />
        </div>
      </div>
      <button className={styles.highlightBar}>
        <div className={styles.rectangleParent}>
          <div className={styles.frameItem} />
          <div className={styles.frameInner} />
          <img className={styles.rectangleIcon} alt="" src="/Rectangle-8.svg" />
          <div className={styles.rectangleDiv} />
        </div>
      </button>
    </section>
  );
};

export default GraphicPanel;
