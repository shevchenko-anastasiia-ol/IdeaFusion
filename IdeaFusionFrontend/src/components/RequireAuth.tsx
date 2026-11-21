import { FunctionComponent, ReactNode } from 'react';
import { Navigate, useLocation } from 'react-router-dom';
import { useAuth } from '../context/AuthContext';
import { Loading } from '../ui';

/**
 * Blocks a subtree until we know who the caller is.
 *
 * While the silent SSO check is still running we render nothing rather than
 * redirecting: bouncing a signed-in user to /login for a few hundred
 * milliseconds on every hard refresh is the classic way to make SSO feel broken.
 */
export const RequireAuth: FunctionComponent<{ children: ReactNode }> = ({ children }) => {
  const { isAuthenticated, loading } = useAuth();
  const location = useLocation();

  if (loading) {
    return (
      <Loading />
    );
  }

  if (!isAuthenticated) {
    // Remember where they were headed so login can send them back.
    return <Navigate to="/login" replace state={{ from: location.pathname + location.search }} />;
  }

  return <>{children}</>;
};

/**
 * Requires one of `roles`. Mirrors the API's policies, which stay the real
 * enforcement point — this only avoids rendering a page the user cannot use.
 *
 * `administrator` implies `moderator`; the composite lists are in
 * src/auth/keycloak.ts so both sides agree on what a role means.
 */
export const RequireRole: FunctionComponent<{ roles: readonly string[]; children: ReactNode }> = ({
  roles,
  children,
}) => {
  const { roles: held, loading } = useAuth();

  if (loading) {
    return (
      <Loading />
    );
  }

  if (!roles.some(r => held.includes(r))) {
    return <Navigate to="/home" replace />;
  }

  return <>{children}</>;
};

/** Convenience pairing for the common "signed in and at least a moderator" case. */
export const RequireModerator: FunctionComponent<{ children: ReactNode }> = ({ children }) => {
  const { isModerator, loading } = useAuth();

  if (loading) {
    return (
      <Loading />
    );
  }

  return isModerator ? <>{children}</> : <Navigate to="/home" replace />;
};

/** Administrator-only pages. */
export const RequireAdministrator: FunctionComponent<{ children: ReactNode }> = ({ children }) => {
  const { isAdministrator, loading } = useAuth();

  if (loading) {
    return (
      <Loading />
    );
  }

  return isAdministrator ? <>{children}</> : <Navigate to="/home" replace />;
};
