import { createContext, useContext, useEffect, useState, type ReactNode } from "react";

export interface Crumb {
  label: string;
  to?: string;
}

interface PageMetaValue {
  crumbs: Crumb[];
  setCrumbs: (c: Crumb[]) => void;
}

const PageMetaContext = createContext<PageMetaValue | null>(null);

export function PageMetaProvider({ children }: { children: ReactNode }) {
  const [crumbs, setCrumbs] = useState<Crumb[]>([]);
  return <PageMetaContext.Provider value={{ crumbs, setCrumbs }}>{children}</PageMetaContext.Provider>;
}

/**
 * Sets the document title and the path shown in the top bar.
 * `crumbs` lists the parents; the page itself is appended as the last crumb.
 */
// eslint-disable-next-line react-refresh/only-export-components
export function usePageMeta(title: string, parents: Crumb[] = []) {
  const ctx = useContext(PageMetaContext);
  const key = JSON.stringify(parents);
  useEffect(() => {
    document.title = title ? `${title} · IdeaFusion` : "IdeaFusion";
    ctx?.setCrumbs([...(JSON.parse(key) as Crumb[]), { label: title }]);
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [title, key]);
}

// eslint-disable-next-line react-refresh/only-export-components
export function useCrumbs(): Crumb[] {
  return useContext(PageMetaContext)?.crumbs ?? [];
}
