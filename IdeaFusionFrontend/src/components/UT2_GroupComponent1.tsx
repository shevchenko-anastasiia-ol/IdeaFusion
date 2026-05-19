import { FunctionComponent } from "react";
import styles from "./UT2_GroupComponent1.module.css";

export type GroupComponent1Type = {
  className?: string;
};

const GroupComponent1: FunctionComponent<GroupComponent1Type> = ({
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
        <img className={styles.vectorIcon} alt="" src="/Arrow-5.svg" />
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
        <div className={styles.moderationInnerWrapper}>
          <div className={styles.moderationInner}>
            <div className={styles.moderationContainer}>
              <div className={styles.moderationDetails}>
                <div className={styles.div4}>
                  З питань модерації звертайтесь сюди:
                </div>
                <div className={styles.moderationContact}>
                  <div className={styles.moderatorsideafusioncom3802}>
                    moderators@ideafusion.com
                    <br />
                    +380 66 757 4343
                  </div>
                </div>
              </div>
              <div className={styles.moderationContainerChild} />
              <div className={styles.parent}>
                <div className={styles.div5}>Власник платформи</div>
                <div className={styles.ownerInfo}>
                  <div className={styles.moderatorsideafusioncom3802}>
                    {`Шевченко Анастасія Олександрівна`}
                    <br />
                    {`Instagram: _.anasteishen_ `}
                  </div>
                </div>
              </div>
            </div>
            <div className={styles.moderationContainerChild} />
          </div>
        </div>
        <div className={styles.supportInnerParent}>
          <div className={styles.supportInner}>
            <div className={styles.supportContainer}>
              <div className={styles.div5}>Служба підтримки</div>
              <div className={styles.supportContact}>
                <div className={styles.moderatorsideafusioncom3802}>
                  {`support@ideafusion.com`}
                  <br />
                  {`+380 66 444 3322 `}
                </div>
              </div>
            </div>
          </div>
          <div className={styles.frameChild3} />
        </div>
        <div className={styles.footerInnerWrapper}>
          <div className={styles.footerInner}>
            <img
              className={styles.footerInnerChild}
              loading="lazy"
              alt=""
              src="/Group-131@2x.png"
            />
            <div className={styles.brandInnerWrapper}>
              <div className={styles.brandInner}>
                <div className={styles.brandContainer}>
                  <h3 className={styles.ideafusion2}>
                    <span>Idea</span>
                    <span className={styles.fusion}>Fusion</span>
                  </h3>
                  <img
                    className={styles.vectorIcon2}
                    alt=""
                    src="/Arrow-5.svg"
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

export default GroupComponent1;
