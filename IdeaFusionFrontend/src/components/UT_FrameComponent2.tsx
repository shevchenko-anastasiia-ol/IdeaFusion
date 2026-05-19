import { FunctionComponent, useState } from "react";
import ProfileRows from "./UT_ProfileRows";
import MemberProfiles from "./UT_MemberProfiles";
import styles from "./UT_FrameComponent2.module.css";

export type FrameComponent2Type = {
  className?: string;
};

const FrameComponent2: FunctionComponent<FrameComponent2Type> = ({
  className = "",
}) => {
  const [memberProfilesItems] = useState([
    {
      memberProfilesAlignItems: "flex-end" as const,
      memberProfilesAlignContent: "flex-end" as const,
      memberProfilesPadding: "13px 14px 12px 17px" as const,
      memberProfilesGap: "128px" as const,
      frameDivFlex: undefined,
      frameDivMinWidth: undefined,
      ellipseDivBackgroundColor: "#ffb347" as const,
      prop: "ЮБ",
      h3Left: "9px" as const,
      h3MinWidth: "41px" as const,
      prop1: "Юлія Бондар",
      prop2: "Композитор",
      frameDivJustifyContent: "flex-end" as const,
      frameDivPadding: "0px 0px 16px" as const,
    },
    {
      memberProfilesAlignItems: "flex-start" as const,
      memberProfilesAlignContent: "flex-start" as const,
      memberProfilesPadding: "12px 14px 13px 17px" as const,
      memberProfilesGap: "86px" as const,
      frameDivFlex: 1,
      frameDivMinWidth: "158px" as const,
      ellipseDivBackgroundColor: "#00c9a7" as const,
      prop: "АВ",
      h3Left: "13px" as const,
      h3MinWidth: "34px" as const,
      prop1: "Андрій Власенко",
      prop2: "3D артист",
      frameDivJustifyContent: "unset" as const,
      frameDivPadding: "16px 0px 0px" as const,
    },
    {
      memberProfilesAlignItems: "flex-start" as const,
      memberProfilesAlignContent: "flex-start" as const,
      memberProfilesPadding: undefined,
      memberProfilesGap: "86px" as const,
      frameDivFlex: 1,
      frameDivMinWidth: "158px" as const,
      ellipseDivBackgroundColor: "#ff6b6b" as const,
      prop: "КГ",
      h3Left: "16px" as const,
      h3MinWidth: "32px" as const,
      prop1: "Катерина Гончар",
      prop2: "UI/UX дизайнер",
      frameDivJustifyContent: "unset" as const,
      frameDivPadding: "14px 0px 0px" as const,
    },
  ]);
  return (
    <section className={[styles.rectangleParent, className].join(" ")}>
      <div className={styles.frameChild} />
      <div className={styles.membersContainer}>
        <h3 className={styles.h3}>Учасники команди</h3>
      </div>
      <div className={styles.membersProfile}>
        <div className={styles.memberProfiles}>
          <div className={styles.memberProfilesChild} />
          <ProfileRows
            prop="ДМ"
            prop1="Дмитро Мельник"
            react="Розробник React"
          />
          <div className={styles.rolesManagement}>
            <div className={styles.rectangleGroup}>
              <div className={styles.frameItem} />
              <div className={styles.div}>Лідер</div>
            </div>
          </div>
        </div>
        {memberProfilesItems.map((item, index) => (
          <MemberProfiles
            key={index}
            memberProfilesAlignItems={item.memberProfilesAlignItems}
            memberProfilesAlignContent={item.memberProfilesAlignContent}
            memberProfilesPadding={item.memberProfilesPadding}
            memberProfilesGap={item.memberProfilesGap}
            frameDivFlex={item.frameDivFlex}
            frameDivMinWidth={item.frameDivMinWidth}
            ellipseDivBackgroundColor={item.ellipseDivBackgroundColor}
            prop={item.prop}
            h3Left={item.h3Left}
            h3MinWidth={item.h3MinWidth}
            prop1={item.prop1}
            prop2={item.prop2}
            frameDivJustifyContent={item.frameDivJustifyContent}
            frameDivPadding={item.frameDivPadding}
          />
        ))}
        <div className={styles.memberProfiles2}>
          <div className={styles.memberProfilesChild} />
          <div className={styles.frameParent}>
            <div className={styles.ellipseWrapper}>
              <button className={styles.frameInner} />
            </div>
            <button className={styles.button}>+</button>
          </div>
          <div className={styles.wrapper}>
            <div className={styles.div2}>Потрібен графічний дизайнер</div>
          </div>
        </div>
      </div>
    </section>
  );
};

export default FrameComponent2;
