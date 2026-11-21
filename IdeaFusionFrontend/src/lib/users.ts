import { useEffect, useState } from "react";
import { api } from "../api/client";
import type { UserDto } from "../api/types";

/**
 * Looks up people by identity id, one `GET /api/users/{id}` per person.
 *
 * The list endpoint (`GET /api/users`) is administrator-only, so pages that only need
 * a name and an avatar for a few ids resolve them individually instead. Results are
 * cached for the session; a failed lookup is remembered as missing.
 */
const cache = new Map<string, Promise<UserDto | null>>();

export function fetchUser(id: string): Promise<UserDto | null> {
  let pending = cache.get(id);
  if (!pending) {
    pending = api.get<UserDto>(`/api/users/${encodeURIComponent(id)}`).catch(() => null);
    cache.set(id, pending);
  }
  return pending;
}

export function useUsers(ids: string[]): Record<string, UserDto> {
  const [map, setMap] = useState<Record<string, UserDto>>({});
  const key = [...new Set(ids.filter(Boolean))].sort().join(",");
  useEffect(() => {
    if (!key) return;
    let cancelled = false;
    Promise.all(key.split(",").map(id => fetchUser(id).then(u => [id, u] as const))).then(pairs => {
      if (cancelled) return;
      const next: Record<string, UserDto> = {};
      for (const [id, u] of pairs) if (u) next[id] = u;
      setMap(next);
    });
    return () => {
      cancelled = true;
    };
  }, [key]);
  return map;
}

export function userDisplayName(u: UserDto | undefined | null, fallback = ""): string {
  if (!u) return fallback;
  return u.fullName || u.userName || u.email?.split("@")[0] || fallback;
}

/** One person by identity id (cached); null until loaded or when the lookup fails. */
export function useUser(id: string | null | undefined): UserDto | null {
  const [user, setUser] = useState<UserDto | null>(null);
  useEffect(() => {
    setUser(null);
    if (!id) return;
    let cancelled = false;
    void fetchUser(id).then(u => {
      if (!cancelled) setUser(u);
    });
    return () => {
      cancelled = true;
    };
  }, [id]);
  return user;
}

/**
 * How a post's personal author is shown: the name on top and, smaller, the email.
 * The post itself only stores a user name, which for many accounts is their email,
 * so the real name comes from Identity when the author has an identity id.
 */
export function useAuthorDisplay(author: { userName: string; externalId?: string } | null | undefined): {
  name: string;
  email?: string;
} {
  const profile = useUser(author?.externalId);
  const stored = author?.userName ?? "";
  const storedIsEmail = stored.includes("@");
  const name =
    profile?.fullName?.trim() ||
    (profile?.userName && !profile.userName.includes("@") ? profile.userName : "") ||
    (storedIsEmail ? stored.split("@")[0] : stored) ||
    "Невідомий автор";
  const email = profile?.email ?? (storedIsEmail ? stored : undefined);
  return { name, email: email && email !== name ? email : undefined };
}
