function getToken(): string | null {
  return localStorage.getItem('accessToken');
}

let _isRefreshing = false;
let _waitQueue: Array<(token: string | null) => void> = [];

async function tryRefresh(): Promise<string | null> {
  const refreshToken = localStorage.getItem('refreshToken');
  if (!refreshToken) return null;
  try {
    const res = await fetch('/api/auth/refresh', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ refreshToken }),
    });
    if (!res.ok) {
      localStorage.removeItem('accessToken');
      localStorage.removeItem('refreshToken');
      return null;
    }
    const data = await res.json();
    localStorage.setItem('accessToken', data.accessToken);
    if (data.refreshToken) localStorage.setItem('refreshToken', data.refreshToken);
    return data.accessToken as string;
  } catch {
    localStorage.removeItem('accessToken');
    localStorage.removeItem('refreshToken');
    return null;
  }
}

async function getRefreshedToken(): Promise<string | null> {
  if (_isRefreshing) {
    return new Promise<string | null>(resolve => { _waitQueue.push(resolve); });
  }
  _isRefreshing = true;
  const newToken = await tryRefresh();
  _isRefreshing = false;
  _waitQueue.forEach(resolve => resolve(newToken));
  _waitQueue = [];
  return newToken;
}

async function request<T>(path: string, options: RequestInit = {}): Promise<T> {
  const token = getToken();
  const headers: Record<string, string> = {
    'Content-Type': 'application/json',
    ...(options.headers as Record<string, string>),
  };
  if (token) headers['Authorization'] = `Bearer ${token}`;

  let res = await fetch(path, { ...options, headers });

  if (res.status === 401) {
    const newToken = await getRefreshedToken();
    if (newToken) {
      headers['Authorization'] = `Bearer ${newToken}`;
      res = await fetch(path, { ...options, headers });
    } else {
      window.location.href = '/login';
      throw new Error('Сесія закінчилась. Будь ласка, увійдіть знову.');
    }
  }

  if (!res.ok) {
    let message = `HTTP ${res.status}`;
    try {
      const raw = await res.text();
      const body = raw ? JSON.parse(raw) : null;
      const base = body?.title ?? body?.message
        ?? (Array.isArray(body) ? body.map((e: any) => e.description).join('; ') : null)
        ?? message;
      const detail = body?.details ? ` (${body.details})` : '';
      message = base + detail;
    } catch {}
    throw new Error(message);
  }

  const text = await res.text();
  if (!text) return undefined as T;
  return JSON.parse(text) as T;
}

export const api = {
  get:    <T>(path: string)                    => request<T>(path),
  post:   <T>(path: string, body?: unknown)    => request<T>(path, { method: 'POST',   body: JSON.stringify(body) }),
  put:    <T>(path: string, body?: unknown)    => request<T>(path, { method: 'PUT',    body: JSON.stringify(body) }),
  patch:  <T>(path: string, body?: unknown)    => request<T>(path, { method: 'PATCH',  body: JSON.stringify(body) }),
  delete: <T>(path: string, body?: unknown)    => request<T>(path, { method: 'DELETE', body: body ? JSON.stringify(body) : undefined }),
};