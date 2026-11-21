import { useCallback, useEffect, useState } from "react";
import { Link } from "react-router-dom";
import { vacanciesApi } from "../api/vacancies";
import type { TeamVacancyEntity } from "../api/types";
import { timeAgo, vacancyStatusLabel } from "../api/types";
import { Page } from "../layout/AppShell";
import { usePageMeta } from "../layout/PageMeta";
import { Avatar, EmptyState, ErrorState, Loading, PageHeader, Pagination, Status, Switch, Tag } from "../ui";
import { IconBriefcase } from "../ui/icons";
import { vacancyTone } from "../lib/status";
import sh from "./shared.module.css";
import s from "./VacanciesPage.module.css";

const CATEGORIES = ["Дизайн", "Музика", "Арт", "Фото", "Анімація"];
const PAGE_SIZE = 8;

function VacancyRow({ vacancy }: { vacancy: TeamVacancyEntity }) {
  return (
    <Link to={`/vacancies/${vacancy.id}`} className={s.row}>
      <Avatar name={vacancy.teamName} src={vacancy.teamAvatarUrl} seed={vacancy.teamId} size={44} square />
      <div className={s.main}>
        <div className={s.titleLine}>
          <span className={s.role}>{vacancy.role}</span>
          <Status tone={vacancyTone(vacancy.status)}>{vacancyStatusLabel(vacancy.status).toLowerCase()}</Status>
        </div>
        <span className={s.team}>{vacancy.teamName}</span>
        {vacancy.description && <span className={s.desc}>{vacancy.description}</span>}
        <span className={s.meta}>
          {vacancy.category && <span>{vacancy.category}</span>}
          <span>{vacancy.requirements ? "є вимоги до кандидата" : "без обов'язкових вимог"}</span>
          <span>{timeAgo(vacancy.createdAt)}</span>
          {vacancy.tags.slice(0, 3).map(t => (
            <span key={t}>#{t}</span>
          ))}
        </span>
      </div>
    </Link>
  );
}

export default function VacanciesPage() {
  usePageMeta("Вакансії");
  const [category, setCategory] = useState<string | null>(null);
  const [openOnly, setOpenOnly] = useState(false);
  const [vacancies, setVacancies] = useState<TeamVacancyEntity[]>([]);
  const [hasMore, setHasMore] = useState(false);
  const [total, setTotal] = useState<number | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [page, setPage] = useState(1);

  const load = useCallback(() => {
    setLoading(true);
    setError(null);
    vacanciesApi
      .getPaged({ pageNumber: page, pageSize: PAGE_SIZE, openOnly, ...(category ? { category } : {}) })
      .then(res => {
        setVacancies(res.items);
        setHasMore(res.hasMore);
        setTotal(res.totalCount ?? null);
      })
      .catch((e: Error) => setError(e.message))
      .finally(() => setLoading(false));
  }, [category, page, openOnly]);

  useEffect(load, [load]);

  const totalPages = total != null ? Math.max(1, Math.ceil(total / PAGE_SIZE)) : undefined;

  return (
    <Page>
      <PageHeader title="Вакансії" description="Відкриті ролі в командах платформи. Відгукніться — і лідер команди отримає вашу заявку." />

      <div className={sh.toolbar}>
        <div className={sh.chips} role="group" aria-label="Категорія">
          <Tag active={category === null} onClick={() => { setCategory(null); setPage(1); }}>
            усі
          </Tag>
          {CATEGORIES.map(c => (
            <Tag key={c} active={category === c} onClick={() => { setCategory(c); setPage(1); }}>
              {c.toLowerCase()}
            </Tag>
          ))}
        </div>
        <Switch checked={openOnly} onChange={v => { setOpenOnly(v); setPage(1); }} label={<span className="mono" style={{ fontSize: 13 }}>лише відкриті</span>} />
      </div>

      {loading && <Loading />}
      {!loading && error && <ErrorState message={error} onRetry={load} />}
      {!loading && !error && vacancies.length === 0 && <EmptyState icon={<IconBriefcase />} title="Вакансій не знайдено" text="Спробуйте іншу категорію." />}
      {!loading && !error && vacancies.length > 0 && (
        <>
          <ul className={sh.list}>
            {vacancies.map(v => (
              <li key={v.id}>
                <VacancyRow vacancy={v} />
              </li>
            ))}
          </ul>
          <Pagination page={page} totalPages={totalPages} hasNext={hasMore} onChange={setPage} />
        </>
      )}
    </Page>
  );
}
