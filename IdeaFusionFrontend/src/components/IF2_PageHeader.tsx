import { FunctionComponent } from "react";
import styles from "./IF2_PageHeader.module.css";

export type PageHeaderType = {
  className?: string;
};

const PageHeader: FunctionComponent<PageHeaderType> = ({ className = "" }) => {
  return (
    <section className={[styles.pageHeader, className].join(" ")}>
      <div className={styles.pageHeaderChild} />
      <div className={styles.detail}>
        <div className={styles.category}>
          <h3 className={styles.h3}>Особиста інформація</h3>
        </div>
        <div className={styles.accessItem}>
          <div className={styles.dataAccess}>
            <div className={styles.div}>Повне ім’я</div>
          </div>
          <input
            className={styles.authorName}
            placeholder="Шевченко Анастасія"
            type="text"
          />
        </div>
        <div className={styles.accessItem}>
          <div className={styles.emailWrapper}>
            <div className={styles.email}>Email</div>
          </div>
          <input
            className={styles.emailAddress}
            placeholder="shevchenko.anastasiia.o@chnu.edu.ua"
            type="text"
          />
        </div>
      </div>
      <div className={styles.attributesList}>
        <div className={styles.attributesListChild} />
        <input
          className={styles.uiux}
          placeholder="UI/UX дизайн · Figma · Анімації"
          type="text"
        />
        <div className={styles.div2}>Спеціалізація</div>
      </div>
      <div className={styles.verificationZone}>
        <button className={styles.interfaceField}>
          <div className={styles.interfaceFieldChild} />
          <h3 className={styles.h32}>Зберегти зміни</h3>
        </button>
      </div>
    </section>
  );
};

export default PageHeader;
