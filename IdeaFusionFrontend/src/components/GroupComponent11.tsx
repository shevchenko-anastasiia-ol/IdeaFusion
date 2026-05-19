import { FunctionComponent, useState } from "react";
import { Box } from "@mui/material";
import styles from "./GroupComponent11.module.css";

export type GroupComponent11Type = {
  className?: string;
  onTypeChange?: (type: string) => void;
  onCategoryChange?: (category: string) => void;
};

const GroupComponent11: FunctionComponent<GroupComponent11Type> = ({
  className = "",
  onTypeChange,
  onCategoryChange,
}) => {
  const [activeType, setActiveType] = useState("Всі");
  const [activeCategory, setActiveCategory] = useState("Всі");

  const types = ["Всі", "Пости", "Користувачі", "Команди"];
  const categories = ["Всі", "Дизайн", "Музика", "Фото", "Анімація", "Арт", "Gamedev", "3D", "UI/UX"];

  const handleType = (t: string) => {
    setActiveType(t);
    onTypeChange?.(t);
  };

  const handleCategory = (c: string) => {
    setActiveCategory(c);
    onCategoryChange?.(c);
  };

  const activeStyle: React.CSSProperties = {
    background: "#7c5cfc", color: "#fff", border: "none",
    borderRadius: "30px", padding: "8px 20px", fontSize: "16px",
    cursor: "pointer", fontFamily: "var(--font-inter)", whiteSpace: "nowrap",
  };
  const inactiveStyle: React.CSSProperties = {
    background: "#2a2a2a", color: "#f0f0f0", border: "none",
    borderRadius: "30px", padding: "8px 20px", fontSize: "16px",
    cursor: "pointer", fontFamily: "var(--font-inter)", whiteSpace: "nowrap",
  };

  return (
    <section className={[styles.rectangleParent, className].join(" ")}>
      {/* Тип */}
      <Box className={styles.categoryFilter}>
        {types.map(t => (
          <button key={t} style={activeType === t ? activeStyle : inactiveStyle}
            onClick={() => handleType(t)}>
            {t}
          </button>
        ))}
      </Box>

      <Box className={styles.categorySeparator}>
        <Box className={styles.refinementCategory} />
      </Box>

      {/* Категорія */}
      <Box className={styles.genreCategory}>
        {categories.map(c => (
          <button key={c} style={activeCategory === c ? activeStyle : inactiveStyle}
            onClick={() => handleCategory(c)}>
            {c}
          </button>
        ))}
      </Box>
    </section>
  );
};

export default GroupComponent11;