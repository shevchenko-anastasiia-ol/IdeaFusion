import { useEffect } from "react";
import { useNavigate } from "react-router-dom";
import { teamsApi } from "../api/teams";
import { useAuth } from "../context/AuthContext";
import { Page } from "../layout/AppShell";
import { usePageMeta } from "../layout/PageMeta";
import { Loading } from "../ui";

/** /team: opens the caller's first team, or the create form when there is none. */
export default function TeamPage() {
  usePageMeta("Команда");
  const navigate = useNavigate();
  const { user, loading: authLoading } = useAuth();

  useEffect(() => {
    if (authLoading) return;
    if (!user) {
      navigate("/login", { replace: true });
      return;
    }
    teamsApi
      .getMine()
      .then(teams => navigate(teams.length > 0 ? `/teams/${teams[0].id}` : "/team/new", { replace: true }))
      .catch(() => navigate("/team/new", { replace: true }));
  }, [user, authLoading, navigate]);

  return (
    <Page>
      <Loading label="Відкриваємо вашу команду…" />
    </Page>
  );
}
