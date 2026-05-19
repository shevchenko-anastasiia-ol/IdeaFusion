import { FunctionComponent } from "react";
import GroupComponent from "./C3_GroupComponent";
import styles from "./C3_CategoryInner.module.css";

export type CategoryInnerType = {
  className?: string;
};

const CategoryInner: FunctionComponent<CategoryInnerType> = ({
  className = "",
}) => {
  return (
    <div className={[styles.categoryInner, className].join(" ")}>
      <div className={styles.categoryInnerChild} />
      <div className={styles.categoryLevel}>
        <div className={styles.categorySelection}>
          <div className={styles.categoryPanel}>
            <button className={styles.rectangleParent}>
              <div className={styles.frameChild} />
              <div className={styles.div}>Всі</div>
            </button>
          </div>
          <div className={styles.rectangleGroup}>
            <div className={styles.frameItem} />
            <div className={styles.div2}>Нові</div>
          </div>
          <div className={styles.categorySelectionInner}>
            <div className={styles.rectangleContainer}>
              <div className={styles.frameInner} />
              <div className={styles.div3}>Прийняті</div>
            </div>
          </div>
          <div className={styles.categorySelectionChild}>
            <div className={styles.groupDiv}>
              <div className={styles.rectangleDiv} />
              <div className={styles.div2}>Відхилені</div>
            </div>
          </div>
        </div>
      </div>
      <div className={styles.frameParent}>
        <section className={styles.groupSection}>
          <div className={styles.frameChild2} />
          <div className={styles.requestEntry}>
            <div className={styles.entryContainer}>
              <div className={styles.entryContainerChild} />
              <div className={styles.frameGroup}>
                <div className={styles.frameContainer}>
                  <div className={styles.ellipseParent}>
                    <div className={styles.ellipseDiv} />
                    <h3 className={styles.h3}>ДМ</h3>
                  </div>
                  <div className={styles.frameWrapper}>
                    <div className={styles.frameDiv}>
                      <div className={styles.wrapper}>
                        <h3 className={styles.h32}>Дмитро Мельник</h3>
                      </div>
                      <div className={styles.div5}>2 години тому</div>
                    </div>
                  </div>
                </div>
                <div className={styles.frameWrapper2}>
                  <div className={styles.rectangleParent2}>
                    <div className={styles.frameChild3} />
                    <button className={styles.button}>Нове</button>
                  </div>
                </div>
              </div>
            </div>
            <div className={styles.requestMessage}>
              <div className={styles.div6}>
                Хочу приєднатись допроєкту банкінгу як розробник
              </div>
            </div>
          </div>
          <div className={styles.buttonContainerWrapper}>
            <div className={styles.buttonContainer}>
              <div className={styles.acceptButton}>
                <div className={styles.acceptButtonChild} />
                <button className={styles.button}>Прийняти</button>
              </div>
              <div className={styles.rejectButton}>
                <div className={styles.div7}>Відхилити</div>
              </div>
            </div>
          </div>
        </section>
        <section className={styles.frameSection}>
          <div className={styles.rectangleParent3}>
            <div className={styles.frameChild4} />
            <div className={styles.requestEntryTwo}>
              <div className={styles.entryContainerTwo}>
                <div className={styles.frameParent2}>
                  <div className={styles.ellipseParent}>
                    <div className={styles.frameChild5} />
                    <h3 className={styles.h33}>ЮБ</h3>
                  </div>
                  <div className={styles.frameWrapper}>
                    <div className={styles.frameDiv}>
                      <div className={styles.wrapper}>
                        <h3 className={styles.h32}>Юлія Бондар</h3>
                      </div>
                      <div className={styles.div5}>5 годин тому</div>
                    </div>
                  </div>
                </div>
                <div className={styles.requestMessageTwo}>
                  <div className={styles.ost}>
                    Можу допомогти з OST,маю досвід у gamedev
                  </div>
                </div>
              </div>
            </div>
            <div className={styles.acceptButtonTwoParent}>
              <div className={styles.acceptButtonTwo}>
                <div className={styles.acceptButtonChild} />
                <button className={styles.button}>Прийняти</button>
              </div>
              <div className={styles.rejectButtonTwo}>
                <div className={styles.div7}>Відхилити</div>
              </div>
            </div>
          </div>
        </section>
        <section className={styles.frameParent4}>
          <GroupComponent
            prop="АВ"
            prop1="Андрій Власенко"
            prop2="12 годин тому"
            prop3="Відхилене"
            prop4="Відхилено"
          />
          <GroupComponent
            prop="ММ"
            prop1="Микола Мельник"
            prop2="17 годин тому"
            prop3="Прийнято"
            prop4="Прийнято в команду"
            groupDivAlignSelf="unset"
            groupDivPadding="12px 9px 21px 10px"
            groupDivGap="35px"
            groupDivWidth="879px"
            h3Left="9px"
            h3MinWidth="45px"
            frameDivPadding="15px 0px 0px"
            statusBackgroundBackgroundColor="rgba(63, 204, 160, 0.45)"
            statusBackgroundBorder="1px solid #3fcca0"
            frameDivWidth="737px"
            divColor="#00c9a7"
          />
        </section>
      </div>
    </div>
  );
};

export default CategoryInner;
