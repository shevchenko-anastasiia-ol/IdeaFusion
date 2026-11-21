/**
 * Display labels for the content tags. The API stores English slugs; the UI
 * shows Ukrainian group names. One map for the whole app.
 */
export const TAG_GROUPS: Record<string, string[]> = {
  "Дизайн": ["design", "branding"],
  "Музика": ["music"],
  "Фото": ["photography"],
  "Анімація": ["animation"],
  "Арт": ["art", "illustration", "street-art"],
  "Цифровий": ["digital"],
  "Gamedev": ["gamedev"],
  "3D": ["3d"],
  "UI/UX": ["ui-ux"],
  "Архітектура": ["architecture"],
  "Колаб": ["collab"],
  "Мода": ["fashion"],
  "Фільм": ["film"],
  "Поезія": ["poetry"],
  "Традиційне": ["traditional"],
  "В процесі": ["wip"],
  "Письменство": ["writing"],
};

/** The filter row on the feed. */
export const FEED_FILTERS = ["Дизайн", "Музика", "Фото", "Анімація", "Арт", "Gamedev", "3D", "UI/UX"];

const LABEL_BY_SLUG: Record<string, string> = {};
for (const [label, slugs] of Object.entries(TAG_GROUPS)) {
  for (const slug of slugs) LABEL_BY_SLUG[slug] = label;
}

export function tagLabel(raw: string): string {
  return LABEL_BY_SLUG[raw.toLowerCase()] ?? raw.charAt(0).toUpperCase() + raw.slice(1);
}

/** True when any of `tags` belongs to the group `label`. */
export function matchesTagGroup(tags: string[], label: string): boolean {
  const slugs = TAG_GROUPS[label] ?? [label.toLowerCase()];
  return tags.some(t => slugs.includes(t.toLowerCase()));
}
