import { FunctionComponent, useMemo, type CSSProperties } from "react";
import styles from "./IF2_FrameComponent1.module.css";

export type FrameComponent1Type = {
  className?: string;
  prop?: string;
  illustratorProcreateBlender?: string;
  prop1?: string;

  /** Style props */
  frameDivMinWidth?: CSSProperties["minWidth"];
  h2Margin?: CSSProperties["margin"];
  frameDivMinWidth1?: CSSProperties["minWidth"];
  frameButtonPadding?: CSSProperties["padding"];
};

const FrameComponent1: FunctionComponent<FrameComponent1Type> = ({
  className = "",
  frameDivMinWidth,
  h2Margin,
  frameDivMinWidth1,
  prop,
  illustratorProcreateBlender,
  frameButtonPadding,
  prop1,
}) => {
  const frameDiv8Style: CSSProperties = useMemo(() => {
    return {
      minWidth: frameDivMinWidth,
    };
  }, [frameDivMinWidth]);

  const h2Style: CSSProperties = useMemo(() => {
    return {
      margin: h2Margin,
    };
  }, [h2Margin]);

  const frameDiv9Style: CSSProperties = useMemo(() => {
    return {
      minWidth: frameDivMinWidth1,
    };
  }, [frameDivMinWidth1]);

  const frameButtonStyle: CSSProperties = useMemo(() => {
    return {
      padding: frameButtonPadding,
    };
  }, [frameButtonPadding]);

  return (
    <section className={[styles.rectangleParent, className].join(" ")}>
      <div className={styles.frameChild} />
      <div className={styles.frameParent} style={frameDiv8Style}>
        <div className={styles.ellipseParent}>
          <div className={styles.frameItem} />
          <h2 className={styles.h2} style={h2Style}>
            АШ
          </h2>
        </div>
        <div className={styles.frameWrapper} style={frameDiv9Style}>
          <div className={styles.parent}>
            <h2 className={styles.h22}>{prop}</h2>
            <h3 className={styles.illustratorProcreate}>
              {illustratorProcreateBlender}
            </h3>
          </div>
        </div>
      </div>
      <div className={styles.frameContainer}>
        <button className={styles.rectangleGroup} style={frameButtonStyle}>
          <div className={styles.frameInner} />
          <h3 className={styles.h3}>{prop1}</h3>
        </button>
      </div>
    </section>
  );
};

export default FrameComponent1;
