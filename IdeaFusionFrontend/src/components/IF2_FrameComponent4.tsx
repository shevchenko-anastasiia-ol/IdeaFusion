import { FunctionComponent } from "react";
import styles from "./IF2_FrameComponent4.module.css";

const FrameComponent4: FunctionComponent = ({ className = "" }: any) => {
  return (
    <section className={[styles.rectangleParent, className].join(" ")}>
      <div className={styles.title}>Статистика</div>
      <div className={styles.statsBox}>
        {[
          { num: "8",  label: "проєктів" },
          { num: "5",  label: "колаборацій" },
          { num: "18", label: "збережених" },
        ].map((s) => (
          <div key={s.label} className={styles.statCard}>
            <span className={styles.num}>{s.num}</span>
            <span className={styles.label}>{s.label}</span>
          </div>
        ))}
      </div>
    </section>
  );
};

export default FrameComponent4;