import { FunctionComponent } from "react";
import styles from "./UT2_ExternalLink.module.css";

export type ExternalLinkType = {
  className?: string;
};

const ExternalLink: FunctionComponent<ExternalLinkType> = ({
  className = "",
}) => {
  return (
    <div className={[styles.externalLink, className].join(" ")}>
      <div className={styles.externalLinkChild} />
      <div className={styles.linkSelection}>
        <h3 className={styles.h3}>Зовнішнє посилання</h3>
      </div>
      <input
        className={styles.externalLinkAddress}
        placeholder="behance.net/kadry-project"
        type="text"
      />
      <div className={styles.linkDescription}>
        <div className={styles.behanceDribbbleFigma}>
          Behance, Dribbble, Figma, SoundCloud, GitHub тощо
        </div>
      </div>
    </div>
  );
};

export default ExternalLink;
