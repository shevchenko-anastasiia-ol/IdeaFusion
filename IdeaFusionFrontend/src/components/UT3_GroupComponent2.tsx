import { FunctionComponent } from "react";
import styles from "./UT3_GroupComponent2.module.css";

export type GroupComponent2Type = {
  className?: string;
};

const GroupComponent2: FunctionComponent<GroupComponent2Type> = ({
  className = "",
}) => {
  return (
    <section className={[styles.rectangleParent, className].join(" ")}>
      <div className={styles.frameChild} />
      <div className={styles.contactContainer}>
        <div className={styles.moderationInfoParent}>
          <div className={styles.moderationInfo}>
            <div className={styles.moderationDetails}>
              <div className={styles.div}>
                З питань модерації звертайтесь сюди:
              </div>
              <div className={styles.moderationEmail}>
                <div className={styles.instagramAnasteishen}>
                  moderators@ideafusion.com
                  <br />
                  +380 66 757 4343
                </div>
              </div>
            </div>
            <div className={styles.moderatorDivider} />
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
          <div className={styles.moderatorDivider} />
        </div>
      </div>
      <div className={styles.supportInfoParent}>
        <div className={styles.supportInfo}>
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
        <div className={styles.siteFooterDivider} />
      </div>
      <div className={styles.footerWrapperWrapper}>
        <div className={styles.footerWrapper}>
          <img
            className={styles.footerWrapperChild}
            loading="lazy"
            alt=""
            src="/Group-13@2x.png"
          />
          <div className={styles.companyInfo}>
            <div className={styles.brandContainer}>
              <h3 className={styles.ideafusion}>
                <span>Idea</span>
                <span className={styles.fusion}>Fusion</span>
              </h3>
              <img className={styles.brandIcon} alt="" src="/Brand-Icon.svg" />
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

export default GroupComponent2;
