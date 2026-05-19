import { FunctionComponent } from "react";
import ProfileRows from "./UT_ProfileRows";
import styles from "./UT_GroupComponent2.module.css";

export type GroupComponent2Type = {
  className?: string;
};

const GroupComponent2: FunctionComponent<GroupComponent2Type> = ({
  className = "",
}) => {
  return (
    <div className={[styles.rectangleParent, className].join(" ")}>
      <div className={styles.frameChild} />
      <div className={styles.identityStatus}>
        <div className={styles.profileSignatureWrapper}>
          <div className={styles.profileSignature} />
        </div>
        <div className={styles.signatureContainer}>
          <ProfileRows
            profileRowsFlex="unset"
            profileRowsMinWidth="unset"
            avatarInitialsBackgroundColor="#4002aa"
            prop="ДМ"
            prop1="Дмитро Мельник"
            h3Width="175px"
            h3Display="inline-block"
            react="2 години тому"
          />
          <div className={styles.stateBanner}>
            <div className={styles.rectangleGroup}>
              <div className={styles.frameItem} />
              <div className={styles.div}>Нове</div>
            </div>
          </div>
        </div>
        <div className={styles.joinRequest}>
          <div className={styles.div2}>
            Хочу приєднатись допроєкту банкінгу як розробник
          </div>
        </div>
      </div>
      <div className={styles.action}>
        <button className={styles.rectangleContainer}>
          <div className={styles.frameInner} />
          <div className={styles.div3}>Прийняти</div>
        </button>
        <div className={styles.wrapper}>
          <div className={styles.div4}>Відхилити</div>
        </div>
      </div>
    </div>
  );
};

export default GroupComponent2;
