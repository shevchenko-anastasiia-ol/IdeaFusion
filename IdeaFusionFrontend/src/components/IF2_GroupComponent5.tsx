import { FunctionComponent } from "react";
import styles from "./IF2_GroupComponent5.module.css";

export type GroupComponent5Type = {
  className?: string;
};

const GroupComponent5: FunctionComponent<GroupComponent5Type> = ({
  className = "",
}) => {
  return (
    <section className={[styles.rectangleParent, className].join(" ")}>
      <div className={styles.frameChild} />
      <div className={styles.supportItem}>
        <div className={styles.supportArea}>
          <div className={styles.div}>З питань модерації звертайтесь сюди:</div>
          <div className={styles.area}>
            <div className={styles.instagramAnasteishen}>
              moderators@ideafusion.com
              <br />
              +380 66 757 4343
            </div>
          </div>
        </div>
      </div>
      <div className={styles.divisor}>
        <div className={styles.stroke} />
      </div>
      <div className={styles.division}>
        <div className={styles.panel}>
          <div className={styles.div}>Власник платформи</div>
          <div className={styles.label}>
            <div className={styles.instagramAnasteishen}>
              {`Шевченко Анастасія Олександрівна`}
              <br />
              {`Instagram: _.anasteishen_ `}
            </div>
          </div>
        </div>
        <div className={styles.splitter} />
      </div>
      <div className={styles.supportItem2}>
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
      <div className={styles.divider}>
        <div className={styles.stroke} />
      </div>
      <div className={styles.interface}>
        <div className={styles.pageBlock}>
          <img
            className={styles.pageBlockChild}
            loading="lazy"
            alt=""
            src="/Group-131@2x.png"
          />
          <div className={styles.stage}>
            <div className={styles.ideafusionParent}>
              <h3 className={styles.ideafusion}>
                <span>Idea</span>
                <span className={styles.fusion}>Fusion</span>
              </h3>
              <img className={styles.graphicIcon} alt="" src="/Graphic.svg" />
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
