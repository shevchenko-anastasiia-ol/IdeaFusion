import { useEffect, useState } from "react";
import { copyCheckApi, type CopyCheckRecord } from "../api/copycheck";
import { useAuth } from "../context/AuthContext";

/**
 * Image copy checks run when a post is published. There is no separate review page:
 * moderators see open matches right on the posts they concern. This hook loads the open
 * red/yellow records once per minute (shared by every card on the page) and groups them
 * by post. Everyone else gets an empty map and no request is made.
 */
const TTL_MS = 60_000;
const LIMIT = 500;

let cached: { at: number; data: Promise<CopyCheckRecord[]> } | null = null;
const listeners = new Set<() => void>();

function load(force = false): Promise<CopyCheckRecord[]> {
  if (!force && cached && Date.now() - cached.at < TTL_MS) return cached.data;
  const data = copyCheckApi.queue({ zones: ["red", "yellow"], status: "open", limit: LIMIT }).catch(() => [] as CopyCheckRecord[]);
  cached = { at: Date.now(), data };
  return data;
}

/** Forces the next read to refetch and tells mounted hooks to reload (after a decision). */
export function invalidateCopyFlags() {
  cached = null;
  listeners.forEach(l => l());
}

export function useOpenCopyFlags(): Map<number, CopyCheckRecord[]> {
  const { isModerator } = useAuth();
  const [byPost, setByPost] = useState<Map<number, CopyCheckRecord[]>>(new Map());
  const [version, setVersion] = useState(0);

  useEffect(() => {
    const bump = () => setVersion(v => v + 1);
    listeners.add(bump);
    return () => {
      listeners.delete(bump);
    };
  }, []);

  useEffect(() => {
    if (!isModerator) {
      setByPost(new Map());
      return;
    }
    let cancelled = false;
    load().then(records => {
      if (cancelled) return;
      const map = new Map<number, CopyCheckRecord[]>();
      for (const r of records) {
        if (r.post_id == null) continue;
        map.set(r.post_id, [...(map.get(r.post_id) ?? []), r]);
      }
      setByPost(map);
    });
    return () => {
      cancelled = true;
    };
  }, [isModerator, version]);

  return byPost;
}

/** All checks (any status) that concern one post — for the moderator block on the post page. */
export async function checksForPost(postId: number): Promise<CopyCheckRecord[]> {
  const records = await copyCheckApi.queue({ zones: ["red", "yellow"], status: "", limit: LIMIT });
  return records.filter(r => r.post_id === postId);
}
