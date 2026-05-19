import { FunctionComponent, useState } from "react";
import { Box, Typography } from "@mui/material";
import { useNavigate } from "react-router-dom";
import styles from "./FrameComponent111.module.css";

export type FrameComponent111Type = {
  className?: string;
  activeFilter?: string;
  onFilterChange?: (filter: string) => void;
};

const FrameComponent111: FunctionComponent<FrameComponent111Type> = ({
  className = "",
  activeFilter: externalFilter,
  onFilterChange,
}) => {
  const navigate = useNavigate();
  const [internalFilter, setInternalFilter] = useState("Всі");

  const active = externalFilter ?? internalFilter;
  const setActive = (f: string) => {
    setInternalFilter(f);
    onFilterChange?.(f);
  };

  const allFilters = ["Всі", "Дизайн", "Музика", "Фото", "Анімація", "Арт", "Gamedev", "3D", "UI/UX"];

  return (
    <section className={[styles.rectangleParent, className].join(" ")}>
      <Box className={styles.frameChild} />
      <Box className={styles.wrapper}>
        <Typography className={styles.button} variantMapping={{ inherit: "Button" }}>
          Стрічка
        </Typography>
      </Box>
      <Box className={styles.frameParent}>
        <Box className={styles.frameGroup}>
          {allFilters.map((f) => (
            <button
              key={f}
              onClick={() => setActive(f)}
              className={active === f ? styles.filterActive : styles.filterBtn}
            >
              {f}
            </button>
          ))}
        </Box>
      </Box>
    </section>
  );
};

export default FrameComponent111;