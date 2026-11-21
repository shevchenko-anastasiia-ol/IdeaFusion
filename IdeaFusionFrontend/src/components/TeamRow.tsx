import type { ReactNode } from "react";
import { Link } from "react-router-dom";
import type { TeamEntity } from "../api/types";
import { teamStatusLabel } from "../api/types";
import { Avatar, AvatarStack, Status } from "../ui";
import { teamStatusTone } from "../lib/status";
import { plural } from "../lib/plural";
import s from "./TeamRow.module.css";

/** A team in a list: avatar, name, status, members and what roles it is looking for. */
export default function TeamRow({ team, actions }: { team: TeamEntity; actions?: ReactNode }) {
  const members = team.members ?? [];
  const roles = team.requiredRoles ?? [];
  return (
    <div className={s.row}>
      <Link to={`/teams/${team.id}`} className={s.main}>
        <Avatar name={team.name} src={team.avatarUrl} seed={team.id} size={44} square />
        <span className={s.text}>
          <span className={s.titleLine}>
            <span className={s.name}>{team.name}</span>
            <Status tone={teamStatusTone(team.status)}>{teamStatusLabel(team.status).toLowerCase()}</Status>
          </span>
          {team.description && <span className={s.desc}>{team.description}</span>}
          <span className={s.meta}>
            {team.category && <span>{team.category}</span>}
            <span>
              {members.length} {plural(members.length, ["учасник", "учасники", "учасників"])}
            </span>
            {team.openVacancyCount > 0 && <span className={s.open}>вакансій: {team.openVacancyCount}</span>}
            {roles.length > 0 && <span className={s.need}>шукають: {roles.slice(0, 2).map(r => r.role).join(", ")}{roles.length > 2 ? "…" : ""}</span>}
            {(team.tags ?? []).slice(0, 3).map(t => (
              <span key={t} className={s.tag}>#{t}</span>
            ))}
          </span>
        </span>
      </Link>
      <div className={s.side}>
        {members.length > 0 && (
          <AvatarStack people={members.map(m => ({ id: m.user.userId, name: m.user.username, src: m.user.avatarUrl }))} max={4} size={26} />
        )}
        {actions}
      </div>
    </div>
  );
}
