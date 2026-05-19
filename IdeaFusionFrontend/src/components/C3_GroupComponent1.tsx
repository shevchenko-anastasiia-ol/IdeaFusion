import { FunctionComponent } from "react";
import styles from "./C3_GroupComponent1.module.css";

export type GroupComponent1Type = {
  className?: string;
};

const GroupComponent1: FunctionComponent<GroupComponent1Type> = ({
  className = "",
}) => {
  return (
    <section className={[styles.rectangleParent, className].join(" ")}>
      <div className={styles.frameChild} />
      <div className={styles.containerWrapper}>
        <div className={styles.container}>
          <div className={styles.contentContainerParent}>
            <div className={styles.contentContainer}>
              <div className={styles.div}>
                З питань модерації звертайтесь сюди:
              </div>
              <div className={styles.email}>
                <div className={styles.instagramAnasteishen}>
                  moderators@ideafusion.com
                  <br />
                  +380 66 757 4343
                </div>
              </div>
            </div>
            <div className={styles.separator} />
            <div className={styles.owner}>
              <div className={styles.div2}>Власник платформи</div>
              <div className={styles.ownerDetails}>
                <div className={styles.instagramAnasteishen}>
                  {`Шевченко Анастасія Олександрівна`}
                  <br />
                  {`Instagram: _.anasteishen_ `}
                </div>
              </div>
            </div>
          </div>
          <div className={styles.separator} />
        </div>
      </div>
      <div className={styles.frameParent}>
        <div className={styles.assistanceWrapper}>
          <div className={styles.assistance}>
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
        <div className={styles.serviceDivider} />
      </div>
      <div className={styles.footerContainerWrapper}>
        <div className={styles.footerContainer}>
          <img
            className={styles.footerContainerChild}
            loading="lazy"
            alt=""
            src="/Group-13@2x.png"
          />
          <div className={styles.logoContentWrapper}>
            <div className={styles.logoContent}>
              <h3 className={styles.ideafusion}>
                <span>Idea</span>
                <span className={styles.fusion}>Fusion</span>
              </h3>
              <img className={styles.logoIcon} alt="" src="/Logo-Icon.svg" />
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

export default GroupComponent1;
