import { FunctionComponent, useEffect } from "react";
import { useNavigate } from "react-router-dom";
import FrameComponent2 from "../components/FrameComponent2";
import { teamsApi } from "../api/teams";
import { useAuth } from "../context/AuthContext";
import styles from "./TeamPage.module.css";

const TeamPage: FunctionComponent = () => {
  const navigate = useNavigate();
  const { user, loading: authLoading } = useAuth();

  useEffect(() => {
    if (authLoading) return;
    if (!user) {
      navigate("/login", { replace: true });
      return;
    }
    teamsApi
      .getByMember(user.id)
      .then((teams) => {
        if (teams.length > 0) {
          navigate(`/teams/${teams[0].id}`, { replace: true });
        } else {
          navigate("/team/new", { replace: true });
        }
      })
      .catch(() => navigate("/team/new", { replace: true }));
  }, [user, authLoading]);

  return (
    <div className={styles.page}>
      <FrameComponent2 />
      <div style={{ marginLeft: 124, padding: 40, color: "#888", fontSize: 16 }}>
        Завантаження...
      </div>
    </div>
  );
};

export default TeamPage;