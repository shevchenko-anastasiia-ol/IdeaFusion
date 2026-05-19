import { FunctionComponent } from "react";
import { Box, Typography } from "@mui/material";
import styles from "./IF_HeaderContainer.module.css";

export type HeaderContainerType = {
  className?: string;
};

const HeaderContainer: FunctionComponent<HeaderContainerType> = ({
  className = "",
}) => {
  return (
    <header className={[styles.headerContainer, className].join(" ")}>
      <Box className={styles.headerContainerChild} />
      <Box className={styles.headerContent}>
        <Typography
          className={styles.h2}
          variant="inherit"
          variantMapping={{ inherit: "h2" }}
          sx={{ fontWeight: "700" }}
        >
          Дашборд
        </Typography>
        <Box className={styles.wrapper}>
          <Typography
            className={styles.h3}
            variant="inherit"
            variantMapping={{ inherit: "h3" }}
            sx={{ fontWeight: "400" }}
          >
            Привіт, Анастасія!
          </Typography>
        </Box>
      </Box>
      <Box className={styles.container}>
        <Typography
          className={styles.h32}
          variant="inherit"
          variantMapping={{ inherit: "h3" }}
          sx={{ fontWeight: "400" }}
        >
          26 квітня 2026
        </Typography>
      </Box>
    </header>
  );
};

export default HeaderContainer;
