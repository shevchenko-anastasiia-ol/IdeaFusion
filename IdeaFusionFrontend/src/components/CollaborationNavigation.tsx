import { FunctionComponent } from "react";
import { Box, Typography } from "@mui/material";
import { useNavigate } from "react-router-dom";
import styles from "./CollaborationNavigation.module.css";

export type CollaborationNavigationType = {
  className?: string;
};

const CollaborationNavigation: FunctionComponent<
  CollaborationNavigationType
> = ({ className = "" }) => {
  const navigate = useNavigate();
  return (
    <section className={[styles.collaborationNavigation, className].join(" ")}>
      <Box className={styles.collaborationNavigationChild} />
      <Box className={styles.wrapper}>
        <Typography
          className={styles.h2}
          variant="inherit"
          variantMapping={{ inherit: "h2" }}
          sx={{ fontWeight: "700" }}
        >
          Колаборації
        </Typography>
      </Box>
      <button className={styles.rectangleParent} onClick={() => navigate("/team/new")}>
        <Box className={styles.frameChild} />
        <Box className={styles.teamLabel}>
          <div className={styles.div}>+</div>
        </Box>
        <Box className={styles.newTeamButton}>
          <div className={styles.div2}>
            Нова <br />
            команда
          </div>
        </Box>
      </button>
    </section>
  );
};

export default CollaborationNavigation;
