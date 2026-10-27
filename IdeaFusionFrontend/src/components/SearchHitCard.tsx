import { FunctionComponent, ReactNode } from "react";
import { Box } from "@mui/material";
import { useNavigate } from "react-router-dom";
import { KIND_LABEL, hitLink } from "../api/search";
import type { SearchHit } from "../api/search";

const KIND_COLOR: Record<string, string> = { post: "#7c5cfc", team: "#00c9a7", vacancy: "#ffb347", user: "#29b6f6" };

export type SearchHitCardProps = {
  hit: SearchHit;
  /** A line above the title, e.g. why the feed recommends this post. */
  note?: ReactNode;
  /** Called before navigating, e.g. to log a feed click. */
  onOpen?: () => void;
  onTagClick?: (tag: string) => void;
};

/** One search result or feed item of any kind: post, team, vacancy or person. */
const SearchHitCard: FunctionComponent<SearchHitCardProps> = ({ hit, note, onOpen, onTagClick }) => {
  const navigate = useNavigate();
  const color = KIND_COLOR[hit.kind] ?? "#7c5cfc";
  const open = () => { onOpen?.(); navigate(hitLink(hit)); };

  return (
    <Box component="article" onClick={open}
      sx={{ display: "flex", gap: "16px", p: "16px 18px", background: "#181818", border: "1px solid #222",
        borderRadius: "20px", cursor: "pointer", transition: "background .15s", "&:hover": { background: "#1e1e1e" } }}>
      {hit.imageUrl && (
        <Box component="img" src={hit.imageUrl} alt="" loading="lazy"
          sx={{ width: 84, height: 84, borderRadius: "14px", objectFit: "cover", flexShrink: 0 }} />
      )}
      <Box sx={{ minWidth: 0, flex: 1, display: "flex", flexDirection: "column", gap: "6px" }}>
        {note && <Box sx={{ fontSize: 12, color: "#a48fff" }}>{note}</Box>}
        <Box sx={{ display: "flex", alignItems: "center", gap: "10px", minWidth: 0 }}>
          <Box component="span" sx={{ fontSize: 11, px: "9px", py: "1px", borderRadius: "30px", flexShrink: 0,
            border: `1px solid ${color}`, color }}>
            {KIND_LABEL[hit.kind]}
          </Box>
          <Box component="h3" sx={{ m: 0, fontSize: 17, fontWeight: 700, color: "#fff",
            overflow: "hidden", textOverflow: "ellipsis", whiteSpace: "nowrap" }}>
            {hit.title}
          </Box>
        </Box>
        {hit.subtitle && <Box sx={{ fontSize: 13, color: "#888" }}>{hit.subtitle}</Box>}
        {hit.snippet && (
          <Box sx={{ fontSize: 14, color: "#aaa", display: "-webkit-box", WebkitLineClamp: 2,
            WebkitBoxOrient: "vertical", overflow: "hidden" }}>
            {hit.snippet}
          </Box>
        )}
        <Box sx={{ display: "flex", flexWrap: "wrap", alignItems: "center", gap: "6px", fontSize: 12, color: "#666" }}>
          {hit.tags.slice(0, 6).map(t => (
            <Box key={t} component="span"
              onClick={e => { if (onTagClick) { e.stopPropagation(); onTagClick(t); } }}
              sx={{ px: "8px", py: "1px", borderRadius: "30px", background: "#222", color: "#aaa",
                "&:hover": onTagClick ? { color: "#fff", background: "#2c2c2c" } : undefined }}>
              #{t}
            </Box>
          ))}
          <Box component="span" sx={{ ml: "auto", whiteSpace: "nowrap" }}>{meta(hit)}</Box>
        </Box>
      </Box>
    </Box>
  );
};

function meta(hit: SearchHit): string {
  const parts: string[] = [];
  if (hit.kind === "post") parts.push(`♥ ${hit.likes}`, `👁 ${hit.views}`);
  if (hit.kind === "team" && hit.openVacancies > 0) parts.push(`Вакансій: ${hit.openVacancies}`);
  if (hit.kind !== "user") parts.push(new Date(hit.createdAt).toLocaleDateString("uk-UA"));
  return parts.join(" · ");
}

export default SearchHitCard;
