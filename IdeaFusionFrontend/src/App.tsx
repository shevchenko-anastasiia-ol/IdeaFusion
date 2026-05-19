import { useEffect } from "react";
import {
  Routes,
  Route,
  Navigate,
  useNavigationType,
  useLocation,
} from "react-router-dom";

// Base pages (from component/component2 - most complete)
import Component3 from "./pages/Component3";
import Component1 from "./pages/Component1";
import Component2 from "./pages/Component2";
import Component4 from "./pages/Component4";

// Dashboard / Profile
import DashboardPage from "./pages/DashboardPage";
import ProfilePage from "./pages/ProfilePage";
import PersonalPage from "./pages/PersonalPage";
import AllUsersPage from "./pages/AllUsersPage";

// User public profile
import UserPublicPage from "./pages/UserPublicPage";

// Team pages
import TeamPage from "./pages/TeamPage";
import TeamDetailPage from "./pages/TeamDetailPage";
import NewTeamPage from "./pages/NewTeamPage";
import TeamInvitePage from "./pages/TeamInvitePage";
import JoinRequestPage from "./pages/JoinRequestPage";
import InviteToTeamPage from "./pages/InviteToTeamPage";
import JoinRequestsListPage from "./pages/JoinRequestsListPage";
import TeamPublicPage from "./pages/TeamPublicPage";
import MyInvitationsPage from "./pages/MyInvitationsPage";
import AllTeamsPage from "./pages/AllTeamsPage";

// Post pages
import NewPostPage from "./pages/NewPostPage";
import EditPostPage from "./pages/EditPostPage";
import PostDetailPage from "./pages/PostDetailPage";
import PortfolioPage from "./pages/PortfolioPage";
import AllPostsPage from "./pages/AllPostsPage";

// Auth
import LoginPage from "./pages/LoginPage";

function App() {
  const action = useNavigationType();
  const location = useLocation();
  const pathname = location.pathname;

  useEffect(() => {
    if (action !== "POP") {
      window.scrollTo(0, 0);
    }
  }, [action, pathname]);

  useEffect(() => {
    const titles: Record<string, string> = {
      "/home": "Головна",
      "/collaborations": "Колаборації",
      "/search": "Пошук",
      "/saved": "Збережене",
      "/dashboard": "Дашборд",
      "/profile": "Профіль",
      "/portfolio": "Портфоліо",
      "/team": "Команда",
      "/team/new": "Нова команда",
      "/team/invite": "Запросити до команди",
      "/team/join-request": "Запит на вступ",
      "/team/invite-to-team": "Запрошення",
      "/team/join": "Приєднатись до команди",
      "/posts/new": "Новий пост",
      "/posts/edit": "Редагування посту",
      
    };
    document.title = titles[pathname] || "IdeaFusion";
  }, [pathname]);

  return (
    <Routes>
      {/* Auth */}
      <Route path="/login" element={<LoginPage />} />

      {/* Main navigation */}
      <Route path="/" element={<Navigate to="/home" replace />} />
      <Route path="/home" element={<Component1 />} />
      <Route path="/collaborations" element={<Component3 />} />
      <Route path="/search" element={<Component2 />} />
      <Route path="/saved" element={<Component4 />} />

      {/* User profile */}
      <Route path="/dashboard" element={<DashboardPage />} />
      <Route path="/profile" element={<ProfilePage />} />
      <Route path="/portfolio" element={<PortfolioPage />} />
      <Route path="/personal" element={<PersonalPage />} />
      <Route path="/users"  element={<AllUsersPage />} />
      <Route path="/users/:id" element={<UserPublicPage />} />

      {/* Team management */}
      <Route path="/team" element={<TeamPage />} />
      <Route path="/team/new" element={<NewTeamPage />} />
      <Route path="/team/invite" element={<TeamInvitePage />} />
      <Route path="/team/join-request" element={<JoinRequestPage />} />
      <Route path="/team/invite-to-team" element={<InviteToTeamPage />} />
      <Route path="/team/requests" element={<JoinRequestsListPage />} />
      <Route path="/team/public" element={<TeamPublicPage />} />
      <Route path="/team/invitations" element={<MyInvitationsPage />} />
      <Route path="/teams" element={<AllTeamsPage />} />
      <Route path="/teams/:id" element={<TeamDetailPage />} />

      {/* Posts */}
      <Route path="/posts/new" element={<NewPostPage />} />
      <Route path="/posts/edit" element={<EditPostPage />} />
      <Route path="/posts/:id" element={<PostDetailPage />} />
      <Route path="/posts"  element={<AllPostsPage />} />
    </Routes>
  );
}

export default App;
