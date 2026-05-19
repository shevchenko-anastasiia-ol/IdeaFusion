import { FunctionComponent } from "react";
import GroupComponent from "./UT_GroupComponent";
import GroupComponent1 from "./UT_GroupComponent1";
import styles from "./UT_FrameComponent3.module.css";

export type FrameComponent3Type = {
  className?: string;
};

const FrameComponent3: FunctionComponent<FrameComponent3Type> = ({
  className = "",
}) => {
  return (
    <section className={[styles.rectangleParent, className].join(" ")}>
      <div className={styles.frameChild} />
      <div className={styles.selection}>
        <div className={styles.filter}>
          <div className={styles.categoryFilters}>
            <h2 className={styles.h2}>Портфоліо</h2>
          </div>
          <div className={styles.works}>
            <div className={styles.worksContainer}>
              <button className={styles.rectangleGroup}>
                <div className={styles.frameItem} />
                <h3 className={styles.h3}>Всі</h3>
              </button>
            </div>
            <button className={styles.rectangleContainer}>
              <div className={styles.frameInner} />
              <div className={styles.div}>Дизайн</div>
            </button>
            <div className={styles.innerFiltersWrapper}>
              <div className={styles.innerFilters}>
                <button className={styles.groupButton}>
                  <div className={styles.rectangleDiv} />
                  <div className={styles.div2}>Музика</div>
                </button>
                <div className={styles.innerFiltersInner}>
                  <button className={styles.rectangleParent2}>
                    <div className={styles.frameChild2} />
                    <div className={styles.div2}>Арт</div>
                  </button>
                </div>
              </div>
            </div>
            <div className={styles.animationFilters}>
              <button className={styles.rectangleContainer}>
                <div className={styles.frameChild3} />
                <div className={styles.div2}>Анімація</div>
              </button>
            </div>
          </div>
        </div>
      </div>
      <div className={styles.portfolioItem}>
        <section className={styles.frameParent}>
          <div className={styles.frameWrapper}>
            <div className={styles.groupDiv}>
              <div className={styles.frameChild4} />
              <div className={styles.parent}>
                <b className={styles.b}>Редизайн мобільного банкінгу</b>
                <div className={styles.rectangleParent4}>
                  <div className={styles.frameChild5} />
                  <div className={styles.div5}>Музика</div>
                </div>
                <div className={styles.frameDiv}>
                  <div className={styles.frameChild6} />
                  <div className={styles.rectangleParent5}>
                    <div className={styles.frameChild7} />
                    <div className={styles.div6}>Колаборація</div>
                  </div>
                </div>
                <div className={styles.div7}>Команда «Мандрівники часом»</div>
                <div className={styles.frameChild8} />
                <div className={styles.artisticLayout} />
                <div className={styles.itemDescriptionWrapper}>
                  <div className={styles.itemDescription}>
                    <div className={styles.bankRedesignDetails}>
                      <b className={styles.b2}>Редизайн мобільного банкінгу</b>
                      <div className={styles.technologyStack}>
                        <div className={styles.rectangleParent6}>
                          <div className={styles.frameChild10} />
                          <div className={styles.div8}>Музика</div>
                        </div>
                      </div>
                    </div>
                    <div className={styles.div9}>Команда «Арт майбутнього»</div>
                  </div>
                </div>
              </div>
            </div>
          </div>
          <GroupComponent prop="Форма, що говорить" prop1="Дизайн" />
          <GroupComponent
            overlapLayerBackgroundColor="#7c5cfc"
            prop="«Звук мого сьогодні»"
            groupDivBackgroundColor="#02372e"
            groupDivBorder="1px solid #00c9a7"
            rectangleDivBackgroundColor="#02372e"
            rectangleDivBorder="1px solid #00c9a7"
            prop1="Музика"
            divColor="#0b9f86"
          />
          <div className={styles.picturePalette}>
            <div className={styles.rectangleParent7}>
              <div className={styles.frameChild4} />
              <div className={styles.group}>
                <b className={styles.b}>Редизайн мобільного банкінгу</b>
                <div className={styles.rectangleParent4}>
                  <div className={styles.frameChild5} />
                  <div className={styles.div5}>Музика</div>
                </div>
                <div className={styles.frameDiv}>
                  <div className={styles.frameChild6} />
                  <div className={styles.rectangleParent5}>
                    <div className={styles.frameChild7} />
                    <div className={styles.div6}>Колаборація</div>
                  </div>
                </div>
                <div className={styles.div7}>Команда «Мандрівники часом»</div>
                <div className={styles.frameChild8} />
                <div className={styles.artworkBase} />
                <div className={styles.feelingDescripton}>
                  <div className={styles.detailsView}>
                    <b className={styles.b4}>«Відчуття на полотні»</b>
                    <div className={styles.mediumBanner}>
                      <div className={styles.brushMedium}>
                        <div className={styles.effectElement} />
                        <div className={styles.div13}>Арт</div>
                      </div>
                    </div>
                  </div>
                  <div className={styles.div9}>Команда «Арт майбутнього»</div>
                </div>
              </div>
            </div>
          </div>
        </section>
        <section className={styles.frameParent}>
          <div className={styles.frameWrapper}>
            <div className={styles.rectangleParent11}>
              <div className={styles.frameChild8} />
              <div className={styles.frameChild17} />
              <div className={styles.itemDescriptionWrapper}>
                <div className={styles.deepBreath}>
                  <div className={styles.container}>
                    <b className={styles.b5}>«Кадри, що дихають»</b>
                    <div className={styles.mediaUsed}>
                      <div className={styles.rectangleParent12}>
                        <div className={styles.frameChild18} />
                        <div className={styles.div8}>Анімація</div>
                      </div>
                    </div>
                  </div>
                  <div className={styles.div9}>Команда «Арт майбутнього»</div>
                </div>
              </div>
            </div>
          </div>
          <GroupComponent1 prop="Мій внутрішній світ" />
          <GroupComponent
            overlapLayerBackgroundColor="#ffb347"
            prop="Сирий настрій"
            groupDivBackgroundColor="rgba(124, 92, 252, 0.23)"
            groupDivBorder="1px solid #7c5cfc"
            rectangleDivBackgroundColor="rgba(124, 92, 252, 0.23)"
            rectangleDivBorder="1px solid #7c5cfc"
            prop1="Дизайн"
            divColor="#7c5cfc"
          />
          <div className={styles.picturePalette}>
            <div className={styles.rectangleParent11}>
              <div className={styles.frameChild8} />
              <div className={styles.frameChild20} />
              <div className={styles.itemDescriptionWrapper}>
                <div className={styles.itemDescription}>
                  <div className={styles.emotivePictures}>
                    <b className={styles.b6}>Коли картинки говорять</b>
                    <div className={styles.artCorner}>
                      <div className={styles.rectangleParent14}>
                        <div className={styles.frameChild18} />
                        <div className={styles.div8}>Анімація</div>
                      </div>
                    </div>
                  </div>
                  <div className={styles.div9}>Команда «Арт майбутнього»</div>
                </div>
              </div>
            </div>
          </div>
        </section>
        <section className={styles.portfolioStack}>
          <div className={styles.assetDisplay}>
            <div className={styles.artisticLibrary}>
              <div className={styles.rectangleParent15}>
                <div className={styles.frameChild8} />
                <div className={styles.backgroundArea} />
                <div className={styles.itemDescriptionWrapper}>
                  <div className={styles.deepBreath}>
                    <div className={styles.container}>
                      <b className={styles.b7}>Геометрія настрою</b>
                      <div className={styles.mediaUsed}>
                        <div className={styles.rectangleParent12}>
                          <div className={styles.frameChild18} />
                          <div className={styles.div8}>Анімація</div>
                        </div>
                      </div>
                    </div>
                    <div className={styles.div9}>Команда «Арт майбутнього»</div>
                  </div>
                </div>
              </div>
            </div>
            <div className={styles.rectangleParent17}>
              <div className={styles.frameChild8} />
              <div className={styles.abstractField}>
                <div className={styles.abstractFieldChild} />
                <div className={styles.emotionalOverlay}>
                  <div className={styles.visualEffects} />
                  <div className={styles.div21}>Колаборація</div>
                </div>
              </div>
              <div className={styles.notationsArea}>
                <div className={styles.perceptiveOverlays}>
                  <div className={styles.container}>
                    <b className={styles.b8}>«Ноти, що відчуваються»</b>
                    <div className={styles.creativeFusion}>
                      <div className={styles.rectangleParent6}>
                        <div className={styles.frameChild10} />
                        <div className={styles.div8}>Музика</div>
                      </div>
                    </div>
                  </div>
                  <div className={styles.div9}>Команда «Арт майбутнього»</div>
                </div>
              </div>
            </div>
            <GroupComponent1
              groupDivFlex="unset"
              groupDivWidth="300.4px"
              rectangleDivBackgroundColor="#c756ff"
              frameDivGap="4px"
              frameDivGap1="31px"
              prop="Те, що не сказати словами"
              bHeight="38px"
              bDisplay="inline-block"
            />
            <div className={styles.conceptualOverlay}>
              <div className={styles.rectangleParent11}>
                <div className={styles.frameChild8} />
                <div className={styles.artisticLayout} />
                <div className={styles.itemDescriptionWrapper}>
                  <div className={styles.deepBreath}>
                    <div className={styles.container}>
                      <b className={styles.b7}>Межа уяви</b>
                      <div className={styles.sonicEmotion}>
                        <div className={styles.rectangleParent6}>
                          <div className={styles.frameChild10} />
                          <div className={styles.div8}>Музика</div>
                        </div>
                      </div>
                    </div>
                    <div className={styles.div9}>Команда «Арт майбутнього»</div>
                  </div>
                </div>
              </div>
            </div>
          </div>
        </section>
      </div>
    </section>
  );
};

export default FrameComponent3;
