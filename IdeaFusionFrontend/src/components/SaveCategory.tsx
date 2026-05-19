import { FunctionComponent } from "react";
import styles from "./SaveCategory.module.css";

export type SaveCategoryType = {
  className?: string;
  title?: string;
  count?: number;
};

const SaveCategory: FunctionComponent<SaveCategoryType> = ({
  className = "",
  title = "Категорія",
  count = 0,
}) => {
  return (
    <div className={[styles.saveCategory, className].join(" ")}>
      <div className={styles.categoryHeader}>
        <span className={styles.categoryTitle}>{title}</span>
        <span className={styles.categoryCount}>{count} збережено</span>
      </div>
      <div className={styles.categoryItems}>
        {Array.from({ length: Math.min(count, 4) }).map((_, i) => (
          <div key={i} className={styles.categoryItem}>
            <div className={styles.itemImage} />
            <span className={styles.itemTitle}>Елемент {i + 1}</span>
            <span className={styles.itemSubtitle}>Підзаголовок</span>
          </div>
        ))}
      </div>
    </div>
  );
};

export default SaveCategory;
