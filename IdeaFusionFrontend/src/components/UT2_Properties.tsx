import { FunctionComponent } from "react";
import styles from "./UT2_Properties.module.css";

export type PropertiesType = {
  className?: string;
};

const Properties: FunctionComponent<PropertiesType> = ({ className = "" }) => {
  return (
    <section className={[styles.properties, className].join(" ")}>
      <div className={styles.ellipseParent}>
        <div className={styles.frameChild} />
        <h3 className={styles.h3}>АШ</h3>
      </div>
      <div className={styles.rectangleParent}>
        <div className={styles.frameItem} />
        <img className={styles.frameInner} alt="" src="/Group-2.svg" />
        <img className={styles.groupIcon} alt="" src="/Group-3.svg" />
        <img className={styles.frameChild2} alt="" src="/Group-4.svg" />
        <img className={styles.starShapeIcon} alt="" src="/Star-Icon.svg" />
        <button className={styles.rectangleButton} />
        <button className={styles.frameChild3} />
        <img className={styles.rectangleIcon} alt="" src="/Rectangle-8.svg" />
        <button className={styles.frameChild4} />
        <img
          className={styles.frameChild5}
          loading="lazy"
          alt=""
          src="/Group-11@2x.png"
        />
      </div>
    </section>
  );
};

export default Properties;
