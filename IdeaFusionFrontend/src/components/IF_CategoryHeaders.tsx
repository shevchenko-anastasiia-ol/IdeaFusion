import { FunctionComponent } from "react";
import { Box, Typography } from "@mui/material";
import styles from "./IF_CategoryHeaders.module.css";

export type CategoryHeadersType = {
  className?: string;
};

const CategoryHeaders: FunctionComponent<CategoryHeadersType> = ({
  className = "",
}) => {
  return (
    <Box className={[styles.categoryHeaders, className].join(" ")}>
      <Box className={styles.categoryHeadersChild} />
      <Box className={styles.categoryItemsParent}>
        <Box className={styles.categoryItems}>
          <Typography
            className={styles.h2}
            variant="inherit"
            variantMapping={{ inherit: "h2" }}
            sx={{ fontWeight: "700" }}
          >
            Бюрократи
          </Typography>
        </Box>
        <Box className={styles.musicCategory}>
          <Box className={styles.rectangleParent}>
            <Box className={styles.frameChild} />
            <div className={styles.div}>Музика</div>
          </Box>
          <Box className={styles.rectangleGroup}>
            <Box className={styles.frameItem} />
            <Typography
              className={styles.h3}
              variant="inherit"
              variantMapping={{ inherit: "h3" }}
              sx={{ fontWeight: "400" }}
            >
              Активна
            </Typography>
          </Box>
        </Box>
      </Box>
      <Box className={styles.uiuxFigmaParent}>
        <Typography
          className={styles.uiux}
          variant="inherit"
          variantMapping={{ inherit: "h3" }}
          sx={{ fontWeight: "400" }}
        >
          UI/UX дизайн · Figma · Анімації
        </Typography>
        <Box className={styles.designerNeeded}>
          <Typography
            className={styles.h32}
            variant="inherit"
            variantMapping={{ inherit: "h3" }}
            sx={{ fontWeight: "400" }}
          >
            5 учасників
          </Typography>
        </Box>
      </Box>
    </Box>
  );
};

export default CategoryHeaders;
