import { FunctionComponent, useState } from "react";
import { Box, Typography } from "@mui/material";
import styles from "./GroupComponent1.module.css";

export type GroupComponent1Type = {
  className?: string;
  activeFilter?: string;
  onFilterChange?: (filter: string) => void;
};

const GroupComponent1: FunctionComponent<GroupComponent1Type> = ({
  className = "",
  activeFilter = "Усі",
  onFilterChange,
}) => {
  const filters = ["Усі", "Активні", "Завершені", "У пошуку"];

  return (
    <section className={[styles.rectangleParent, className].join(" ")}>
      <Box className={styles.frameChild} />
      <Box className={styles.frameParent}>
        <Box className={styles.wrapper}>
          <Typography className={styles.statusLabel}>Статус</Typography>
        </Box>
        <Box className={styles.statusOptions}>
          {filters.map((f) => (
            <button
              key={f}
              className={activeFilter === f ? styles.btnActive : styles.btn}
              onClick={() => onFilterChange?.(f)}
            >
              {f}
            </button>
          ))}
        </Box>
      </Box>
      <Box className={styles.container}>
        <Typography className={styles.h34}>1 активні команди</Typography>
      </Box>
    </section>
  );
};

export default GroupComponent1;