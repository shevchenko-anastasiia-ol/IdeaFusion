import { FunctionComponent } from "react";
import { Box, Typography } from "@mui/material";
import styles from "./FilterRegion.module.css";

export type FilterRegionType = {
  className?: string;
};

const FilterRegion: FunctionComponent<FilterRegionType> = ({
  className = "",
}) => {
  return (
    <section className={[styles.filterRegion, className].join(" ")}>
      <Box className={styles.filterRegionChild} />
      <Box className={styles.filterRegionInner}>
        <img
          className={styles.frameChild}
          loading="lazy"
          alt=""
          src="/Group-1@2x.png"
        />
      </Box>
      <Box className={styles.sortSelection}>
        <Box className={styles.sortOptions}>
          <Box className={styles.ordering}>
            <img className={styles.orderingChild} alt="" src="/Group-2.svg" />
          </Box>
          <Box className={styles.viewSetting}>
            <Box className={styles.displayStyle}>
              <Box className={styles.displayStyleChild} />
              <img
                className={styles.displayStyleItem}
                alt=""
                src="/Group-3.svg"
              />
            </Box>
            <Box className={styles.orderStyle}>
              <img
                className={styles.orderStyleChild}
                alt=""
                src="/Group-41.svg"
              />
            </Box>
          </Box>
        </Box>
        <Box className={styles.ratingStars}>
          <img className={styles.buttonsettingsIcon} alt="" src="/Star-1.svg" />
        </Box>
        <Box className={styles.orderStyle}>
          <Box className={styles.ustawieniaParent}>
            <Box className={styles.ustawienia} />
            <Box className={styles.frameItem} />
            <img className={styles.frameInner} alt="" src="/Rectangle-8.svg" />
            <Box className={styles.konto} />
          </Box>
        </Box>
      </Box>
      <Box className={styles.profilePreview}>
        <Box className={styles.profilePreviewChild} />
        <Typography
          className={styles.h3}
          variant="inherit"
          variantMapping={{ inherit: "h3" }}
          sx={{ fontWeight: "700" }}
        >
          АШ
        </Typography>
      </Box>
    </section>
  );
};

export default FilterRegion;
