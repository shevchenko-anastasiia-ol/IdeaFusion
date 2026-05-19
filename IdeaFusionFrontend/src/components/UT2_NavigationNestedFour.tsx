import { FunctionComponent, useMemo, type CSSProperties } from "react";
import styles from "./UT2_NavigationNestedFour.module.css";

export type NavigationNestedFourType = {
  className?: string;
  prop?: string;

  /** Style props */
  navigationNestedFourTop?: CSSProperties["top"];
  navigationNestedFourLeft?: CSSProperties["left"];
};

const NavigationNestedFour: FunctionComponent<NavigationNestedFourType> = ({
  className = "",
  navigationNestedFourTop,
  navigationNestedFourLeft,
  prop,
}) => {
  const navigationNestedFourStyle: CSSProperties = useMemo(() => {
    return {
      top: navigationNestedFourTop,
      left: navigationNestedFourLeft,
    };
  }, [navigationNestedFourTop, navigationNestedFourLeft]);

  return (
    <section
      className={[styles.navigationNestedFour, className].join(" ")}
      style={navigationNestedFourStyle}
    >
      <div className={styles.navigationNestedFourChild} />
      <div className={styles.avatarInner}>
        <h3 className={styles.h3}>{prop}</h3>
      </div>
      <div className={styles.parent}>
        <label className={styles.label} htmlFor="file-loco-0010-s2-1:126">
          <div className={styles.frameChild} />
          <div className={styles.pngJpgContainer}>
            Оберіть файли або перетягніть сюди
            <br />
            PNG, JPG · до 50 МБ
          </div>
        </label>
        <input
          className={styles.input}
          type="file"
          id="file-loco-0010-s2-1:126"
        />
      </div>
    </section>
  );
};

export default NavigationNestedFour;
