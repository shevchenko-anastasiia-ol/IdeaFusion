import { FunctionComponent } from "react";
import FrameComponent5 from "./IF2_FrameComponent5";
import styles from "./IF2_PageHeader1.module.css";

export type PageHeader1Type = {
  className?: string;
};

const PageHeader1: FunctionComponent<PageHeader1Type> = ({
  className = "",
}) => {
  return (
    <section className={[styles.pageHeader, className].join(" ")}>
      <div className={styles.pageHeaderChild} />
      <div className={styles.frameParent}>
        <div className={styles.wrapper}>
          <h3 className={styles.h3}>Зміна паролю</h3>
        </div>
        <FrameComponent5 prop="Поточний пароль" />
        <FrameComponent5 frameDivPadding="0px 16px" prop="Новий пароль" />
      </div>
      <input className={styles.pageHeaderItem} type="text" />
      <div className={styles.div}>Підтвердити пароль</div>
      <div className={styles.editItem}>
        <button className={styles.passSetting}>
          <div className={styles.passSettingChild} />
          <h3 className={styles.h32}>Оновити пароль</h3>
        </button>
      </div>
    </section>
  );
};

export default PageHeader1;
