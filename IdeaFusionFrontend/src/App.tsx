import { useEffect, type ReactNode } from "react";
import { Navigate, Route, Routes, useLocation, useNavigationType, useSearchParams } from "react-router-dom";

import AppShell from "./layout/AppShell";
import { RequireAdministrator, RequireAuth, RequireModerator } from "./components/RequireAuth";

// Auth
import LoginPage from "./pages/LoginPage";

// Feed, discovery
import FeedPage from "./pages/FeedPage";
import ForYouPage from "./pages/ForYouPage";
import SearchPage from "./pages/SearchPage";
import SavedSearchesPage from "./pages/SavedSearchesPage";
import SavedPostsPage from "./pages/SavedPostsPage";

// Account and people
import DashboardPage from "./pages/DashboardPage";
import PersonalPage from "./pages/PersonalPage";
import UserPublicPage from "./pages/UserPublicPage";
import AllUsersPage from "./pages/AllUsersPage";
import SecurityPage from "./pages/SecurityPage";

// Teams
import CollaborationsPage from "./pages/CollaborationsPage";
import AllTeamsPage from "./pages/AllTeamsPage";
import TeamPage from "./pages/TeamPage";
import TeamDetailPage from "./pages/team/TeamDetailPage";
import NewTeamPage from "./pages/NewTeamPage";
import JoinRequestPage from "./pages/JoinRequestPage";
import InviteToTeamPage from "./pages/InviteToTeamPage";
import JoinRequestsListPage from "./pages/JoinRequestsListPage";
import MyInvitationsPage from "./pages/MyInvitationsPage";

// Vacancies
import VacanciesPage from "./pages/VacanciesPage";
import VacancyDetailPage from "./pages/VacancyDetailPage";

// Posts
import NewPostPage from "./pages/NewPostPage";
import EditPostPage from "./pages/EditPostPage";
import PostDetailPage from "./pages/PostDetailPage";

// Moderation
import ModerationLogPage from "./pages/ModerationLogPage";
import SuspiciousPostsPage from "./pages/SuspiciousPostsPage";

import NotFoundPage from "./pages/NotFoundPage";

const auth = (el: ReactNode) => <RequireAuth>{el}</RequireAuth>;

/** /team/public used to be a static mock; it now opens the real team, or the catalogue. */
function TeamPublicRedirect() {
  const [params] = useSearchParams();
  const id = params.get("id") ?? params.get("teamId");
  return <Navigate to={id ? `/teams/${id}` : "/teams"} replace />;
}

function App() {
  const action = useNavigationType();
  const { pathname } = useLocation();

  useEffect(() => {
    if (action !== "POP") window.scrollTo(0, 0);
  }, [action, pathname]);

  return (
    <Routes>
      <Route path="/login" element={<LoginPage />} />

      <Route element={<AppShell />}>
        <Route path="/" element={<Navigate to="/home" replace />} />

        {/* Discovery */}
        <Route path="/home" element={<FeedPage />} />
        <Route path="/posts" element={<Navigate to="/home" replace />} />
        <Route path="/for-you" element={auth(<ForYouPage />)} />
        <Route path="/search" element={auth(<SearchPage />)} />
        <Route path="/search/saved" element={auth(<SavedSearchesPage />)} />
        <Route path="/search/classic" element={<Navigate to="/search" replace />} />
        <Route path="/saved" element={auth(<SavedPostsPage />)} />

        {/* Account and people */}
        <Route path="/dashboard" element={auth(<DashboardPage />)} />
        <Route path="/personal" element={auth(<PersonalPage />)} />
        <Route path="/profile" element={<Navigate to="/personal?tab=profile" replace />} />
        <Route path="/portfolio" element={<Navigate to="/personal?tab=portfolio" replace />} />
        <Route path="/security" element={auth(<SecurityPage />)} />
        {/* The user list endpoint is administrator-only server-side, so the guard
            here matches instead of rendering a page that can only show an error. */}
        <Route path="/users" element={auth(<RequireAdministrator><AllUsersPage /></RequireAdministrator>)} />
        <Route path="/users/:id" element={<UserPublicPage />} />

        {/* Moderation */}
        <Route path="/moderation" element={auth(<RequireModerator><ModerationLogPage /></RequireModerator>)} />
        <Route path="/moderation/copies" element={auth(<RequireModerator><SuspiciousPostsPage /></RequireModerator>)} />

        {/* Teams */}
        <Route path="/collaborations" element={<CollaborationsPage />} />
        <Route path="/teams" element={<AllTeamsPage />} />
        <Route path="/teams/:id" element={<TeamDetailPage />} />
        <Route path="/team" element={<TeamPage />} />
        <Route path="/team/new" element={auth(<NewTeamPage />)} />
        <Route path="/team/join-request" element={auth(<JoinRequestPage />)} />
        <Route path="/team/invite-to-team" element={auth(<InviteToTeamPage />)} />
        <Route path="/team/requests" element={auth(<JoinRequestsListPage />)} />
        <Route path="/team/invitations" element={auth(<MyInvitationsPage />)} />
        <Route path="/team/invite" element={<Navigate to="/team/invitations" replace />} />
        <Route path="/team/public" element={<TeamPublicRedirect />} />

        {/* Vacancies */}
        <Route path="/vacancies" element={<VacanciesPage />} />
        <Route path="/vacancies/:id" element={<VacancyDetailPage />} />

        {/* Posts */}
        <Route path="/posts/new" element={auth(<NewPostPage />)} />
        <Route path="/posts/edit" element={auth(<EditPostPage />)} />
        <Route path="/posts/:id" element={<PostDetailPage />} />

        <Route path="*" element={<NotFoundPage />} />
      </Route>
    </Routes>
  );
}

export default App;
