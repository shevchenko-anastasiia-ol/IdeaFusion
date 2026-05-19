import { FunctionComponent } from "react";
import styles from "./UT3_GroupComponent.module.css";

export type GroupComponentType = {
  className?: string;
};

const GroupComponent: FunctionComponent<GroupComponentType> = ({
  className = "",
}) => {
  return (
    <section className={[styles.rectangleParent, className].join(" ")}>
      <div className={styles.frameChild} />
      <div className={styles.lineParent}>
        <div className={styles.frameItem} />
        <div className={styles.div}>Служба підтримки</div>
        <b className={styles.ideafusion}>
          <span>Idea</span>
          <span className={styles.fusion}>Fusion</span>
        </b>
        <img className={styles.vectorIcon} alt="" src="/Brand-Icon.svg" />
        <div className={styles.creativeCollaborationPlatfor}>
          CREATIVE COLLABORATION PLATFORM
        </div>
        <img className={styles.frameInner} alt="" src="/Group-131@2x.png" />
        <div className={styles.div}>З питань модерації звертайтесь сюди:</div>
        <div className={styles.instagramAnasteishen}>
          moderators@ideafusion.com
          <br />
          +380 66 757 4343
        </div>
        <div className={styles.div}>Власник платформи</div>
        <div className={styles.instagramAnasteishen}>
          {`Шевченко Анастасія Олександрівна`}
          <br />
          {`Instagram: _.anasteishen_ `}
        </div>
        <div className={styles.instagramAnasteishen}>
          {`support@ideafusion.com`}
          <br />
          {`+380 66 444 3322 `}
        </div>
        <div className={styles.frameItem} />
        <div className={styles.frameItem} />
        <div className={styles.rectangleDiv} />
        <div className={styles.moderationBlock}>
          <div className={styles.frameParent}>
            <div className={styles.frameGroup}>
              <div className={styles.parent}>
                <div className={styles.div4}>
                  З питань модерації звертайтесь сюди:
                </div>
                <div className={styles.moderatorsEmail}>
                  <div className={styles.moderatorsideafusioncom3802}>
                    moderators@ideafusion.com
                    <br />
                    +380 66 757 4343
                  </div>
                </div>
              </div>
              <div className={styles.frameChild3} />
              <div className={styles.platformOwner}>
                <div className={styles.div5}>Власник платформи</div>
                <div className={styles.ownerSocials}>
                  <div className={styles.moderatorsideafusioncom3802}>
                    {`Шевченко Анастасія Олександрівна`}
                    <br />
                    {`Instagram: _.anasteishen_ `}
                  </div>
                </div>
              </div>
            </div>
            <div className={styles.frameChild3} />
          </div>
        </div>
        <div className={styles.supportContainer}>
          <div className={styles.supportContainerInner}>
            <div className={styles.group}>
              <div className={styles.div5}>Служба підтримки</div>
              <div className={styles.supportEmail}>
                <div className={styles.moderatorsideafusioncom3802}>
                  {`support@ideafusion.com`}
                  <br />
                  {`+380 66 444 3322 `}
                </div>
              </div>
            </div>
          </div>
          <div className={styles.supportContainerChild} />
        </div>
        <div className={styles.creativePanel}>
          <div className={styles.frameContainer}>
            <img
              className={styles.groupIcon}
              loading="lazy"
              alt=""
              src="/Group-131@2x.png"
            />
            <div className={styles.frameWrapper}>
              <div className={styles.frameDiv}>
                <div className={styles.ideafusionParent}>
                  <h3 className={styles.ideafusion2}>
                    <span>Idea</span>
                    <span className={styles.fusion}>Fusion</span>
                  </h3>
                  <img
                    className={styles.vectorIcon2}
                    loading="lazy"
                    alt=""
                    src="/Brand-Icon.svg"
                  />
                </div>
                <div className={styles.creativeCollaborationPlatfor2}>
                  CREATIVE COLLABORATION PLATFORM
                </div>
              </div>
            </div>
          </div>
        </div>
      </div>
    </section>
  );
};

export default GroupComponent;
