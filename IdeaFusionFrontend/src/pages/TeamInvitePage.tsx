import { FunctionComponent } from "react";
import FrameComponent1 from "../components/UT3_FrameComponent1";
import FrameComponent2 from "../components/UT3_FrameComponent2";
import GroupComponent1 from "../components/UT3_GroupComponent1";
import FrameComponent3 from "../components/UT3_FrameComponent3";
import GroupComponent2 from "../components/UT3_GroupComponent2";
import styles from "./TeamInvitePage.module.css";

const Component1: FunctionComponent = () => {
  return (
    <div className={styles.div}>
      <div className={styles.child} />
      <b className={styles.b}>Створення нової команди</b>
      <div className={styles.item} />
      <img className={styles.inner} alt="" src="/Group-2.svg" />
      <img className={styles.groupIcon} alt="" src="/Group-3.svg" />
      <img className={styles.child2} alt="" src="/Group-4.svg" />
      <img className={styles.starIcon} alt="" src="/Star-Icon.svg" />
      <div className={styles.rectangleDiv} />
      <div className={styles.rectangleDiv} />
      <img className={styles.rectangleIcon} alt="" src="/Rectangle-8.svg" />
      <div className={styles.rectangleDiv} />
      <button className={styles.button}>Скасувати</button>
      <img className={styles.child5} alt="" src="/Group-12@2x.png" />
      <div className={styles.child6} />
      <main className={styles.frameParent}>
        <FrameComponent1 prop="Запрошення до команди" prop1="Назад" />
        <section className={styles.frameGroup}>
          <div className={styles.frameContainer}>
            <div className={styles.rectangleParent}>
              <div className={styles.frameChild} />
              <div className={styles.content}>
                <div className={styles.tabsParent}>
                  <div className={styles.tabs}>
                    <button className={styles.rectangleGroup}>
                      <div className={styles.frameItem} />
                      <div className={styles.div2}>Всі</div>
                    </button>
                  </div>
                  <button className={styles.rectangleContainer}>
                    <div className={styles.frameInner} />
                    <h3 className={styles.h3}>Нові</h3>
                  </button>
                  <div className={styles.frameWrapper}>
                    <div className={styles.groupDiv}>
                      <div className={styles.frameChild2} />
                      <button className={styles.button2}>Прийняті</button>
                    </div>
                  </div>
                  <div className={styles.frameDiv}>
                    <div className={styles.rectangleParent2}>
                      <div className={styles.frameChild3} />
                      <button className={styles.button3}>Відхилені</button>
                    </div>
                  </div>
                </div>
              </div>
              <div className={styles.invitesList}>
                <section className={styles.groupSection}>
                  <div className={styles.frameChild4} />
                  <div className={styles.inviteContainer}>
                    <div className={styles.inviteItem}>
                      <div className={styles.inviteItemChild} />
                      <div className={styles.frameParent2}>
                        <FrameComponent2
                          userAvatarTwoBackgroundColor="#4002aa"
                          prop="2 години тому"
                          prop1="ДМ"
                          prop2="Дмитро Мельник"
                        />
                        <div className={styles.newBadgeContainerWrapper}>
                          <div className={styles.newBadgeContainer}>
                            <div className={styles.newBadgeContainerChild} />
                            <button className={styles.button4}>Нове</button>
                          </div>
                        </div>
                      </div>
                    </div>
                    <div className={styles.requestMessage}>
                      <div className={styles.div3}>
                        Хочу приєднатись допроєкту банкінгу як розробник
                      </div>
                    </div>
                  </div>
                  <div className={styles.actionsContainerWrapper}>
                    <div className={styles.actionsContainer}>
                      <div className={styles.actionsApprove}>
                        <div className={styles.actionsApproveChild} />
                        <button className={styles.button4}>Прийняти</button>
                      </div>
                      <div className={styles.wrapper}>
                        <div className={styles.div4}>Відхилити</div>
                      </div>
                    </div>
                  </div>
                </section>
                <section className={styles.invitesListInner}>
                  <div className={styles.rectangleParent3}>
                    <div className={styles.frameChild5} />
                    <div className={styles.inviteContainerTwo}>
                      <div className={styles.inviteItemTwo}>
                        <FrameComponent2
                          prop="5 годин тому"
                          prop1="ЮБ"
                          prop2="Юлія Бондар"
                        />
                        <div className={styles.requestMessageTwo}>
                          <div className={styles.ost}>
                            Можу допомогти з OST,маю досвід у gamedev
                          </div>
                        </div>
                      </div>
                    </div>
                    <div className={styles.actionsApproveTwoParent}>
                      <div className={styles.actionsApproveTwo}>
                        <div className={styles.actionsApproveChild} />
                        <button className={styles.button4}>Прийняти</button>
                      </div>
                      <div className={styles.container}>
                        <div className={styles.div4}>Відхилити</div>
                      </div>
                    </div>
                  </div>
                </section>
                <section className={styles.frameSection}>
                  <GroupComponent1
                    prop="АВ"
                    prop1="Андрій Власенко"
                    prop2="12 годин тому"
                    prop3="Відхилене"
                    prop4="Відхилено"
                  />
                  <GroupComponent1
                    groupDivAlignSelf="unset"
                    groupDivPadding="12px 9px 21px 10px"
                    groupDivGap="35px"
                    groupDivWidth="879px"
                    prop="ММ"
                    h3Left="9px"
                    h3MinWidth="45px"
                    prop1="Микола Мельник"
                    prop2="17 годин тому"
                    frameDivPadding="15px 0px 0px"
                    careerBackgroundColor="rgba(63, 204, 160, 0.45)"
                    careerBorder="1px solid #3fcca0"
                    prop3="Прийнято"
                    combinedProposalWidth="737px"
                    prop4="Прийнято в команду"
                    divColor="#00c9a7"
                  />
                </section>
              </div>
            </div>
            <FrameComponent3 />
          </div>
          <GroupComponent2 />
        </section>
      </main>
    </div>
  );
};

export default Component1;
