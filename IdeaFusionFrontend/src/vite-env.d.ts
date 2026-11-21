/// <reference types="vite/client" />

interface ImportMetaEnv {
  /** Base URL of the Keycloak server, e.g. http://localhost:8080 (no trailing slash). */
  readonly VITE_KEYCLOAK_URL: string;
  /** Realm that holds the IdeaFusion roles. */
  readonly VITE_KEYCLOAK_REALM: string;
  /** Public SPA client id, registered with redirect uri http://localhost:5173. */
  readonly VITE_KEYCLOAK_CLIENT_ID: string;
}

interface ImportMeta {
  readonly env: ImportMetaEnv;
}
