import { FunctionComponent, useState } from "react";
import { Box, Typography, Button } from "@mui/material";
import styles from "./PostColumn.module.css";
import { useNavigate } from "react-router-dom";

type Post = {
  id: number;
  title: string;
  author: string;
  initials: string;
  avatarColor: string;
  time: string;
  category: string;
  previewColor: string;
  likes: number;
  comments: number;
  stars: number;
  previewLabel: string;
};

const ALL_POSTS: Post[] = [
  {
    id: 1,
    title: "Редизайн мобільного банкінгу",
    author: "Дмитро Мельник",
    initials: "ДМ",
    avatarColor: "#4002aa",
    time: "2 години тому",
    category: "Дизайн",
    previewColor: "#2d2d4a",
    previewLabel: "Прев'ю · Редизайн мобільного банкінгу",
    likes: 24,
    comments: 8,
    stars: 12,
  },
  {
    id: 2,
    title: "Саундтрек для інді-гри",
    author: "Максим Сидоренко",
    initials: "МС",
    avatarColor: "#00c9a7",
    time: "14 годин тому",
    category: "Музика",
    previewColor: "#1a3a2a",
    previewLabel: "Аудіо прев'ю",
    likes: 18,
    comments: 5,
    stars: 9,
  },
  {
    id: 3,
    title: "Серія для книги",
    author: "Катерина Гончар",
    initials: "КГ",
    avatarColor: "#ff6b6b",
    time: "2 дні тому",
    category: "Арт",
    previewColor: "#3a1a2a",
    previewLabel: "Ілюстрація",
    likes: 56,
    comments: 11,
    stars: 24,
  },
  {
    id: 4,
    title: "Фотопроєкт «Місто вночі»",
    author: "Юлія Бондар",
    initials: "ЮБ",
    avatarColor: "#ffb347",
    time: "3 дні тому",
    category: "Фото",
    previewColor: "#1a1a3a",
    previewLabel: "Фото прев'ю",
    likes: 34,
    comments: 7,
    stars: 15,
  },
  {
    id: 5,
    title: "3D анімація персонажа",
    author: "Андрій Власенко",
    initials: "АВ",
    avatarColor: "#00c9a7",
    time: "4 дні тому",
    category: "Анімація",
    previewColor: "#1a2a3a",
    previewLabel: "3D прев'ю",
    likes: 41,
    comments: 9,
    stars: 20,
  },
  {
    id: 6,
    title: "Брендинг для стартапу",
    author: "Катерина Гончар",
    initials: "КГ",
    avatarColor: "#ff6b6b",
    time: "5 днів тому",
    category: "Дизайн",
    previewColor: "#2a1a3a",
    previewLabel: "Дизайн прев'ю",
    likes: 29,
    comments: 6,
    stars: 11,
  },
];

export type PostColumnFilteredType = {
  className?: string;
  query?: string;
};

const PostCard: FunctionComponent<{ post: Post }> = ({ post }) => {
  const navigate = useNavigate();
  const [liked, setLiked] = useState(false);
  const [starred, setStarred] = useState(false);

  const heartFilter =
    "brightness(0) saturate(100%) invert(27%) sepia(96%) saturate(1600%) hue-rotate(320deg)";
  const starFilter =
    "brightness(0) saturate(100%) invert(75%) sepia(60%) saturate(600%) hue-rotate(20deg)";

  // Reuse postCard styles from PostColumn.module.css
  return (
    <section
      className={styles.postCard}
      onClick={() => navigate(`/posts/${post.id}`)}
      style={{ cursor: "pointer" }}
    >
      <Box className={styles.postCardChild} />
      <Box className={styles.previewRegion}>
        <Box
          className={styles.postPreview}
          style={{ backgroundColor: post.previewColor }}
        />
        <Typography
          className={styles.b}
          variant="inherit"
          variantMapping={{ inherit: "b" }}
          sx={{ fontWeight: "700" }}
        >
          {post.previewLabel}
        </Typography>
      </Box>
      <Box className={styles.authorDetail}>
        <Box className={styles.authorRegion}>
          <Box className={styles.profileSummary}>
            <Box className={styles.authorPicture}>
              <Box
                className={styles.authorAvatar}
                style={{ backgroundColor: post.avatarColor }}
              />
              <Typography
                className={styles.h3}
                variant="inherit"
                variantMapping={{ inherit: "h3" }}
                sx={{ fontWeight: "700" }}
              >
                {post.initials}
              </Typography>
            </Box>
            <Box className={styles.authorDetails}>
              <Box className={styles.authorInformation}>
                <Box className={styles.authorName}>
                  <Typography
                    className={styles.h32}
                    variant="inherit"
                    variantMapping={{ inherit: "h3" }}
                    sx={{ fontWeight: "600" }}
                  >
                    {post.author}
                  </Typography>
                </Box>
                <div className={styles.div}>{post.time}</div>
              </Box>
            </Box>
            <Box className={styles.postTags}>
              <Box className={styles.rectangleParent}>
                <Box className={styles.frameChild} />
                <div className={styles.div2}>{post.category}</div>
              </Box>
            </Box>
          </Box>
          <Box className={styles.postPresentation}>
            <Typography
              className={styles.h33}
              variant="inherit"
              variantMapping={{ inherit: "h3" }}
              sx={{ fontWeight: "600" }}
            >
              {post.title}
            </Typography>
          </Box>
        </Box>
        <Box className={styles.likeCommentWrapper}>
          <Box className={styles.likeComment}>
            <Box className={styles.likeCommentInner}>
              <Box className={styles.iconHeart1Parent}>
                <img
                  className={styles.iconHeart1}
                  alt=""
                  src="/icon-heart-1.svg"
                  onClick={(e) => {
                    e.stopPropagation();
                    setLiked(!liked);
                  }}
                  style={{
                    cursor: "pointer",
                    filter: liked ? heartFilter : "none",
                  }}
                />
                <Box className={styles.likeCount}>
                  <Typography
                    className={styles.likeLayout}
                    variant="inherit"
                    variantMapping={{ inherit: "h3" }}
                    sx={{ fontWeight: "300" }}
                  >
                    {post.likes}
                  </Typography>
                </Box>
              </Box>
            </Box>
            <Box className={styles.frameParent}>
              <Box className={styles.likeCommentInner}>
                <Box className={styles.commentLike}>
                  <img
                    className={styles.iconHeart1}
                    alt=""
                    src="/icon-comment-1.svg"
                  />
                  <Box className={styles.likeCount}>
                    <Typography
                      className={styles.commentNumber}
                      variant="inherit"
                      variantMapping={{ inherit: "h3" }}
                      sx={{ fontWeight: "300" }}
                    >
                      {post.comments}
                    </Typography>
                  </Box>
                </Box>
              </Box>
              <Box className={styles.starIconParent}>
                <img
                  className={styles.starIcon}
                  alt=""
                  src="/Value-Pin.svg"
                  onClick={(e) => {
                    e.stopPropagation();
                    setStarred(!starred);
                  }}
                  style={{
                    cursor: "pointer",
                    filter: starred ? starFilter : "none",
                  }}
                />
                <Box className={styles.starNumber}>
                  <Typography
                    className={styles.commentNumber}
                    variant="inherit"
                    variantMapping={{ inherit: "h3" }}
                    sx={{ fontWeight: "300" }}
                  >
                    {post.stars}
                  </Typography>
                </Box>
              </Box>
            </Box>
          </Box>
        </Box>
      </Box>
    </section>
  );
};

const PostColumnFiltered: FunctionComponent<PostColumnFilteredType> = ({
  className = "",
  query = "",
}) => {
  const q = query.trim().toLowerCase();
  const filtered = q
    ? ALL_POSTS.filter(
        (p) =>
          p.title.toLowerCase().includes(q) ||
          p.author.toLowerCase().includes(q)
      )
    : ALL_POSTS;

  if (filtered.length === 0) {
    return (
      <Box
        sx={{
          color: "#555",
          fontSize: 15,
          fontFamily: "var(--font-inter)",
          py: "8px",
        }}
      >
        Нічого не знайдено
      </Box>
    );
  }

  return (
    <Box sx={{
      display: "grid",
      gridTemplateColumns: "repeat(3, 401px)",
      gap: "16px",
      alignItems: "start",
    }}>
      {filtered.map((post) => (
        <PostCard key={post.id} post={post} />
      ))}
    </Box>
  );
};

export default PostColumnFiltered;