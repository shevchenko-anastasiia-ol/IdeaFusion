import Keycloak, {
  type KeycloakConfig,
  type KeycloakInitOptions,
} from 'keycloak-js';

/**
 * The three Vite variables this app needs. They are public by design: this is a
 * public SPA client (PKCE, no client secret), so these values are visible in the
 * bundle either way. Secrets belong in the backend, never here.
 */
const KC_URL    = import.meta.env.VITE_KEYCLOAK_URL as string | undefined;
const KC_REALM  = import.meta.env.VITE_KEYCLOAK_REALM as string | undefined;
const KC_CLIENT = import.meta.env.VITE_KEYCLOAK_CLIENT_ID as string | undefined;

/** Realm roles, matching ServiceDefaults/Security/IdeaFusionRoles.cs. */
export const ROLE_AUTHOR = 'author';
export const ROLE_MODERATOR = 'moderator';
export const ROLE_ADMINISTRATOR = 'administrator';

export const ALL_ROLES = [ROLE_AUTHOR, ROLE_MODERATOR, ROLE_ADMINISTRATOR] as const;
export type IdeaFusionRole = (typeof ALL_ROLES)[number];

/** Moderator is composite: an administrator is a moderator. */
export const MODERATOR_OR_ABOVE: readonly string[] = [ROLE_MODERATOR, ROLE_ADMINISTRATOR];
export const ADMINISTRATOR_ONLY: readonly string[] = [ROLE_ADMINISTRATOR];

function readConfig() {
  if (!KC_URL || !KC_REALM || !KC_CLIENT) {
    throw new Error(
      'Keycloak is not configured. Set VITE_KEYCLOAK_URL, VITE_KEYCLOAK_REALM and ' +
      'VITE_KEYCLOAK_CLIENT_ID (see .env.example) and restart the dev server.',
    );
  }
  // A trailing slash here produces a double slash in the issuer, which makes
  // every token fail validation.
  return {
    url: KC_URL.replace(/\/+$/, ''),
    realm: KC_REALM,
    clientId: KC_CLIENT,
  };
}

const config = readConfig();

/** Where Keycloak sends the user for self-service tasks we do not implement. */
export const accountConsoleUrl = `${config.url}/realms/${config.realm}/account/`;
export const resetCredentialsUrl = `${config.url}/realms/${config.realm}/login-actions/reset-credentials`;

/**
 * Authorization Code + PKCE. There is deliberately no secret: `ideafusion-frontend`
 * is a public client, so `responseType` stays `code` and `pkceMethod` is S256.
 */
const kcConfig: KeycloakConfig = {
  url: config.url,
  realm: config.realm,
  clientId: config.clientId,
};

let instance: Keycloak | null = null;
let initPromise: Promise<boolean> | null = null;

/**
 * The one Keycloak instance for the whole app.
 *
 * The adapter is left at the default, which keeps the token and the refresh
 * bookkeeping in sessionStorage rather than localStorage: anything an injected
 * script can read is equally reachable either way, and sessionStorage at least
 * dies with the tab.
 */
export function getKeycloak(): Keycloak {
  if (!instance) instance = new Keycloak(kcConfig);
  return instance;
}

const initOptions: KeycloakInitOptions = {
  // Resume an SSO session silently on load. If there is none, we simply stay
  // logged out — no redirect, because /login owns that decision.
  onLoad: 'check-sso',
  flow: 'standard',
  pkceMethod: 'S256',
  scope: 'openid profile email',
  // The session-status iframe depends on third-party cookies, which modern
  // browsers block by default; leaving it off keeps refresh working without them.
  checkLoginIframe: false,
  silentCheckSsoRedirectUri: `${window.location.origin}/silent-check-sso.html`,
};

/**
 * Idempotent. keycloak-js refuses a second init() on the same instance, and
 * React strict mode mounts effects twice, so the first promise is cached.
 */
export function initKeycloak(): Promise<boolean> {
  const pending = initPromise;
  if (pending) return pending;

  const attempt: Promise<boolean> = getKeycloak()
    .init(initOptions)
    .then(authenticated => authenticated === true);

  initPromise = attempt;

  // A failed init must not be cached, otherwise retrying without a page reload is
  // impossible. Comparing against the stored reference avoids clobbering a newer
  // attempt. The caller still receives the rejection.
  attempt.catch(() => {
    if (initPromise === attempt) initPromise = null;
  });

  return attempt;
}

export function isAuthenticated(): boolean {
  return getKeycloak().authenticated === true;
}

/**
 * The current access token without attempting a refresh, or null when logged out.
 */
export function getToken(): string | null {
  const kc = getKeycloak();
  if (!kc.authenticated || !kc.token) return null;
  return kc.token;
}

/**
 * Returns a token valid for at least `minValidity` seconds, refreshing silently if
 * needed, or null when there is no session. This is the only refresh path in the app.
 *
 * keycloak-js signals "was refreshed" rather than returning the token itself; the
 * token always has to be read back off the instance afterwards.
 */
export async function updateToken(minValidity = 5): Promise<string | null> {
  const kc = getKeycloak();
  if (!kc.authenticated) return null;
  try {
    await kc.updateToken(minValidity);
  } catch {
    // A rejected refresh means the session is gone; the caller decides what next.
    return null;
  }
  return kc.token ?? null;
}

export function getSubject(): string | null {
  const parsed = getKeycloak().tokenParsed;
  return typeof parsed?.sub === 'string' ? parsed.sub : null;
}

function rolesFromParsedToken(): string[] {
  const parsed = getKeycloak().tokenParsed as
    | {
        realm_access?: { roles?: unknown };
        resource_access?: Record<string, { roles?: unknown }>;
        role?: unknown;
      }
    | undefined;
  if (!parsed) return [];

  const out = new Set<string>();

  // Standard Keycloak shape.
  const realmRoles = parsed.realm_access?.roles;
  if (Array.isArray(realmRoles)) {
    for (const r of realmRoles) if (typeof r === 'string') out.add(r);
  }

  // The ideafusion-roles client scope also flattens the roles into a bare
  // `role` claim, which is the one the API's policies read.
  if (Array.isArray(parsed.role)) {
    for (const r of parsed.role) if (typeof r === 'string') out.add(r);
  } else if (typeof parsed.role === 'string') {
    out.add(parsed.role);
  }

  // Client roles, in case a role is scoped to ideafusion_api instead.
  const clientRoles = parsed.resource_access?.[config.clientId]?.roles;
  if (Array.isArray(clientRoles)) {
    for (const r of clientRoles) if (typeof r === 'string') out.add(r);
  }

  return [...out];
}

export function getRoles(): string[] {
  if (!isAuthenticated()) return [];
  return rolesFromParsedToken();
}

export function hasAnyRole(accepted: readonly string[]): boolean {
  const roles = getRoles();
  return accepted.some(r => roles.includes(r));
}

export function isModerator(): boolean {
  return hasAnyRole(MODERATOR_OR_ABOVE);
}

export function isAdministrator(): boolean {
  return hasAnyRole(ADMINISTRATOR_ONLY);
}

export interface LoginOptions {
  /** Sends the user straight to a federated IdP instead of the login form. */
  idpHint?: string;
  redirectUri?: string;
}

export async function login(opts: LoginOptions = {}): Promise<void> {
  await getKeycloak().login({
    redirectUri: opts.redirectUri ?? window.location.origin,
    idpHint: opts.idpHint,
  });
  // keycloak-js navigates away here; this never resolves on the happy path.
}

/** Opens Keycloak's sign-up form (the realm allows self-registration). */
export async function register(redirectUri?: string): Promise<void> {
  await getKeycloak().register({ redirectUri: redirectUri ?? window.location.href });
}

export async function logout(redirectUri?: string): Promise<void> {
  const kc = getKeycloak();
  // Drop the local session even if the call to the end-session endpoint fails,
  // otherwise a logged-out user still looks authenticated after a reload.
  try {
    await kc.logout({ redirectUri: redirectUri ?? `${window.location.origin}/login` });
  } finally {
    kc.clearToken();
  }
}

/** Test seam: drops the cached instance so a new config can be applied. */
export function resetKeycloakForTests(): void {
  instance = null;
  initPromise = null;
}
