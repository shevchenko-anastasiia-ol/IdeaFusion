import { FunctionComponent, useState } from "react";
import { Box, Typography } from "@mui/material";
import { useNavigate } from "react-router-dom";
import FrameComponent from "./FrameComponent";
import styles from "./GroupComponent.module.css";

export type GroupComponentType = {
  className?: string;
};

const GroupComponent: FunctionComponent<GroupComponentType> = ({
  className = "",
}) => {
  const navigate = useNavigate();
  const [liked, setLiked] = useState(false);
const [starred, setStarred] = useState(false);

const heartFilter = "brightness(0) saturate(100%) invert(27%) sepia(96%) saturate(1600%) hue-rotate(320deg)";
const starFilter = "brightness(0) saturate(100%) invert(75%) sepia(60%) saturate(600%) hue-rotate(20deg)";
  return (
    <Box
      className={[styles.rectangleParent, className].join(" ")}
      onClick={() => navigate("/posts/1")}
      style={{ cursor: "pointer" }}
    >
      <Box className={styles.frameChild} />
      <Box className={styles.frameWrapper}>
        <Box className={styles.frameParent}>
          <FrameComponent
            ellipseBoxBackgroundColor="#4002aa"
            prop="ДМ"
            typographyLeft="10px"
            typographyMinWidth="44px"
            prop1="Дмитро Мельник"
            react="2 години тому"
          />
          <Box className={styles.rectangleGroup}>
            <Box className={styles.frameItem} />
            <Typography
              className={styles.h3}
              variant="inherit"
              variantMapping={{ inherit: "h3" }}
              sx={{ fontWeight: "400" }}
            >
              Дизайн
            </Typography>
          </Box>
        </Box>
      </Box>
      <Box className={styles.rectangleContainer}>
        <Box className={styles.frameInner} />
        <Typography
          className={styles.h32}
          variant="inherit"
          variantMapping={{ inherit: "h3" }}
          sx={{ fontWeight: "700" }}
        >
          Прев'ю · Редизайн мобільного банкінгу
        </Typography>
      </Box>
      <section className={styles.frameContainer}>
        <Box className={styles.frameGroup}>
          <Box className={styles.frameDiv}>
            <Box className={styles.parent}>
              <Typography
                className={styles.h33}
                variant="inherit"
                variantMapping={{ inherit: "h3" }}
                sx={{ fontWeight: "600" }}
              >
                Редизайн мобільного банкінгу
              </Typography>
              <Box className={styles.titleDividerWrapper}>
                <Box className={styles.titleDivider} />
              </Box>
            </Box>
            <div className={styles.ui}>
              Шукаю розробника для реалізації нового UI для фінтех-додатку. Є
              готові макети у Figma, потрібна допомога з анімаціями та
              інтеграцією.
            </div>
          </Box>
          <Box className={styles.frameWrapper2}>
            <Box className={styles.frameParent2}>
              <Box className={styles.frameWrapper3}>
                <Box className={styles.iconHeart1Parent}>
                <img
                  className={styles.iconHeart1}
                  alt=""
                  src="/icon-heart-1.svg"
                  onClick={(e) => { e.stopPropagation(); setLiked(!liked); }}
                  style={{ cursor: "pointer", filter: liked ? heartFilter : "none" }}
                />
                  <Box className={styles.likeCountWrapper}>
                    <Typography
                      className={styles.likeCount}
                      variant="inherit"
                      variantMapping={{ inherit: "h3" }}
                      sx={{ fontWeight: "300" }}
                    >
                      24
                    </Typography>
                  </Box>
                </Box>
              </Box>
              <Box className={styles.frameParent3}>
                <Box className={styles.frameWrapper3}>
                  <Box className={styles.iconComment1Parent}>
                    <img
                      className={styles.iconHeart1}
                      alt=""
                      src="/icon-comment-1.svg"
                    />
                    <Box className={styles.likeCountWrapper}>
                      <Typography
                        className={styles.commentCount}
                        variant="inherit"
                        variantMapping={{ inherit: "h3" }}
                        sx={{ fontWeight: "300" }}
                      >
                        8
                      </Typography>
                    </Box>
                  </Box>
                </Box>
                <Box className={styles.starIconParent}>
                <img
                  className={styles.starIcon}
                  alt=""
                  src="/Star-Icon.svg"
                  onClick={(e) => { e.stopPropagation(); setStarred(!starred); }}
                  style={{ cursor: "pointer", filter: starred ? starFilter : "none" }}
                />
                  <Box className={styles.label}>
                    <Typography
                      className={styles.commentCount}
                      variant="inherit"
                      variantMapping={{ inherit: "h3" }}
                      sx={{ fontWeight: "300" }}
                    >
                      12
                    </Typography>
                  </Box>
                </Box>
              </Box>
            </Box>
          </Box>
        </Box>
      </section>
    </Box>
  );
};

export default GroupComponent;