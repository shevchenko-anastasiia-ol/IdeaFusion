import { FunctionComponent } from "react";
import { useNavigate } from "react-router-dom";
import FrameComponent2 from "../components/FrameComponent2";
import Moderation from "../components/Moderation";
import styles from "./TeamPublicPage.module.css";

const TeamPublicPage: FunctionComponent = () => {
  const navigate = useNavigate();

  const members = [
    { initials: "ДМ", bg: "#7c5cfc", name: "Дмитро Мельник", role: "Лідер", isLeader: true },
    { initials: "ЮБ", bg: "#ff6b6b", name: "Юлія Бондар", role: "", isLeader: false },
    { initials: "АВ", bg: "#3fcca0", name: "Андрій Власенко", role: "", isLeader: false },
    { initials: "КГ", bg: "#ffb347", name: "Катерина Гончар", role: "", isLeader: false },
  ];

  const portfolio = [
    { bg: "#3fcca0", title: "Редизайн мобільного банкінгу", author: "Команда «Мандрівники часом»", tag: "Колаборація", tagColor: "#7c5cfc" },
    { bg: "#f87c6b", title: "Форма, що говорить", author: "Анастасія Шевченко", tag: "Дизайн", tagColor: "#a48fff" },
    { bg: "#7c5cfc", title: "«Кадри, що дихають»", author: "Команда «Sound Crafters»", tag: "Колаборація", tagColor: "#7c5cfc" },
    { bg: "#3fcca0", title: "Мій внутрішній світ", author: "Анастасія Шевченко", tag: "Арт", tagColor: "#daa520" },
    { bg: "#ffb347", title: "Геометрія настрою", author: "Анастасія Шевченко", tag: "Анімація", tagColor: "#ff6b6b" },
    { bg: "#5bc8f5", title: "«Ноти, що відчуваються»", author: "Команда «Sound Crafters»", tag: "Колаборація", tagColor: "#7c5cfc" },
    { bg: "#c756ff", title: "Те, що не сказати словами", author: "Анастасія Шевченко", tag: "Арт", tagColor: "#daa520" },
    { bg: "#f5c842", title: "Сирий настрій", author: "Команда «Pixel Pulse»", tag: "Колаборація", tagColor: "#7c5cfc" },
  ];

  return (
    <div className={styles.page}>
      <FrameComponent2 />

      <div className={styles.content}>
        {/* Хедер профілю */}
        <div className={styles.profileHeader}>
          <div className={styles.profileAvatar}>АМ</div>
          <div className={styles.profileInfo}>
            <h1 className={styles.profileName}>Арт майбутнього</h1>
            <p className={styles.profileSkills}>Illustrator · Procreate · Blender · Арт · Ілюстрація</p>
          </div>
          <button className={styles.applyBtn} onClick={() => navigate("/team/join-request")}>
            Подати заявку
          </button>
        </div>

        <div className={styles.main}>
          {/* Ліва колонка — портфоліо */}
          <div className={styles.leftCol}>
            <div className={styles.sectionHeader}>
              <h2 className={styles.sectionTitle}>Портфоліо</h2>
              <div className={styles.filters}>
                {["Всі", "Дизайн", "Музика", "Арт", "Анімація"].map((f, i) => (
                  <button key={f} className={`${styles.filterBtn} ${i === 0 ? styles.filterActive : ""}`}>
                    {f}
                  </button>
                ))}
              </div>
            </div>

            <div className={styles.portfolioGrid}>
              {portfolio.map((p, i) => (
                <div key={i} className={styles.portfolioCard} onClick={() => navigate("/posts/1")}>
                  <div className={styles.cardPlate} style={{ backgroundColor: p.bg }}>
                    {p.tag === "Колаборація" && (
                      <span className={styles.colabTag}>Колаборація</span>
                    )}
                  </div>
                  <div className={styles.cardBody}>
                    <div className={styles.cardRow}>
                      <span className={styles.cardTitle}>{p.title}</span>
                      {p.tag !== "Колаборація" && (
                        <span className={styles.cardTag} style={{ color: p.tagColor, border: `1px solid ${p.tagColor}`, background: `${p.tagColor}22` }}>
                          {p.tag}
                        </span>
                      )}
                    </div>
                    <span className={styles.cardAuthor}>{p.author}</span>
                  </div>
                </div>
              ))}
            </div>
          </div>

          {/* Права колонка */}
          <div className={styles.rightCol}>
            {/* Про команду */}
            <div className={styles.card}>
              <h3 className={styles.cardSectionTitle}>Про команду</h3>
              <p className={styles.aboutText}>
                Ми — група художників та дизайнерів, що досліджують межі між традиційним
                мистецтвом і цифровим майбутнім. Шукаємо графічного дизайнера для спільних проєктів.
              </p>

              <div className={styles.infoRow}>
                <span className={styles.infoLabel}>Опис</span>
                <span className={styles.infoValue}>Ми — група художників та дизайнерів...</span>
              </div>
              <div className={styles.infoRow}>
                <span className={styles.infoLabel}>Спеціальність</span>
                <span className={styles.infoValue}>Графічний дизайн · Illustrator · Арт</span>
              </div>
              <div className={styles.infoRow}>
                <span className={styles.infoLabel}>Статус</span>
                <span className={styles.statusBadge}>У пошуку</span>
              </div>
              <div className={styles.infoRow}>
                <span className={styles.infoLabel}>Категорія</span>
                <span className={styles.infoValue}>Дизайн</span>
              </div>
            </div>

            {/* Контакти лідера */}
            <div className={styles.card}>
              <h3 className={styles.cardSectionTitle}>Контакти лідера</h3>
              <div className={styles.infoRow}>
                <span className={styles.infoLabel}>Повне ім'я</span>
                <span className={styles.infoValue}>Дмитро Мельник</span>
              </div>
              <div className={styles.infoRow}>
                <span className={styles.infoLabel}>Email</span>
                <span className={styles.infoValue}>dmytro.melnuk@gmail.com</span>
              </div>
            </div>

            {/* Учасники */}
            <div className={styles.card}>
              <h3 className={styles.cardSectionTitle}>Учасники команди</h3>
              <div className={styles.membersList}>
                {members.map((m, i) => (
                  <div key={i} className={styles.memberRow} onClick={() => navigate("/profile")}>
                    <div className={styles.memberAvatar} style={{ backgroundColor: m.bg }}>{m.initials}</div>
                    <span className={styles.memberName}>{m.name}</span>
                    {m.isLeader && <span className={styles.leaderBadge}>Лідер</span>}
                  </div>
                ))}
                <button className={styles.addMemberBtn}>
                  + Потрібен графічний дизайнер
                </button>
              </div>
            </div>
          </div>
        </div>
        <Moderation moderationPosition="relative" moderationTop="unset" moderationLeft="unset" />
      </div>
    </div>
  );
};

export default TeamPublicPage;