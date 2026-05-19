import { FunctionComponent, useMemo, type CSSProperties } from "react";
import styles from "./UT3_FrameComponent1.module.css";

export type FrameComponent1Type = {
  className?: string;
  prop?: string;
  prop1?: string;

  /** Style props */
  frameSectionPadding?: CSSProperties["padding"];
  frameDivPadding?: CSSProperties["padding"];
};

const FrameComponent1: FunctionComponent<FrameComponent1Type> = ({
  className = "",
  frameSectionPadding,
  prop,
  frameDivPadding,
  prop1,
}) => {
  const frameSectionStyle: CSSProperties = useMemo(() => {
    return {
      padding: frameSectionPadding,
    };
  }, [frameSectionPadding]);

  const frameDivStyle: CSSProperties = useMemo(() => {
    return {
      padding: frameDivPadding,
    };
  }, [frameDivPadding]);

  return (
    <section
      className={[styles.rectangleParent, className].join(" ")}
      style={frameSectionStyle}
    >
      <div className={styles.frameChild} />
      <h1 className={styles.h1}>{prop}</h1>
      <div className={styles.wrapper} style={frameDivStyle}>
        <h3 className={styles.h3}>{prop1}</h3>
      </div>
    </section>
  );
};

export default FrameComponent1;
