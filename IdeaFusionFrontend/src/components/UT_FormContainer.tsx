import { FunctionComponent } from "react";
import styles from "./UT_FormContainer.module.css";

export type FormContainerType = {
  className?: string;
};

const FormContainer: FunctionComponent<FormContainerType> = ({
  className = "",
}) => {
  return (
    <section className={[styles.formContainer, className].join(" ")}>
      <div className={styles.formContainerChild} />
      <div className={styles.formColumn}>
        <div className={styles.wrapper}>
          <h3 className={styles.h3}>Особиста інформація</h3>
        </div>
        <div className={styles.dataFields}>
          <div className={styles.fieldRows}>
            <div className={styles.nameDescriptionRow}>
              <div className={styles.container}>
                <div className={styles.div}>Назва</div>
              </div>
              <input
                className={styles.valueDescriptionRow}
                placeholder="Арт майбутнього"
                type="text"
              />
            </div>
          </div>
          <div className={styles.fieldRows2}>
            <div className={styles.nameDescriptionRow}>
              <div className={styles.frame}>
                <div className={styles.div}>Опис</div>
              </div>
              <div className={styles.rectangleParent}>
                <div className={styles.frameChild} />
                <div className={styles.div3}>
                  Ми — група художників та дизайнерів, що досліджують межі між
                  традиційним мистецтвом і цифровим майбутнім.
                </div>
              </div>
            </div>
          </div>
          <div className={styles.wrapper}>
            <div className={styles.div4}>Специфікація</div>
          </div>
          <input
            className={styles.specDetails}
            placeholder="Графічний дизайн · Illustrator · Арт"
            type="text"
          />
          <div className={styles.categoriesData}>
            <div className={styles.statusCategoryRow}>
              <div className={styles.div4}>Статус</div>
            </div>
            <div className={styles.statusCategoryRow2}>
              <div className={styles.statusCategoryRowChild} />
              <div className={styles.div6}>У пошуку</div>
              <div className={styles.categoryIcons}>
                <img
                  className={styles.categoryIconsChild}
                  alt=""
                  src="/Arrow-10.svg"
                />
              </div>
            </div>
            <div className={styles.statusCategoryRow}>
              <div className={styles.div4}>Каткгорія</div>
            </div>
            <input
              className={styles.specDetails}
              placeholder="Дизайн"
              type="text"
            />
          </div>
        </div>
      </div>
      <div className={styles.actionButtons}>
        <button className={styles.saveButton}>
          <div className={styles.saveButtonChild} />
          <h3 className={styles.h32}>Зберегти зміни</h3>
        </button>
      </div>
    </section>
  );
};

export default FormContainer;
