import { FunctionComponent } from "react";
import styles from "./IF2_FrameComponent2.module.css";

export type FrameComponent2Type = {
  className?: string;
};

// Уніфікована картка з кольоровою плашкою і бейджем «Колаборація»
const ColabCard = ({
  bgColor,
  title,
  tag,
  tagColor,
  tagBg,
  tagBorder,
  author,
}: {
  bgColor: string;
  title: string;
  tag: string;
  tagColor: string;
  tagBg: string;
  tagBorder: string;
  author: string;
}) => (
  <div className={styles.card}>
    <div className={styles.cardPlate} style={{ backgroundColor: bgColor }}>
      <div className={styles.colabBadge}>Колаборація</div>
    </div>
    <div className={styles.cardBody}>
      <div className={styles.cardRow}>
        <b className={styles.cardTitle}>{title}</b>
        <span
          className={styles.cardTag}
          style={{ color: tagColor, background: tagBg, border: tagBorder }}
        >
          {tag}
        </span>
      </div>
      <div className={styles.cardAuthor}>{author}</div>
    </div>
  </div>
);

// Уніфікована картка без «Колаборація»
const SimpleCard = ({
  bgColor,
  title,
  tag,
  tagColor,
  tagBg,
  tagBorder,
  author,
}: {
  bgColor: string;
  title: string;
  tag: string;
  tagColor: string;
  tagBg: string;
  tagBorder: string;
  author: string;
}) => (
  <div className={styles.card}>
    <div className={styles.cardPlate} style={{ backgroundColor: bgColor }} />
    <div className={styles.cardBody}>
      <div className={styles.cardRow}>
        <b className={styles.cardTitle}>{title}</b>
        <span
          className={styles.cardTag}
          style={{ color: tagColor, background: tagBg, border: tagBorder }}
        >
          {tag}
        </span>
      </div>
      <div className={styles.cardAuthor}>{author}</div>
    </div>
  </div>
);

const FrameComponent2: FunctionComponent<FrameComponent2Type> = ({
  className = "",
}) => {
  return (
    <div className={[styles.rectangleParent, className].join(" ")}>
      <div className={styles.frameChild} />

      {/* Заголовок */}
      <div className={styles.wrapper}>
        <h2 className={styles.h2}>Портфоліо</h2>
      </div>

      {/* Фільтри */}
      <div className={styles.filterContainerParent}>
        <div className={styles.rectangleGroup}><h3 className={styles.h3}>Всі</h3></div>
        <div className={styles.rectangleContainer}><h3 className={styles.h32}>Дизайн</h3></div>
        <button className={styles.groupButton}><div className={styles.div}>Музика</div></button>
        <button className={styles.rectangleParent2}><div className={styles.div}>Арт</div></button>
        <button className={styles.rectangleParent3}><div className={styles.div}>Анімація</div></button>
      </div>

      {/* Ряд 1 */}
      <div className={styles.cardRow2}>
        <ColabCard
          bgColor="#3fcca0"
          title="Редизайн мобільного банкінгу"
          tag="Музика"
          tagColor="#3fcca0"
          tagBg="rgba(63,204,160,0.15)"
          tagBorder="1px solid #3fcca0"
          author="Команда «Мандрівники часом»"
        />
        <SimpleCard
          bgColor="#f87c6b"
          title="Форма, що говорить"
          tag="Дизайн"
          tagColor="#a48fff"
          tagBg="rgba(124,92,252,0.15)"
          tagBorder="1px solid #7c5cfc"
          author="Анастасія Шевченко"
        />
      </div>

      {/* Ряд 2 */}
      <div className={styles.cardRow2}>
        <ColabCard
          bgColor="#7c5cfc"
          title="«Кадри, що дихають»"
          tag="Анімація"
          tagColor="#ff6b6b"
          tagBg="rgba(255,107,107,0.15)"
          tagBorder="1px solid #ff6b6b"
          author="Команда «Sound Crafters»"
        />
        <SimpleCard
          bgColor="#3fcca0"
          title="Мій внутрішній світ"
          tag="Арт"
          tagColor="#daa520"
          tagBg="rgba(218,165,32,0.15)"
          tagBorder="1px solid #daa520"
          author="Анастасія Шевченко"
        />
      </div>

      {/* Ряд 3 */}
      <div className={styles.cardRow2}>
        <SimpleCard
          bgColor="#ffb347"
          title="Геометрія настрою"
          tag="Анімація"
          tagColor="#ff6b6b"
          tagBg="rgba(255,107,107,0.15)"
          tagBorder="1px solid #ff6b6b"
          author="Анастасія Шевченко"
        />
        <ColabCard
          bgColor="#5bc8f5"
          title="«Ноти, що відчуваються»"
          tag="Музика"
          tagColor="#3fcca0"
          tagBg="rgba(63,204,160,0.15)"
          tagBorder="1px solid #3fcca0"
          author="Команда «Sound Crafters»"
        />
      </div>

      {/* Ряд 4 */}
      <div className={styles.cardRow2}>
        <SimpleCard
          bgColor="#c756ff"
          title="Те, що не сказати словами"
          tag="Арт"
          tagColor="#daa520"
          tagBg="rgba(218,165,32,0.15)"
          tagBorder="1px solid #daa520"
          author="Анастасія Шевченко"
        />
        <ColabCard
          bgColor="#f5c842"
          title="Сирий настрій"
          tag="Дизайн"
          tagColor="#a48fff"
          tagBg="rgba(124,92,252,0.15)"
          tagBorder="1px solid #7c5cfc"
          author="Команда «Pixel Pulse»"
        />
      </div>
    </div>
  );
};

export default FrameComponent2;