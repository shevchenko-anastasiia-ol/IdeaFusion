import { useCallback, useEffect, useMemo, useState } from "react";
import { useNavigate } from "react-router-dom";
import { teamsApi, collaborationRequestsApi } from "../api/teams";
import type { TeamEntity } from "../api/types";
import { teamStatusLabel } from "../api/types";
import { useAuth } from "../context/AuthContext";
import { Page } from "../layout/AppShell";
import { usePageMeta } from "../layout/PageMeta";
import { Button, ButtonLink, EmptyState, ErrorState, Loading, Menu, PageHeader, Pagination, Segmented, Tag, useDialogs, useToast } from "../ui";
import { IconCheck, IconPlus, IconTrash, IconUsers } from "../ui/icons";
import TeamRow from "../components/TeamRow";
import sh from "./shared.module.css";

const PAGE_SIZE = 10;
type StatusFilter = "all" | "У пошуку" | "Активна" | "Завершена";

function ApplyButton({ team }: { team: TeamEntity }) {
  const navigate = useNavigate();
  const { user } = useAuth();
  const toast = useToast();
  const [applying, setApplying] = useState(false);
  const [applied, setApplied] = useState(false);
  const openRole = team.requiredRoles[0]?.role ?? null;

  if (teamStatusLabel(team.status) === "Завершена") return null;
  if (user && team.members.some(m => m.user.userId === user.id)) return null;
  if (applied) {
    return (
      <span className={sh.mono} style={{ color: "var(--teal)", display: "inline-flex", alignItems: "center", gap: 4 }}>
        <IconCheck size={14} /> заявку подано
      </span>
    );
  }

  async function apply() {
    if (!user) {
      navigate("/login");
      return;
    }
    setApplying(true);
    try {
      await collaborationRequestsApi.create({ teamId: team.id, role: openRole ?? "Учасник" });
      setApplied(true);
      toast.success(`Заявку до «${team.name}» подано`);
    } catch (ex) {
      toast.fromError(ex, "Не вдалося подати заявку");
    } finally {
      setApplying(false);
    }
  }

  return (
    <Button size="sm" loading={applying} onClick={() => void apply()} title={openRole ? `Роль: ${openRole}` : "Роль: Учасник"}>
      Подати заявку
    </Button>
  );
}

export default function AllTeamsPage() {
  usePageMeta("Усі команди", [{ label: "Команди", to: "/collaborations" }]);
  const { isAdministrator, isAuthenticated } = useAuth();
  const { confirm } = useDialogs();
  const toast = useToast();
  const [category, setCategory] = useState<string | null>(null);
  const [status, setStatus] = useState<StatusFilter>("all");
  const [teams, setTeams] = useState<TeamEntity[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [page, setPage] = useState(1);

  const load = useCallback(() => {
    setLoading(true);
    setError(null);
    teamsApi
      .getPaged(1, 50)
      .then(res => setTeams(res.items ?? (res as unknown as TeamEntity[])))
      .catch((e: Error) => setError(e.message))
      .finally(() => setLoading(false));
  }, []);

  useEffect(load, [load]);

  const categories = useMemo(() => [...new Set(teams.map(t => t.category).filter(Boolean))].sort((a, b) => a.localeCompare(b, "uk")), [teams]);

  async function deleteTeam(team: TeamEntity) {
    const ok = await confirm({ title: `Видалити «${team.name}»?`, message: "Команду буде видалено назавжди разом з її даними.", confirmLabel: "Видалити", danger: true });
    if (!ok) return;
    try {
      await teamsApi.deleteTeam(team.id);
      setTeams(prev => prev.filter(t => t.id !== team.id));
      toast.success("Команду видалено");
    } catch (e) {
      toast.fromError(e, "Помилка видалення");
    }
  }

  const filtered = teams.filter(t => (!category || t.category === category) && (status === "all" || teamStatusLabel(t.status) === status));
  const totalPages = Math.max(1, Math.ceil(filtered.length / PAGE_SIZE));
  const pageTeams = filtered.slice((page - 1) * PAGE_SIZE, page * PAGE_SIZE);

  return (
    <Page>
      <PageHeader
        title="Усі команди"
        description="Команди платформи — приєднуйтесь до тих, що шукають учасників."
        actions={
          isAuthenticated && (
            <ButtonLink to="/team/new" variant="primary" icon={<IconPlus size={16} />}>
              Нова команда
            </ButtonLink>
          )
        }
      />

      <div className={sh.toolbar}>
        <div className={sh.chips} role="group" aria-label="Категорія">
          <Tag active={category === null} onClick={() => { setCategory(null); setPage(1); }}>
            усі категорії
          </Tag>
          {categories.map(c => (
            <Tag key={c} active={category === c} onClick={() => { setCategory(c); setPage(1); }}>
              {c}
            </Tag>
          ))}
        </div>
        <Segmented
          label="Статус"
          value={status}
          onChange={v => { setStatus(v); setPage(1); }}
          items={[
            { value: "all", label: "Усі" },
            { value: "У пошуку", label: "У пошуку" },
            { value: "Активна", label: "Активні" },
            { value: "Завершена", label: "Завершені" },
          ]}
        />
      </div>

      {loading && <Loading />}
      {!loading && error && <ErrorState message={error} onRetry={load} />}
      {!loading && !error && filtered.length === 0 && <EmptyState icon={<IconUsers />} title="Команд не знайдено" text="Змініть фільтри або створіть власну команду." />}
      {!loading && !error && pageTeams.length > 0 && (
        <>
          <ul className={sh.list}>
            {pageTeams.map(team => (
              <li key={team.id}>
                <TeamRow
                  team={team}
                  actions={
                    <>
                      <ApplyButton team={team} />
                      {isAdministrator && (
                        <Menu items={[{ label: "Видалити команду", icon: <IconTrash size={16} />, danger: true, onSelect: () => void deleteTeam(team) }]} />
                      )}
                    </>
                  }
                />
              </li>
            ))}
          </ul>
          <Pagination page={page} totalPages={totalPages} onChange={setPage} />
        </>
      )}
    </Page>
  );
}
