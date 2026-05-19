import { FunctionComponent } from "react";
import styles from "./IF2_GroupComponent2.module.css";

export type GroupComponent2Type = {
  className?: string;
};

const GroupComponent2: FunctionComponent<GroupComponent2Type> = ({
  className = "",
}) => {
  return (
    <footer className={[styles.rectangleParent, className].join(" ")}>
      <div className={styles.frameChild} />
      <div className={styles.adminDetailsWrapper}>
        <div className={styles.adminDetails}>
          <div className={styles.infoContainerParent}>
            <div className={styles.infoContainer}>
              <div className={styles.div}>
                З питань модерації звертайтесь сюди:
              </div>
              <div className={styles.moderatorsideafusioncom380Wrapper}>
                <div className={styles.instagramAnasteishen}>
                  moderators@ideafusion.com
                  <br />
                  +380 66 757 4343
                </div>
              </div>
            </div>
            <div className={styles.modSeparator} />
            <div className={styles.platformOwner}>
              <div className={styles.div2}>Власник платформи</div>
              <div className={styles.instagramAnasteishenWrapper}>
                <div className={styles.instagramAnasteishen}>
                  {`Шевченко Анастасія Олександрівна`}
                  <br />
                  {`Instagram: _.anasteishen_ `}
                </div>
              </div>
            </div>
          </div>
          <div className={styles.modSeparator} />
        </div>
      </div>
      <div className={styles.supportDetails}>
        <div className={styles.supportDetailsInner}>
          <div className={styles.parent}>
            <div className={styles.div2}>Служба підтримки</div>
            <div className={styles.supportideafusioncom38066Wrapper}>
              <div className={styles.instagramAnasteishen}>
                {`support@ideafusion.com`}
                <br />
                {`+380 66 444 3322 `}
              </div>
            </div>
          </div>
        </div>
        <div className={styles.serviceSeparator} />
      </div>
      <div className={styles.fusionFooterWrapper}>
        <div className={styles.fusionFooter}>
          <img
            className={styles.fusionFooterChild}
            loading="lazy"
            alt=""
            src="/Group-13@2x.png"
          />
          <div className={styles.brandInfo}>
            <div className={styles.ideafusionParent}>
              <h3 className={styles.ideafusion}>
                <span>Idea</span>
                <span className={styles.fusion}>Fusion</span>
              </h3>
              <img
                className={styles.fusionLogoIcon}
                alt=""
                src="/Graphic.svg"
              />
              <div className={styles.creativeCollaborationPlatfor}>
                CREATIVE COLLABORATION PLATFORM
              </div>
            </div>
          </div>
        </div>
      </div>
    </footer>
  );
};

export default GroupComponent2;
