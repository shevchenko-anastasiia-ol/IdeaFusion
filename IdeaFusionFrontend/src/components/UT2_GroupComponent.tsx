import { FunctionComponent } from "react";
import styles from "./UT2_GroupComponent.module.css";

export type GroupComponentType = {
  className?: string;
};

const GroupComponent: FunctionComponent<GroupComponentType> = ({
  className = "",
}) => {
  return (
    <section className={[styles.rectangleParent, className].join(" ")}>
      <div className={styles.frameChild} />
      <div className={styles.moderationContainerWrapper}>
        <div className={styles.moderationContainer}>
          <div className={styles.moderationDetails}>
            <div className={styles.moderationInfo}>
              <div className={styles.div}>
                З питань модерації звертайтесь сюди:
              </div>
              <div className={styles.moderationContact}>
                <div className={styles.instagramAnasteishen}>
                  moderators@ideafusion.com
                  <br />
                  +380 66 757 4343
                </div>
              </div>
            </div>
            <div className={styles.moderationDetailsChild} />
            <div className={styles.parent}>
              <div className={styles.div2}>Власник платформи</div>
              <div className={styles.ownerInfo}>
                <div className={styles.instagramAnasteishen}>
                  {`Шевченко Анастасія Олександрівна`}
                  <br />
                  {`Instagram: _.anasteishen_ `}
                </div>
              </div>
            </div>
          </div>
          <div className={styles.moderationDetailsChild} />
        </div>
      </div>
      <div className={styles.supportContainerParent}>
        <div className={styles.supportContainer}>
          <div className={styles.supportDetails}>
            <div className={styles.div2}>Служба підтримки</div>
            <div className={styles.supportContact}>
              <div className={styles.instagramAnasteishen}>
                {`support@ideafusion.com`}
                <br />
                {`+380 66 444 3322 `}
              </div>
            </div>
          </div>
        </div>
        <div className={styles.frameItem} />
      </div>
      <div className={styles.platformInfo}>
        <div className={styles.infoContainer}>
          <img
            className={styles.infoContainerChild}
            loading="lazy"
            alt=""
            src="/Group-131@2x.png"
          />
          <div className={styles.brandContainerWrapper}>
            <div className={styles.brandContainer}>
              <div className={styles.brandDetails}>
                <h3 className={styles.ideafusion}>
                  <span>Idea</span>
                  <span className={styles.fusion}>Fusion</span>
                </h3>
                <img className={styles.brandIcon} alt="" src="/Arrow-5.svg" />
              </div>
              <div className={styles.creativeCollaborationPlatfor}>
                CREATIVE COLLABORATION PLATFORM
              </div>
            </div>
          </div>
        </div>
      </div>
    </section>
  );
};

export default GroupComponent;
