import { FunctionComponent } from "react";
import { useNavigate } from "react-router-dom";
import FrameComponent2 from "../components/FrameComponent2";
import styles from "./PostDetailPage.module.css";

const PostDetailPage: FunctionComponent = () => {
  const navigate = useNavigate();

  const comments = [
    {
      initials: "КГ",
      bg: "#ff6b6b",
      name: "Катерина Гончар",
      time: "2 год. тому",
      text: "Дуже крутий концепт! Особливо подобається підхід до онбордингу — чисто і зрозуміло.",
      hasReply: true,
    },
    {
      initials: "ДМ",
      bg: "#4002aa",
      name: "Дмитро Мельник",
      time: "1 год. тому",
      text: "Дякую! Онбординг — моя улюблена частина в цьому проєкті",
      hasReply: false,
      nested: true,
    },
    {
      initials: "МС",
      bg: "#00c9a7",
      name: "Максим Сидоренко",
      time: "5 год. тому",
      text: "А чи є можливість переглянути прототип? Хотів би оцінити анімації в дії.",
      hasReply: true,
    },
    {
      initials: "ЮБ",
      bg: "#7c5cfc",
      name: "Юлія Бондар",
      time: "15 год. тому",
      text: "Колірна палітра — вогонь 🔥 Як давно працюєш над цим проєктом?",
      hasReply: true,
    },
  ];

  return (
    <div className={styles.page}>
      <FrameComponent2 />

      <div className={styles.content}>
        <button className={styles.back} onClick={() => navigate("/home")}>
          ← Назад до стрічки
        </button>

        <div className={styles.main}>
          {/* Ліва колонка — пост */}
          <div className={styles.postCol}>

            {/* Автор + іконка редагування */}
            <div className={styles.authorRow}>
            <div className={styles.author}>
              <div className={styles.authorAvatar}>Б</div>
              <div>
                <div className={styles.authorName}>Бюрократи</div>
                <div className={styles.authorMeta}>UI/UX дизайн · Figma · Анімації · 3 учасники</div>
              </div>
            </div>
            <button className={styles.editBtn} onClick={() => navigate("/posts/edit")} title="Редагувати">
              <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.8">
                <path d="M11 4H4a2 2 0 0 0-2 2v14a2 2 0 0 0 2 2h14a2 2 0 0 0 2-2v-7"/>
                <path d="M18.5 2.5a2.121 2.121 0 0 1 3 3L12 15l-4 1 1-4 9.5-9.5z"/>
              </svg>
            </button>
          </div>

            <h1 className={styles.title}>Редизайн мобільного банкінгу</h1>
            <p className={styles.tags}>UI/UX дизайн · Figma · Прототипування · Мобільний застосунок</p>

            <div className={styles.cover} />

            <p className={styles.desc}>
              Шукаю розробника для реалізації нового UI для фінтех-додатку.
              Є готові макети у Figma, потрібна допомога з анімаціями та інтеграцією з бекендом.
            </p>
            <p className={styles.desc}>
              Концепція орієнтована на молоду аудиторію: мінімалістичний інтерфейс, темна тема,
              плавні мікроанімації. Пріоритет — онбординг, головний екран та екран транзакцій.
              Усі компоненти вже задизайнені та задокументовані у Figma.
            </p>

            <p className={styles.hashtags}>
              #UI_дизайн #фінтех #мобільний #Figma #анімація #темна_тема
            </p>

            <p className={styles.linksTitle}>Зовнішні посилання</p>
            <div className={styles.links}>
              <a className={styles.link} href="#">figma.com/file/aBcD1234/Banking-Redesign</a>
              <a className={styles.link} href="#">behance.net/dmelnyk/banking-ui</a>
            </div>

            <div className={styles.stats}>
              <span className={styles.stat}>
                <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.8">
                  <path d="M20.84 4.61a5.5 5.5 0 0 0-7.78 0L12 5.67l-1.06-1.06a5.5 5.5 0 0 0-7.78 7.78l1.06 1.06L12 21.23l7.78-7.78 1.06-1.06a5.5 5.5 0 0 0 0-7.78z"/>
                </svg>
                24
              </span>
              <span className={styles.stat}>
                <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.8">
                  <path d="M21 15a2 2 0 0 1-2 2H7l-4 4V5a2 2 0 0 1 2-2h14a2 2 0 0 1 2 2z"/>
                </svg>
                8
              </span>
              <span className={styles.stat}>
                <svg width="18" height="18" viewBox="0 0 24 24" fill="currentColor">
                  <polygon points="12 2 15.09 8.26 22 9.27 17 14.14 18.18 21.02 12 17.77 5.82 21.02 7 14.14 2 9.27 8.91 8.26 12 2"/>
                </svg>
                12
              </span>
            </div>
          </div>

          {/* Права колонка — коментарі */}
          <div className={styles.commentsCol}>
            <p className={styles.commentsTitle}>Коментарі</p>

            <div className={styles.commentsList}>
              {comments.map((c, i) => (
                <div key={i} className={`${styles.comment} ${(c as any).nested ? styles.nested : ""}`}>
                  <div className={styles.commentAvatar} style={{ backgroundColor: c.bg }}>
                    {c.initials}
                  </div>
                  <div className={styles.commentBody}>
                    <div className={styles.commentHeader}>
                      <span className={styles.commentName}>{c.name}</span>
                      <span className={styles.commentTime}>{c.time}</span>
                    </div>
                    <p className={styles.commentText}>{c.text}</p>
                    {c.hasReply && (
                      <button className={styles.replyBtn}>Відповісти</button>
                    )}
                  </div>
                </div>
              ))}
            </div>

            <div className={styles.inputRow}>
              <div className={styles.myAvatar}>АШ</div>
              <div className={styles.inputWrap}>
                <textarea className={styles.input} placeholder="Напишіть коментар..." rows={3} />
                <button className={styles.sendBtn}>Надіслати</button>
              </div>
            </div>
          </div>
        </div>
      </div>
    </div>
  );
};

export default PostDetailPage;