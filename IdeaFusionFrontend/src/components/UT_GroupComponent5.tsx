import { FunctionComponent } from "react";
import styles from "./UT_GroupComponent5.module.css";

export type GroupComponent5Type = {
  className?: string;
};

const GroupComponent5: FunctionComponent<GroupComponent5Type> = ({
  className = "",
}) => {
  return (
    <section className={[styles.rectangleParent, className].join(" ")}>
      <div className={styles.frameChild} />
      <div className={styles.assistanceStackWrapper}>
        <div className={styles.assistanceStack}>
          <div className={styles.div}>З питань модерації звертайтесь сюди:</div>
          <div className={styles.supportAddresses}>
            <div className={styles.instagramAnasteishen}>
              moderators@ideafusion.com
              <br />
              +380 66 757 4343
            </div>
          </div>
        </div>
      </div>
      <div className={styles.lineWrapper}>
        <div className={styles.frameItem} />
      </div>
      <div className={styles.creatorDetails}>
        <div className={styles.ownerInformation}>
          <div className={styles.div}>Власник платформи</div>
          <div className={styles.ownershipInfo}>
            <div className={styles.instagramAnasteishen}>
              {`Шевченко Анастасія Олександрівна`}
              <br />
              {`Instagram: _.anasteishen_ `}
            </div>
          </div>
        </div>
        <div className={styles.creatorDetailsChild} />
      </div>
      <div className={styles.frameWrapper}>
        <div className={styles.parent}>
          <div className={styles.div}>Служба підтримки</div>
          <div className={styles.supportideafusioncom38066Wrapper}>
            <div className={styles.instagramAnasteishen}>
              {`support@ideafusion.com`}
              <br />
              {`+380 66 444 3322 `}
            </div>
          </div>
        </div>
      </div>
      <div className={styles.lineContainer}>
        <div className={styles.frameItem} />
      </div>
      <div className={styles.siteCreator}>
        <div className={styles.platformFooter}>
          <img
            className={styles.platformFooterChild}
            loading="lazy"
            alt=""
            src="/Group-131@2x.png"
          />
          <div className={styles.platformFooterInner}>
            <div className={styles.designIdentityParent}>
              <div className={styles.designIdentity}>
                <h3 className={styles.ideafusion}>
                  <span>Idea</span>
                  <span className={styles.fusion}>Fusion</span>
                </h3>
                <img
                  className={styles.brandSignIcon}
                  alt=""
                  src="/Arrow-10.svg"
                />
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

export default GroupComponent5;
