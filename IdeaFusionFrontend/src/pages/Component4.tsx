import { FunctionComponent, useEffect, useState } from "react";
import { Box } from "@mui/material";
import { useNavigate } from "react-router-dom";
import FrameComponent2 from "../components/FrameComponent2";
import Moderation from "../components/Moderation";
import styles from "./Component4.module.css";
import { savedPostsApi } from "../api/posts";
import { useAuth } from "../context/AuthContext";
import { timeAgo } from "../api/types";

type SavedPostItem = {
  savedPostId: number;
  userId: number;
  savedAt: string;
  postId: number;
  postTitle: string;
  postMediaUrls: string[];
};

const PREVIEW_COLORS = ["#1e1740","#02372e","#1a1a2e","#2d2d4a","#1a3a2a","#2a1a3a"];
function colorFor(id: number) { return PREVIEW_COLORS[id % PREVIEW_COLORS.length]; }

function getMediaType(url: string): 'image' | 'video' | 'audio' | 'pdf' {
  const ext = url.split('?')[0].split('.').pop()?.toLowerCase() ?? '';
  if (['mp4', 'webm', 'mov', 'avi', 'mkv'].includes(ext)) return 'video';
  if (['mp3', 'wav', 'ogg', 'aac', 'm4a', 'flac'].includes(ext)) return 'audio';
  if (ext === 'pdf') return 'pdf';
  return 'image';
}

const PAGE_SIZE = 15;

const Component4: FunctionComponent = () => {
  const navigate = useNavigate();
  const { contentUserId, loading: authLoading } = useAuth();
  const [posts, setPosts] = useState<SavedPostItem[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [page, setPage] = useState(1);

  useEffect(() => {
    if (authLoading) return;           // wait for auth to resolve
    if (!contentUserId) { setLoading(false); return; }
    setLoading(true);
    setError(null);
    savedPostsApi.getByUser(contentUserId)
      .then(data => { setPosts(data); setPage(1); })
      .catch((e: Error) => setError(e.message ?? 'Помилка завантаження'))
      .finally(() => setLoading(false));
  }, [contentUserId, authLoading]);

  const totalPages = Math.max(1, Math.ceil(posts.length / PAGE_SIZE));
  const pagePosts = posts.slice((page - 1) * PAGE_SIZE, page * PAGE_SIZE);

  return (
    <Box className={styles.div}>
      <main className={styles.main}>
        <FrameComponent2 />

        <section className={styles.content}>
          <div className={styles.headerRow}>
            <span className={styles.pageTitle}>Збережене</span>
            <span className={styles.count}>
              {loading ? "Завантаження..." : `${posts.length} публікацій`}
            </span>
          </div>

          <div className={styles.grid}>
            {error && (
              <p style={{ color: "#ff6b6b", padding: "24px 0", gridColumn: "1 / -1" }}>{error}</p>
            )}
            {!loading && !error && posts.length === 0 && (
              <p style={{ color: "#777", padding: "24px 0", gridColumn: "1 / -1" }}>
                Ви ще нічого не зберегли
              </p>
            )}
            {pagePosts.map((item) => (
              <div
                key={item.savedPostId}
                className={styles.card}
                onClick={() => navigate(`/posts/${item.postId}`)}
              >
                <div
                  className={styles.cardThumb}
                  style={{ backgroundColor: colorFor(item.postId) }}
                >
                  {item.postMediaUrls.length > 0 ? (() => {
                    const imageUrl = item.postMediaUrls.find(u => getMediaType(u) === 'image');
                    if (imageUrl) return (
                      <img
                        src={imageUrl}
                        alt={item.postTitle}
                        style={{ width: "100%", height: "100%", objectFit: "cover", display: "block" }}
                      />
                    );
                    const url = item.postMediaUrls[0];
                    const type = getMediaType(url);
                    if (type === 'video') return (
                      <video
                        src={url}
                        muted
                        preload="metadata"
                        style={{ width: "100%", height: "100%", objectFit: "cover", display: "block" }}
                      />
                    );
                    if (type === 'audio') return <span style={{ fontSize: 48 }}>🎵</span>;
                    if (type === 'pdf') return <span style={{ fontSize: 48 }}>📄</span>;
                    return null;
                  })() : (
                    <span className={styles.cardThumbText}>{item.postTitle}</span>
                  )}
                </div>

                <div className={styles.cardInfo}>
                  <span className={styles.cardTitle}>{item.postTitle}</span>
                  <div className={styles.stats}>
                    <span className={styles.stat}>
                      <svg width="14" height="14" viewBox="0 0 24 24" fill="currentColor">
                        <polygon points="12 2 15.09 8.26 22 9.27 17 14.14 18.18 21.02 12 17.77 5.82 21.02 7 14.14 2 9.27 8.91 8.26 12 2" />
                      </svg>
                      Збережено {timeAgo(item.savedAt)}
                    </span>
                  </div>
                </div>
              </div>
            ))}
          </div>

          {!loading && !error && posts.length > PAGE_SIZE && (
            <div className={styles.pagination}>
              <button
                className={styles.pageBtn}
                disabled={page === 1}
                onClick={() => setPage(p => p - 1)}
              >← Назад</button>
              <span className={styles.pageNum}>Сторінка {page} з {totalPages}</span>
              <button
                className={styles.pageBtn}
                disabled={page >= totalPages}
                onClick={() => setPage(p => p + 1)}
              >Вперед →</button>
            </div>
          )}
        </section>
      </main>

      <Moderation
        moderationMarginTop="unset"
        moderationPosition="relative"
        moderationTop="unset"
        moderationLeft="125px"
      />
    </Box>
  );
};

export default Component4;