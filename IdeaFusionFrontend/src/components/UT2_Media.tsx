import { FunctionComponent } from "react";
import styles from "./UT2_Media.module.css";

export type MediaType = {
  className?: string;
};

const Media: FunctionComponent<MediaType> = ({ className = "" }) => {
  return (
    <section className={[styles.media, className].join(" ")}>
      <div className={styles.mediaChild} />
      <div className={styles.mediaSelection}>
        <h3 className={styles.h3}>Медія</h3>
      </div>
      <div className={styles.mediaContainers}>
        <div className={styles.rectangleParent}>
          <div className={styles.frameChild} />
          <div className={styles.cards} />
          <div className={styles.pilotsOverview} />
          <div className={styles.uploadedMedia}>
            <div className={styles.uploadedMediaChild} />
            <h1 className={styles.availablepilots}>+</h1>
          </div>
        </div>
      </div>
      <div className={styles.fileUpload}>
        <label className={styles.label} htmlFor="file-loco-0014-s2-1:347">
          <div className={styles.fileUploadChild} />
          <div className={styles.pngJpgMp4}>
            Оберіть файли або перетягніть сюди
            <br />
            PNG, JPG, MP4, MP3, PDF · до 50 МБ
          </div>
        </label>
        <input
          className={styles.input}
          type="file"
          id="file-loco-0014-s2-1:347"
        />
      </div>
    </section>
  );
};

export default Media;
