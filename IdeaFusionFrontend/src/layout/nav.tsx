import type { ReactNode } from "react";
import {
  IconBookmark,
  IconBriefcase,
  IconCopy,
  IconFeed,
  IconGrid,
  IconSearch,
  IconShield,
  IconSpark,
  IconUserCog,
  IconUsers,
} from "../ui/icons";

export interface NavItem {
  label: string;
  to: string;
  icon: ReactNode;
  /** Path prefixes that also mark this item active. */
  match: string[];
  requires?: "auth" | "moderator" | "administrator";
}

export interface NavGroup {
  /** Shown above the group; empty for the main sections. */
  label: string;
  items: NavItem[];
}

export const NAV_GROUPS: NavGroup[] = [
  {
    label: "",
    items: [
      { label: "Стрічка", to: "/home", icon: <IconFeed />, match: ["/home", "/posts"] },
      { label: "Для вас", to: "/for-you", icon: <IconSpark />, match: ["/for-you"], requires: "auth" },
      { label: "Пошук", to: "/search", icon: <IconSearch />, match: ["/search"] },
      { label: "Вакансії", to: "/vacancies", icon: <IconBriefcase />, match: ["/vacancies"] },
    ],
  },
  {
    label: "",
    items: [
      { label: "Команди", to: "/collaborations", icon: <IconUsers />, match: ["/collaborations", "/team", "/teams"] },
      { label: "Дашборд", to: "/dashboard", icon: <IconGrid />, match: ["/dashboard"], requires: "auth" },
      { label: "Збережене", to: "/saved", icon: <IconBookmark />, match: ["/saved"], requires: "auth" },
    ],
  },
  {
    label: "Модерація",
    items: [
      { label: "Підозрілі пости", to: "/moderation/copies", icon: <IconCopy />, match: ["/moderation/copies"], requires: "moderator" },
      { label: "Журнал модерації", to: "/moderation", icon: <IconShield />, match: ["/moderation"], requires: "moderator" },
      { label: "Користувачі", to: "/users", icon: <IconUserCog />, match: ["/users"], requires: "administrator" },
    ],
  },
];

/** Picks the single most specific nav item for a path. */
export function activeNavTo(pathname: string, items: NavItem[]): string | null {
  let best: { to: string; len: number } | null = null;
  for (const item of items) {
    for (const m of item.match) {
      const hit = pathname === m || pathname.startsWith(m + "/");
      if (hit && (!best || m.length > best.len)) best = { to: item.to, len: m.length };
    }
  }
  // /users/:id is a public profile, not the admin list.
  if (best?.to === "/users" && pathname !== "/users") return null;
  return best?.to ?? null;
}
