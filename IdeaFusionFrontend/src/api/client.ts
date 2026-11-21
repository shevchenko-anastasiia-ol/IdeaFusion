import { getToken, updateToken } from '../auth/keycloak';

/**
 * How far ahead of expiry a token is considered still good. keycloak-js refreshes
 * silently when the remaining lifetime is under this, so a 401 almost always
 * means the session is genuinely gone rather than the token being stale.
 */
const MIN_TOKEN_VALIDITY_SECONDS = 5;

async function authHeader(): Promise<Record<string, string>> {
  const token = await updateToken(MIN_TOKEN_VALIDITY_SECONDS);
  return token ? { Authorization: `Bearer ${token}` } : {};
}

async function request<T>(path: string, options: RequestInit = {}): Promise<T> {
  const headers: Record<string, string> = {
    'Content-Type': 'application/json',
    ...(await authHeader()),
    ...(options.headers as Record<string, string> | undefined),
  };

  const res = await fetch(path, { ...options, headers });

  if (res.status === 401) {
    // One retry: the token may have been revoked server-side between our refresh
    // and this call, and updateToken is a no-op while the session is still good.
    const retried = await updateToken(-1);
    if (retried && !path.startsWith('/api/auth/me')) {
      const retryHeaders: Record<string, string> = {
        ...headers,
        Authorization: `Bearer ${retried}`,
      };
      const retry = await fetch(path, { ...options, headers: retryHeaders });
      if (retry.ok) return parseBody<T>(retry);
      throw await toError(retry);
    }
    // The session is gone. Leave it to the route guard to send the user to login.
    throw await toError(res);
  }

  if (!res.ok) throw await toError(res);

  return parseBody<T>(res);
}

async function parseBody<T>(res: Response): Promise<T> {
  const text = await res.text();
  if (!text) return undefined as T;
  // ASP.NET sends a bare string result (e.g. the URL from /api/media/upload) as
  // text/plain, without JSON quotes; only JSON bodies go through JSON.parse.
  const type = res.headers.get('content-type') ?? '';
  if (!type.includes('json')) return text as T;
  return JSON.parse(text) as T;
}

async function toError(res: Response): Promise<Error> {
  let message = `HTTP ${res.status}`;
  try {
    const raw = await res.text();
    const body = raw ? JSON.parse(raw) : null;
    const base = body?.title ?? body?.message
      ?? (Array.isArray(body) ? body.map((e: { description?: string }) => e.description).join('; ') : null)
      ?? message;
    const detail = body?.details ? ` (${body.details})` : '';
    message = base + detail;
  } catch {
    // тіло помилки не є валідним JSON, лишаємо повідомлення за замовчуванням
  }
  return new Error(message);
}

/**
 * Multipart uploads must not carry a JSON content-type: the browser has to add
 * the multipart boundary itself.
 */
export async function upload<T>(path: string, form: FormData, method = 'POST'): Promise<T> {
  const res = await fetch(path, {
    method,
    headers: await authHeader(),
    body: form,
  });
  if (!res.ok) throw await toError(res);
  return parseBody<T>(res);
}

/** Used by endpoints that are readable anonymously, so a missing token is fine. */
export function peekToken(): string | null {
  return getToken();
}

export const api = {
  get:    <T>(path: string)                    => request<T>(path),
  post:   <T>(path: string, body?: unknown)    => request<T>(path, { method: 'POST',   body: JSON.stringify(body) }),
  put:    <T>(path: string, body?: unknown)    => request<T>(path, { method: 'PUT',    body: JSON.stringify(body) }),
  patch:  <T>(path: string, body?: unknown)    => request<T>(path, { method: 'PATCH',  body: JSON.stringify(body) }),
  delete: <T>(path: string, body?: unknown)    => request<T>(path, { method: 'DELETE', body: body ? JSON.stringify(body) : undefined }),
  upload,
};
