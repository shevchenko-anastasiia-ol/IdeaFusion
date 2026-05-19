import { FunctionComponent } from "react";
import { Box, Typography } from "@mui/material";
import styles from "./RequestArea.module.css";

export type RequestAreaType = {
  className?: string;
};

const RequestArea: FunctionComponent<RequestAreaType> = ({
  className = "",
}) => {
  return (
    <section className={[styles.requestArea, className].join(" ")}>
      <Box className={styles.requestAreaChild} />
      <Box className={styles.requestAreaInner}>
        <Box className={styles.frameParent}>
          <Box className={styles.frameWrapper}>
            <Box className={styles.frameGroup}>
              <Box className={styles.wrapper}>
                <Typography
                  className={styles.h3}
                  variant="inherit"
                  variantMapping={{ inherit: "h3" }}
                  sx={{ fontWeight: "800" }}
                >
                  Реквести
                </Typography>
              </Box>
              <Box className={styles.requestIndicator}>
                <Box className={styles.requestIndicatorChild} />
                <Typography
                  className={styles.notificationCounter}
                  variant="inherit"
                  variantMapping={{ inherit: "h3" }}
                  sx={{ fontWeight: "800" }}
                >
                  2
                </Typography>
              </Box>
              <span
                className={styles.viewAllBtn}
                onClick={() => window.location.href = "/team/invitations"}
              >
                Переглянути всі
              </span>
            </Box>
          </Box>
          <Box className={styles.rectangleParent}>
            <Box className={styles.frameChild} />
            <Box className={styles.requestSeparatorParent}>
              <Box className={styles.requestSeparator} />
              <Box className={styles.requestEntryInfo}>
                <Box className={styles.requestInnerEntry}>
                  <Box className={styles.requestProfileArea}>
                    <Box className={styles.ellipseParent}>
                      <Box className={styles.frameItem} />
                      <Typography
                        className={styles.h32}
                        variant="inherit"
                        variantMapping={{ inherit: "h3" }}
                        sx={{ fontWeight: "700" }}
                      >
                        ДМ
                      </Typography>
                    </Box>
                    <Box className={styles.applicantData}>
                      <Box className={styles.frameContainer}>
                        <Box className={styles.container}>
                          <Typography
                            className={styles.h33}
                            variant="inherit"
                            variantMapping={{ inherit: "h3" }}
                            sx={{ fontWeight: "600" }}
                          >
                            Дмитро Мельник
                          </Typography>
                        </Box>
                        <div className={styles.div}>2 години тому</div>
                      </Box>
                    </Box>
                  </Box>
                  <Box className={styles.requestStatus}>
                    <Box className={styles.rectangleGroup}>
                      <Box className={styles.frameInner} />
                      <div className={styles.div2}>Нове</div>
                    </Box>
                  </Box>
                </Box>
                <Box className={styles.applicantDescription}>
                  <div className={styles.ost}>
                    Хочу приєднатись допроєкту банкінгу як розробник
                  </div>
                </Box>
              </Box>
            </Box>
            <Box className={styles.frameDiv}>
              <button className={styles.rectangleContainer}>
                <Box className={styles.rectangleDiv} />
                <div className={styles.div4}>Прийняти</div>
              </button>
              <Box className={styles.frame}>
                <div className={styles.div5}>Відхилити</div>
              </Box>
            </Box>
          </Box>
        </Box>
      </Box>
      <Box className={styles.requestAreaInner2}>
        <Box className={styles.groupDiv}>
          <Box className={styles.frameChild2} />
          <Box className={styles.frameParent2}>
            <Box className={styles.avatarDetailsParent}>
              <Box className={styles.ellipseParent}>
                <Box className={styles.requestIndicatorChild} />
                <Typography
                  className={styles.h34}
                  variant="inherit"
                  variantMapping={{ inherit: "h3" }}
                  sx={{ fontWeight: "700" }}
                >
                  ЮБ
                </Typography>
              </Box>
              <Box className={styles.applicantData}>
                <Box className={styles.frameContainer}>
                  <Box className={styles.container}>
                    <Typography
                      className={styles.h33}
                      variant="inherit"
                      variantMapping={{ inherit: "h3" }}
                      sx={{ fontWeight: "600" }}
                    >
                      Юлія Бондар
                    </Typography>
                  </Box>
                  <div className={styles.div}>5 годин тому</div>
                </Box>
              </Box>
            </Box>
            <Box className={styles.ostGamedevWrapper}>
              <div className={styles.ost}>
                Можу допомогти з OST,маю досвід у gamedev
              </div>
            </Box>
          </Box>
          <Box className={styles.frameParent4}>
            <button className={styles.rectangleContainer}>
              <Box className={styles.rectangleDiv} />
              <div className={styles.div4}>Прийняти</div>
            </button>
            <Box className={styles.frame}>
              <div className={styles.div5}>Відхилити</div>
            </Box>
          </Box>
        </Box>
      </Box>
      <Box className={styles.frameParent5}>
        <Box className={styles.wrapper4}>
          <div className={styles.div9}>Оброблені</div>
        </Box>
        <Box className={styles.rectangleParent2}>
          <Box className={styles.frameChild4} />
          <Box className={styles.handledContentParent}>
            <Box className={styles.handledContent}>
              <Box className={styles.handledDetails}>
                <Box className={styles.ellipseParent}>
                  <Box className={styles.avatarHandledChild} />
                  <Typography
                    className={styles.h36}
                    variant="inherit"
                    variantMapping={{ inherit: "h3" }}
                    sx={{ fontWeight: "700" }}
                  >
                    АВ
                  </Typography>
                </Box>
                <Box className={styles.userInfoHandled}>
                  <Box className={styles.frameContainer}>
                    <Box className={styles.container}>
                      <Typography
                        className={styles.h33}
                        variant="inherit"
                        variantMapping={{ inherit: "h3" }}
                        sx={{ fontWeight: "600" }}
                      >
                        Андрій Власенко
                      </Typography>
                    </Box>
                    <div className={styles.div10}>12 годин тому</div>
                  </Box>
                </Box>
              </Box>
              <Box className={styles.handledContentInner}>
                <Box className={styles.acceptRect}>
                  <Box className={styles.frameChild5} />
                  <div className={styles.div11}>Відхилене</div>
                </Box>
              </Box>
            </Box>
            <Box className={styles.applicantDescription}>
              <div
                className={styles.ost}
              >{`Пропоную об’єднатись, я пишу музику, ти малюєш арт для заставки пісні `}</div>
            </Box>
          </Box>
          <Box className={styles.wrapper5}>
            <div className={styles.div5}>Відхилено</div>
          </Box>
        </Box>
      </Box>
      <Box className={styles.rectangleParent4}>
        <Box className={styles.frameChild4} />
        <Box className={styles.handledContentParent}>
          <Box className={styles.acceptedDetails}>
            <Box className={styles.ellipseParent}>
              <Box className={styles.avatarHandledChild} />
              <Typography
                className={styles.h38}
                variant="inherit"
                variantMapping={{ inherit: "h3" }}
                sx={{ fontWeight: "700" }}
              >
                ММ
              </Typography>
            </Box>
            <Box className={styles.userInfoHandled}>
              <Box className={styles.frameContainer}>
                <Box className={styles.container}>
                  <Typography
                    className={styles.h33}
                    variant="inherit"
                    variantMapping={{ inherit: "h3" }}
                    sx={{ fontWeight: "600" }}
                  >
                    Микола Мельник
                  </Typography>
                </Box>
                <div className={styles.div10}>17 годин тому</div>
              </Box>
            </Box>
            <Box className={styles.handledContentInner}>
              <Box className={styles.acceptRect}>
                <Box className={styles.acceptRectChild} />
                <div className={styles.div11}>Прийнято</div>
              </Box>
            </Box>
          </Box>
          <Box className={styles.applicantDescription}>
            <div
              className={styles.ost}
            >{`Пропоную об’єднатись, я пишу музику, ти малюєш арт для заставки пісні `}</div>
          </Box>
        </Box>
        <Box className={styles.acceptedTeam}>
          <div className={styles.div5}>Прийнято в команду</div>
        </Box>
      </Box>
    </section>
  );
};

export default RequestArea;
