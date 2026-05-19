import { FunctionComponent } from "react";
import styles from "./UT_GroupComponent4.module.css";

export type GroupComponent4Type = {
  className?: string;
};

const GroupComponent4: FunctionComponent<GroupComponent4Type> = ({
  className = "",
}) => {
  return (
    <section className={[styles.vectorParent, className].join(" ")}>
      <img className={styles.frameChild} alt="" src="/Arrow-10.svg" />
      <div className={styles.frameItem} />
      <div className={styles.needsRoles}>
        <h3 className={styles.h3}>Необхідні ролі</h3>
      </div>
      <div className={styles.roleChoices}>
        <div className={styles.roleChoicesChild} />
        <h3 className={styles.react}>Розробник React</h3>
      </div>
      <div className={styles.roleChoices2}>
        <div className={styles.roleChoicesItem} />
        <h3 className={styles.react}>Композитор</h3>
      </div>
      <div className={styles.roleChoices3}>
        <div className={styles.roleChoicesItem} />
        <h3 className={styles.react}>3D артист</h3>
      </div>
      <div className={styles.roleChoices3}>
        <div className={styles.roleChoicesItem} />
        <h3 className={styles.react}>UI/UX дизайнер</h3>
      </div>
      <div className={styles.roleChoices}>
        <div className={styles.roleChoicesChild2} />
        <h3 className={styles.react}>Графічний дизайнер</h3>
      </div>
      <div className={styles.roleChoices6}>
        <div className={styles.roleChoicesChild3} />
        <h3 className={styles.h34}>+ Додати нову роль</h3>
      </div>
    </section>
  );
};

export default GroupComponent4;
