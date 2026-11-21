import type { ReactNode } from "react";
import { Link } from "react-router-dom";
import type { TeamEntity } from "../api/types";
import { teamStatusLabel } from "../api/types";
import { Avatar, Badge, EmptyState, Loading, Status } from "../ui";
import { IconUsers } from "../ui/icons";
import { teamStatusTone } from "../lib/status";
import { plural } from "../lib/plural";
import sh from "../pages/shared.module.css";

/** The teams a person is in, with what they do there: owner, deputy or their position. */
export default function UserTeams({
  teams,
  userId,
  loading,
  empty,
}: {
  teams: TeamEntity[];
  userId: string;
  loading?: boolean;
  empty: { title: string; action?: ReactNode };
}) {
  if (loading) return <Loading />;
  if (teams.length === 0) return <EmptyState icon={<IconUsers />} title={empty.title} action={empty.action} />;

  return (
    <ul className={sh.list}>
      {teams.map(t => {
        const me = t.members.find(m => m.user.userId === userId);
        const role = me?.role === "Owner" ? "лідер" : me?.role === "Leader" ? "заступник" : me?.position;
        return (
          <li key={t.id}>
            <Link to={`/teams/${t.id}`} className={`${sh.row} ${sh.rowLink}`} style={{ color: "inherit", textDecoration: "none" }}>
              <Avatar name={t.name} src={t.avatarUrl} seed={t.id} size={36} square />
              <div className={sh.rowMain}>
                <span className={sh.rowTitle}>{t.name}</span>
                <span className={sh.mono}>
                  {t.category} · {t.members.length} {plural(t.members.length, ["учасник", "учасники", "учасників"])}
                </span>
              </div>
              {role && <Badge tone={me?.role === "Owner" ? "accent" : undefined}>{role}</Badge>}
              <Status tone={teamStatusTone(t.status)}>{teamStatusLabel(t.status).toLowerCase()}</Status>
            </Link>
          </li>
        );
      })}
    </ul>
  );
}
