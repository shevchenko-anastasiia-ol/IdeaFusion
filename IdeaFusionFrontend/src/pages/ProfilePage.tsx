import { FunctionComponent } from "react";
import FrameComponent2 from "../components/FrameComponent2";
import FrameComponent1 from "../components/IF2_FrameComponent1";
import FrameComponent2Profile from "../components/IF2_FrameComponent2";
import FrameComponent3 from "../components/IF2_FrameComponent3";
import FrameComponent4 from "../components/IF2_FrameComponent4";
import GroupComponent2 from "../components/IF2_GroupComponent2";
import styles from "./ProfilePage.module.css";
import { useNavigate } from "react-router-dom";
import { useAuth } from "../context/AuthContext";


const Component2: FunctionComponent = () => {
  const navigate = useNavigate();
  const { user } = useAuth();

  const displayName = user?.email?.split('@')[0] ?? 'Користувач';
  const userRole    = user?.roles?.join(' · ') ?? 'Користувач';

  return (
    <div className={styles.div}>
      <FrameComponent2 />

      <main className={styles.main}>
        <header className={styles.pageHeader}>
          <h2 className={styles.h2}>Подробиці профілю</h2>
        </header>

        {/* Профіль — кнопка «Запросити до команди» */}
        <div onClick={() => navigate("/team/invite-to-team")} style={{ cursor: "pointer" }}>
          <FrameComponent1
            prop={displayName}
            illustratorProcreateBlender={userRole}
            prop1="Запросити до команди"
          />
        </div>
  
        <div className={styles.twoCol}>
  
          {/* Портфоліо — клік на картку → деталі посту */}
          <div onClick={() => navigate("/posts/1")} style={{ cursor: "pointer" }}>
            <FrameComponent2Profile />
          </div>
  
          <div className={styles.rightCol}>
  
            {/* Команди — клік на рядок → сторінка команди */}
            <div onClick={() => navigate("/team")} style={{ cursor: "pointer" }}>
              <FrameComponent3 />
            </div>
  
            <FrameComponent4 />
          </div>
        </div>
  
        <GroupComponent2 />
      </main>
    </div>
  );
};

export default Component2;