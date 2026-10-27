import { api } from "./client";

const BASE = "/api/search";

export type SearchKind = "post" | "team" | "vacancy" | "user";
export type SearchSort = "relevance" | "new" | "popular";
export type TagMode = "any" | "all";

/** Filters of a search, as the API stores them in history and saved searches. */
export interface SearchFilters {
  kind?: SearchKind;
  category?: string;
  tags?: string[];
  tagMode?: TagMode;
  from?: string;
  to?: string;
  minLikes?: number;
  hasOpenVacancies?: boolean;
  role?: string;
  author?: string;
  team?: string;
  sort?: SearchSort;
}

export interface SearchParams extends SearchFilters {
  q?: string;
  cursor?: string;
  pageSize?: number;
}

export interface SearchHit {
  id: string;
  kind: SearchKind;
  sourceId: string;
  title: string;
  subtitle?: string;
  snippet?: string;
  tags: string[];
  categories: string[];
  imageUrl?: string;
  ownerId?: string;
  teamId?: string;
  createdAt: string;
  likes: number;
  saves: number;
  views: number;
  openVacancies: number;
  score: number;
}

export interface FacetValue { value: string; count: number }
export interface TagFacet { key: string; label: string; count: number }

export interface SearchResponse {
  query: string;
  didYouMean?: string;
  total: number;
  sort: SearchSort;
  facets: { kinds: Partial<Record<SearchKind, number>>; categories: FacetValue[]; tags: TagFacet[] };
  items: SearchHit[];
  nextCursor?: string;
}

export interface TagSuggestion { key: string; label: string; documents: number }
export interface RoleSuggestion { label: string; vacancies: number; teams: number; people: number }
export interface CategoryCount { category: string; documents: number }

export interface SuggestResponse {
  completions: string[];
  documents: SearchHit[];
  tags: TagSuggestion[];
}

export interface HistoryEntry {
  id: number;
  query: string;
  filters: SearchFilters;
  resultsCount: number;
  searchedAt: string;
}

export interface SavedSearch {
  id: string;
  name: string;
  query: string;
  filters: SearchFilters;
  notify: boolean;
  unread: number;
  createdAt: string;
  updatedAt: string;
}

export interface SavedSearchInput {
  name: string;
  query?: string;
  filters?: SearchFilters;
  notify: boolean;
}

export interface SearchNotification {
  id: number;
  savedSearchId: string;
  savedSearchName: string;
  documentId: string;
  kind: SearchKind;
  title: string;
  createdAt: string;
  read: boolean;
}

export interface FeedReason {
  kind: "tag" | "category" | "author" | "specialization" | "popular";
  value?: string;
  label?: string;
  text: string;
}

export interface FeedItem { post: SearchHit; reason: FeedReason; score: number }

export interface FeedResponse {
  variant: string;
  page: number;
  pageSize: number;
  hasMore: boolean;
  asOf: string;
  items: FeedItem[];
}

export interface FeedEvent { documentId: string; type: "impression" | "click"; position: number }

export const KIND_LABEL: Record<SearchKind, string> = { post: "Пост", team: "Команда", vacancy: "Вакансія", user: "Людина" };

export const SEARCH_KINDS: { kind?: SearchKind; label: string }[] = [
  { label: "Усе" },
  { kind: "post", label: "Пости" },
  { kind: "team", label: "Команди" },
  { kind: "vacancy", label: "Вакансії" },
  { kind: "user", label: "Люди" },
];

/** Where a hit opens in the app. */
export function hitLink(hit: Pick<SearchHit, "kind" | "sourceId">): string {
  switch (hit.kind) {
    case "post": return `/posts/${hit.sourceId}`;
    case "team": return `/teams/${hit.sourceId}`;
    case "vacancy": return `/vacancies/${hit.sourceId}`;
    default: return `/users/${hit.sourceId}`;
  }
}

/** The same keys as the API query string, so a page URL can be passed through as is. */
export function toQueryString(params: SearchParams): string {
  const q = new URLSearchParams();
  for (const [key, value] of Object.entries(params)) {
    if (value === undefined || value === null || value === "" || value === false) continue;
    if (key === "tags") (value as string[]).forEach(t => q.append("tag", t));
    else q.set(key, String(value));
  }
  return q.toString();
}

/** The inverse of {@link toQueryString}. */
export function fromQueryString(search: string): SearchParams {
  const q = new URLSearchParams(search);
  const num = (k: string) => (q.get(k) ? Number(q.get(k)) : undefined);
  return {
    q: q.get("q") ?? undefined,
    kind: (q.get("kind") as SearchKind) ?? undefined,
    category: q.get("category") ?? undefined,
    tags: q.getAll("tag").length ? q.getAll("tag") : undefined,
    tagMode: (q.get("tagMode") as TagMode) ?? undefined,
    from: q.get("from") ?? undefined,
    to: q.get("to") ?? undefined,
    minLikes: num("minLikes"),
    hasOpenVacancies: q.get("hasOpenVacancies") === "true" || undefined,
    role: q.get("role") ?? undefined,
    author: q.get("author") ?? undefined,
    team: q.get("team") ?? undefined,
    sort: (q.get("sort") as SearchSort) ?? undefined,
  };
}

export function filtersOf(params: SearchParams): SearchFilters {
  const filters: SearchParams = { ...params };
  delete filters.q;
  delete filters.cursor;
  delete filters.pageSize;
  return filters;
}

export const searchApi = {
  search: (params: SearchParams) => api.get<SearchResponse>(`${BASE}?${toQueryString(params)}`),
  suggest: (q: string, limit = 8) => api.get<SuggestResponse>(`${BASE}/suggest?q=${encodeURIComponent(q)}&limit=${limit}`),
  tags: (q: string, limit = 10) => api.get<TagSuggestion[]>(`${BASE}/tags?q=${encodeURIComponent(q)}&limit=${limit}`),
  roles: (q: string, limit = 10) => api.get<RoleSuggestion[]>(`${BASE}/roles?q=${encodeURIComponent(q)}&limit=${limit}`),
  categories: () => api.get<CategoryCount[]>(`${BASE}/categories`),
  /** A result was opened from the search page (0-based position), for the click-in-top-3 metric. */
  click: (kind: SearchKind, position: number) => api.post<void>(`${BASE}/clicks`, { kind, position }),

  history: (q = "", limit = 10) => api.get<HistoryEntry[]>(`${BASE}/history?q=${encodeURIComponent(q)}&limit=${limit}`),
  deleteHistory: (id: number) => api.delete<void>(`${BASE}/history/${id}`),
  clearHistory: () => api.delete<void>(`${BASE}/history`),

  saved: () => api.get<SavedSearch[]>(`${BASE}/saved`),
  createSaved: (input: SavedSearchInput) => api.post<SavedSearch>(`${BASE}/saved`, input),
  updateSaved: (id: string, input: SavedSearchInput) => api.put<SavedSearch>(`${BASE}/saved/${id}`, input),
  deleteSaved: (id: string) => api.delete<void>(`${BASE}/saved/${id}`),

  notifications: (unreadOnly = false, limit = 20) =>
    api.get<SearchNotification[]>(`${BASE}/notifications?unreadOnly=${unreadOnly}&limit=${limit}`),
  markRead: (ids: number[] = []) => api.post<{ marked: number }>(`${BASE}/notifications/read`, { ids }),

  feed: (page = 1, pageSize = 20, asOf?: string) =>
    api.get<FeedResponse>(`${BASE}/feed?page=${page}&pageSize=${pageSize}${asOf ? `&asOf=${encodeURIComponent(asOf)}` : ""}`),
  feedEvents: (variant: string, events: FeedEvent[]) => api.post<{ written: number }>(`${BASE}/feed/events`, { variant, events }),
};
