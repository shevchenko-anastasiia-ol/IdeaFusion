import { FunctionComponent, useState } from "react";
import { Box, Typography, Button } from "@mui/material";
import styles from "./PostColumn.module.css";
import { useNavigate } from "react-router-dom";

export type PostColumnType = {
  className?: string;
};

const PostColumn: FunctionComponent<PostColumnType> = ({ className = "" }) => {
  const navigate = useNavigate();
  const [liked, setLiked] = useState(false);
  const [starred, setStarred] = useState(false);
  const [liked2, setLiked2] = useState(false);
  const [starred2, setStarred2] = useState(false);
  const [liked3, setLiked3] = useState(false);
  const [starred3, setStarred3] = useState(false);

  const heartFilter = "brightness(0) saturate(100%) invert(27%) sepia(96%) saturate(1600%) hue-rotate(320deg)";
  const starFilter = "brightness(0) saturate(100%) invert(75%) sepia(60%) saturate(600%) hue-rotate(20deg)";

  return (
    <Box className={[styles.postColumn, className].join(" ")}>
      {/* Картка 1 */}
      <section className={styles.postCard} onClick={() => navigate("/posts/1")} style={{ cursor: "pointer" }}>
        <Box className={styles.postCardChild} />
        <Box className={styles.previewRegion}>
          <Box className={styles.postPreview} />
          <Typography className={styles.b} variant="inherit" variantMapping={{ inherit: "b" }} sx={{ fontWeight: "700" }}>
            Прев'ю · Редизайн мобільного банкінгу
          </Typography>
        </Box>
        <Box className={styles.authorDetail}>
          <Box className={styles.authorRegion}>
            <Box className={styles.profileSummary}>
              <Box className={styles.authorPicture}>
                <Box className={styles.authorAvatar} />
                <Typography className={styles.h3} variant="inherit" variantMapping={{ inherit: "h3" }} sx={{ fontWeight: "700" }}>ДМ</Typography>
              </Box>
              <Box className={styles.authorDetails}>
                <Box className={styles.authorInformation}>
                  <Box className={styles.authorName}>
                    <Typography className={styles.h32} variant="inherit" variantMapping={{ inherit: "h3" }} sx={{ fontWeight: "600" }}>Дмитро Мельник</Typography>
                  </Box>
                  <div className={styles.div}>2 години тому</div>
                </Box>
              </Box>
              <Box className={styles.postTags}>
                <Box className={styles.rectangleParent}>
                  <Box className={styles.frameChild} />
                  <div className={styles.div2}>Дизайн</div>
                </Box>
              </Box>
            </Box>
            <Box className={styles.postPresentation}>
              <Typography className={styles.h33} variant="inherit" variantMapping={{ inherit: "h3" }} sx={{ fontWeight: "600" }}>Редизайн мобільного банкінгу</Typography>
            </Box>
          </Box>
          <Box className={styles.likeCommentWrapper}>
            <Box className={styles.likeComment}>
              <Box className={styles.likeCommentInner}>
                <Box className={styles.iconHeart1Parent}>
                  <img
                    className={styles.iconHeart1} alt="" src="/icon-heart-1.svg"
                    onClick={(e) => { e.stopPropagation(); setLiked(!liked); }}
                    style={{ cursor: "pointer", filter: liked ? heartFilter : "none" }}
                  />
                  <Box className={styles.likeCount}>
                    <Typography className={styles.likeLayout} variant="inherit" variantMapping={{ inherit: "h3" }} sx={{ fontWeight: "300" }}>24</Typography>
                  </Box>
                </Box>
              </Box>
              <Box className={styles.frameParent}>
                <Box className={styles.likeCommentInner}>
                  <Box className={styles.commentLike}>
                    <img className={styles.iconHeart1} alt="" src="/icon-comment-1.svg" />
                    <Box className={styles.likeCount}>
                      <Typography className={styles.commentNumber} variant="inherit" variantMapping={{ inherit: "h3" }} sx={{ fontWeight: "300" }}>8</Typography>
                    </Box>
                  </Box>
                </Box>
                <Box className={styles.starIconParent}>
                  <img
                    className={styles.starIcon} alt="" src="/Value-Pin.svg"
                    onClick={(e) => { e.stopPropagation(); setStarred(!starred); }}
                    style={{ cursor: "pointer", filter: starred ? starFilter : "none" }}
                  />
                  <Box className={styles.starNumber}>
                    <Typography className={styles.commentNumber} variant="inherit" variantMapping={{ inherit: "h3" }} sx={{ fontWeight: "300" }}>12</Typography>
                  </Box>
                </Box>
              </Box>
            </Box>
          </Box>
        </Box>
      </section>

      {/* Картка 2 */}
      <section className={styles.illustrationCard} onClick={() => navigate("/posts/2")} style={{ cursor: "pointer" }}>
        <Box className={styles.rectangleGroup}>
          <Box className={styles.postCardChild} />
          <Box className={styles.illustrationDetails}>
            <Box className={styles.postPreview} />
            <Typography className={styles.h34} variant="inherit" variantMapping={{ inherit: "h3" }} sx={{ fontWeight: "700" }}>Ілюстрація</Typography>
          </Box>
          <Box className={styles.creatorBlock}>
            <Box className={styles.authorRegion}>
              <Box className={styles.userIdentity}>
                <Box className={styles.creatorProfile}>
                  <Box className={styles.authorPicture}>
                    <Box className={styles.identityAvatarChild} />
                    <Typography className={styles.h3} variant="inherit" variantMapping={{ inherit: "h3" }} sx={{ fontWeight: "700" }}>ДМ</Typography>
                  </Box>
                  <Box className={styles.authorInfo}>
                    <Box className={styles.authorInformation}>
                      <Box className={styles.authorName}>
                        <Typography className={styles.h32} variant="inherit" variantMapping={{ inherit: "h3" }} sx={{ fontWeight: "600" }}>Катерина Гончар</Typography>
                      </Box>
                      <div className={styles.div}>2 дні тому</div>
                    </Box>
                  </Box>
                </Box>
                <Box className={styles.artCategory}>
                  <Button className={styles.artCategoryChild} disableElevation variant="contained"
                    sx={{ textTransform: "none", color: "#a48fff", fontSize: "15", background: "#1e1740", border: "#000 solid 1px", borderRadius: "30px", "&:hover": { background: "#1e1740" }, height: 30 }}>
                    Арт
                  </Button>
                </Box>
              </Box>
              <Box className={styles.wrapper}>
                <Typography className={styles.h33} variant="inherit" variantMapping={{ inherit: "h3" }} sx={{ fontWeight: "600" }}>Серія для книги</Typography>
              </Box>
            </Box>
            <Box className={styles.likeCommentWrapper}>
              <Box className={styles.frameGroup}>
                <Box className={styles.engagePanelWrapper}>
                  <Box className={styles.iconHeart1Parent}>
                    <img
                      className={styles.iconHeart1} alt="" src="/icon-heart-1.svg"
                      onClick={(e) => { e.stopPropagation(); setLiked2(!liked2); }}
                      style={{ cursor: "pointer", filter: liked2 ? heartFilter : "none" }}
                    />
                    <Box className={styles.likeCount}>
                      <Typography className={styles.likeLayout} variant="inherit" variantMapping={{ inherit: "h3" }} sx={{ fontWeight: "300" }}>56</Typography>
                    </Box>
                  </Box>
                </Box>
                <Box className={styles.likeCommentInner}>
                  <Box className={styles.commentLike}>
                    <img className={styles.iconHeart1} alt="" src="/icon-comment-1.svg" />
                    <Box className={styles.likeCount}>
                      <Typography className={styles.commentNumber} variant="inherit" variantMapping={{ inherit: "h3" }} sx={{ fontWeight: "300" }}>11</Typography>
                    </Box>
                  </Box>
                </Box>
                <Box className={styles.starIconParent}>
                  <img
                    className={styles.starIcon} alt="" src="/Value-Pin.svg"
                    onClick={(e) => { e.stopPropagation(); setStarred2(!starred2); }}
                    style={{ cursor: "pointer", filter: starred2 ? starFilter : "none" }}
                  />
                  <Box className={styles.starNumber}>
                    <Typography className={styles.commentNumber} variant="inherit" variantMapping={{ inherit: "h3" }} sx={{ fontWeight: "300" }}>24</Typography>
                  </Box>
                </Box>
              </Box>
            </Box>
          </Box>
        </Box>
      </section>

      {/* Картка 3 */}
      <section className={styles.audioSnippet} onClick={() => navigate("/posts/3")} style={{ cursor: "pointer" }}>
        <Box className={styles.rectangleContainer}>
          <Box className={styles.frameInner} />
          <Box className={styles.previewLabel}>
            <Box className={styles.previewLabelChild} />
            <Typography className={styles.h39} variant="inherit" variantMapping={{ inherit: "h3" }} sx={{ fontWeight: "600" }}>Аудіо прев'ю</Typography>
          </Box>
          <Box className={styles.frameContainer}>
            <Box className={styles.trackAuthorInfoParent}>
              <Box className={styles.trackAuthorInfo}>
                <Box className={styles.artistProfile}>
                  <Box className={styles.frameDiv}>
                    <Box className={styles.authorPicture}>
                      <Box className={styles.initialsCircle} />
                      <Typography className={styles.h310} variant="inherit" variantMapping={{ inherit: "h3" }} sx={{ fontWeight: "700" }}>МС</Typography>
                    </Box>
                    <Box className={styles.authorInfo}>
                      <Box className={styles.authorInformation}>
                        <Box className={styles.authorName}>
                          <Typography className={styles.h32} variant="inherit" variantMapping={{ inherit: "h3" }} sx={{ fontWeight: "600" }}>Максим Сидоренко</Typography>
                        </Box>
                        <div className={styles.div4}>14 годин тому</div>
                      </Box>
                    </Box>
                  </Box>
                  <Box className={styles.artistProfileInner}>
                    <Box className={styles.groupDiv}>
                      <Box className={styles.rectangleDiv} />
                      <div className={styles.div2}>Музика</div>
                    </Box>
                  </Box>
                </Box>
                <Box className={styles.soundtrackDesc}>
                  <Typography className={styles.h33} variant="inherit" variantMapping={{ inherit: "h3" }} sx={{ fontWeight: "600" }}>Саундтрек для інді-гри</Typography>
                </Box>
              </Box>
              <Box className={styles.ratingInteraction}>
                <Box className={styles.reviewMetrics}>
                  <Box className={styles.touchPanel}>
                    <Box className={styles.iconHeart1Parent}>
                      <img
                        className={styles.iconHeart1} alt="" src="/icon-heart-1.svg"
                        onClick={(e) => { e.stopPropagation(); setLiked3(!liked3); }}
                        style={{ cursor: "pointer", filter: liked3 ? heartFilter : "none" }}
                      />
                      <Box className={styles.likeCount}>
                        <Typography className={styles.commentNumber} variant="inherit" variantMapping={{ inherit: "h3" }} sx={{ fontWeight: "300" }}>24</Typography>
                      </Box>
                    </Box>
                  </Box>
                  <Box className={styles.likeCommentInner}>
                    <Box className={styles.commentLike}>
                      <img className={styles.iconHeart1} alt="" src="/icon-comment-1.svg" />
                      <Box className={styles.likeCount}>
                        <Typography className={styles.commentNumber} variant="inherit" variantMapping={{ inherit: "h3" }} sx={{ fontWeight: "300" }}>8</Typography>
                      </Box>
                    </Box>
                  </Box>
                  <Box className={styles.rankingZone}>
                    <img
                      className={styles.starIcon} alt="" src="/Value-Pin.svg"
                      onClick={(e) => { e.stopPropagation(); setStarred3(!starred3); }}
                      style={{ cursor: "pointer", filter: starred3 ? starFilter : "none" }}
                    />
                    <Box className={styles.starNumber}>
                      <Typography className={styles.commentNumber} variant="inherit" variantMapping={{ inherit: "h3" }} sx={{ fontWeight: "300" }}>12</Typography>
                    </Box>
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

export default PostColumn;