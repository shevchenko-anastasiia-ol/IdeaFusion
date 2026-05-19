import { FunctionComponent } from "react";
import ProfileRows from "./UT_ProfileRows";
import styles from "./UT_GroupComponent3.module.css";

export type GroupComponent3Type = {
  className?: string;
};

const GroupComponent3: FunctionComponent<GroupComponent3Type> = ({
  className = "",
}) => {
  return (
    <div className={[styles.rectangleParent, className].join(" ")}>
      <div className={styles.frameChild} />
      <ProfileRows
        profileRowsFlex="unset"
        profileRowsMinWidth="unset"
        avatarInitialsBackgroundColor="#ff6b6b"
        prop="ЮБ"
        prop1="Юлія Бондар"
        h3Width="unset"
        h3Display="unset"
        react="5 годин тому"
      />
      <div className={styles.ostGamedevWrapper}>
        <div className={styles.ost}>
          Можу допомогти з OST,маю досвід у gamedev
        </div>
      </div>
      <div className={styles.frameParent}>
        <button className={styles.rectangleGroup}>
          <div className={styles.frameItem} />
          <div className={styles.div}>Прийняти</div>
        </button>
        <div className={styles.wrapper}>
          <div className={styles.div2}>Відхилити</div>
        </div>
      </div>
    </div>
  );
};

export default GroupComponent3;
