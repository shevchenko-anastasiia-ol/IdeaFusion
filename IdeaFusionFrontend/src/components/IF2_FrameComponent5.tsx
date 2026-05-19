import { FunctionComponent, useMemo, type CSSProperties } from "react";
import styles from "./IF2_FrameComponent5.module.css";

export type FrameComponent5Type = {
  className?: string;
  prop?: string;

  /** Style props */
  frameDivPadding?: CSSProperties["padding"];
};

const FrameComponent5: FunctionComponent<FrameComponent5Type> = ({
  className = "",
  frameDivPadding,
  prop,
}) => {
  const frameDiv7Style: CSSProperties = useMemo(() => {
    return {
      padding: frameDivPadding,
    };
  }, [frameDivPadding]);

  return (
    <div className={[styles.frameParent, className].join(" ")}>
      <div className={styles.wrapper} style={frameDiv7Style}>
        <div className={styles.div}>{prop}</div>
      </div>
      <input className={styles.frameChild} type="text" />
    </div>
  );
};

export default FrameComponent5;
