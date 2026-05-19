import { FunctionComponent } from "react";
import styles from "./UT3_PasswordInput.module.css";

export type PasswordInputType = {
  className?: string;
};

const PasswordInput: FunctionComponent<PasswordInputType> = ({
  className = "",
}) => {
  return (
    <div className={[styles.passwordInput, className].join(" ")}>
      <div className={styles.passwordInputChild} />
      <div className={styles.wrapper}>
        <h3 className={styles.h3}>Надішли запрошення користувачу</h3>
      </div>
      <div className={styles.roleContainerParent}>
        <div className={styles.roleContainer}>
          <div className={styles.div}>Від кого</div>
        </div>
        <div className={styles.emailInput}>
          <div className={styles.emailInputChild} />
          <div className={styles.passwordFields}>
            <div className={styles.loginAction}>
              <div className={styles.loginActionChild} />
              <h3 className={styles.h32}>ЮБ</h3>
            </div>
            <div className={styles.loginRegisterWrapper}>
              <div className={styles.loginRegister}>
                <div className={styles.container}>
                  <h3 className={styles.h33}>Юлія Бондар</h3>
                </div>
                <div className={styles.div2}>Композитор</div>
              </div>
            </div>
          </div>
          <button className={styles.receptionDisplayWrapper}>
            <div className={styles.receptionDisplay}>
              <div className={styles.receptionDisplayChild} />
              <div className={styles.x}>x</div>
            </div>
          </button>
        </div>
      </div>
      <div className={styles.passwordInputInner}>
        <div className={styles.teamHeaderParent}>
          <div className={styles.teamHeader}>
            <div className={styles.div}>В команду</div>
          </div>
          <div className={styles.rectangleParent}>
            <div className={styles.frameChild} />
            <div className={styles.lineParent}>
              <div className={styles.frameItem} />
              <div className={styles.frameWrapper}>
                <div className={styles.frameParent}>
                  <div className={styles.parent}>
                    <h3 className={styles.h34}>Арт майбутнього</h3>
                    <div className={styles.frameContainer}>
                      <div className={styles.rectangleGroup}>
                        <div className={styles.frameInner} />
                        <div className={styles.div4}>У пошуку</div>
                      </div>
                    </div>
                  </div>
                  <div className={styles.illustratorParent}>
                    <div className={styles.illustrator}>
                      Графічний дизайн · Illustrator · Арт
                    </div>
                    <div className={styles.div5}>
                      потрібен графічний дизайнер
                    </div>
                  </div>
                </div>
              </div>
            </div>
            <input className={styles.frameInput} type="radio" />
          </div>
        </div>
      </div>
      <div className={styles.roleContainerParent}>
        <div className={styles.frame}>
          <div className={styles.div}>На роль</div>
        </div>
        <div className={styles.rectangleContainer}>
          <div className={styles.emailInputChild} />
          <div className={styles.div7}>Оберіть з наявних...</div>
          <div className={styles.vectorWrapper}>
            <img
              className={styles.arrowIcon}
              loading="lazy"
              alt=""
              src="/Brand-Icon.svg"
            />
          </div>
        </div>
      </div>
      <div className={styles.frameDiv}>
        <div className={styles.frame}>
          <div className={styles.div}>Текст повідомлення</div>
        </div>
        <div className={styles.statusBarContainer}>
          <div className={styles.statusBarContainerChild} />
          <div className={styles.div9}>
            Доброго дня, прошу розглянути мою кандидатуру на учасника вашої
            команди. Я переглянула ваше портфоліо та ознайомилась зі специфікою
            проєкту, тому можу запевнити, що я найкращий кандидат для вашого
            проєкту. Я маю усі практичні навички та глибого ознакомлена з темою.
            Прошу продивитись моє портфоліо та впевнетись в мені.
          </div>
        </div>
      </div>
    </div>
  );
};

export default PasswordInput;
