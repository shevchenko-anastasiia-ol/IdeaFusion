import { api } from "./client";
import type {
  AcceptVacancyApplicationCommand,
  ApplyToVacancyCommand,
  CancelVacancyApplicationCommand,
  CloseTeamVacancyCommand,
  CreateTeamVacancyCommand,
  UpdateTeamVacancyCommand,
  RejectVacancyApplicationCommand,
  TeamVacancyEntity,
  VacancyApplicationEntity,
  VacancyPage,
} from "./types";

const BASE = "/api/vacancies";

export interface SearchVacanciesParams {
  category?: string;
  role?: string;
  teamId?: string;
  openOnly?: boolean;
  pageNumber?: number;
  pageSize?: number;
}

export const vacanciesApi = {
  async getPaged(params: SearchVacanciesParams = {}): Promise<VacancyPage> {
    const q = new URLSearchParams();
    if (params.category) q.set("category", params.category);
    if (params.role) q.set("role", params.role);
    if (params.teamId) q.set("teamId", params.teamId);
    q.set("openOnly", String(params.openOnly ?? true));
    q.set("pageNumber", String(params.pageNumber ?? 1));
    q.set("pageSize", String(params.pageSize ?? 20));
    return api.get<VacancyPage>(`${BASE}?${q.toString()}`);
  },

  async getByTeam(teamId: string, pageNumber = 1, pageSize = 20): Promise<VacancyPage> {
    return api.get<VacancyPage>(`${BASE}/team/${teamId}?pageNumber=${pageNumber}&pageSize=${pageSize}`);
  },

  async getById(id: string): Promise<TeamVacancyEntity> {
    return api.get<TeamVacancyEntity>(`${BASE}/${id}`);
  },

  async create(command: CreateTeamVacancyCommand): Promise<TeamVacancyEntity> {
    return api.post<TeamVacancyEntity>(BASE, command);
  },

  async update(command: UpdateTeamVacancyCommand): Promise<TeamVacancyEntity> {
    return api.put<TeamVacancyEntity>(`${BASE}/${command.vacancyId}`, command);
  },

  async close(command: CloseTeamVacancyCommand): Promise<TeamVacancyEntity> {
    return api.post<TeamVacancyEntity>(`${BASE}/${command.vacancyId}/close`, command);
  },

  async apply(command: ApplyToVacancyCommand): Promise<VacancyApplicationEntity> {
    return api.post<VacancyApplicationEntity>(`${BASE}/${command.vacancyId}/apply`, command);
  },

  // The server checks the caller's review privilege from the token.
  async getApplications(vacancyId: string): Promise<VacancyApplicationEntity[]> {
    return api.get<VacancyApplicationEntity[]>(`${BASE}/${vacancyId}/applications`);
  },

  async accept(command: AcceptVacancyApplicationCommand): Promise<VacancyApplicationEntity> {
    return api.post<VacancyApplicationEntity>(`${BASE}/applications/${command.applicationId}/accept`, command);
  },

  async reject(command: RejectVacancyApplicationCommand): Promise<VacancyApplicationEntity> {
    return api.post<VacancyApplicationEntity>(`${BASE}/applications/${command.applicationId}/reject`, command);
  },

  async cancel(command: CancelVacancyApplicationCommand): Promise<void> {
    return api.post<void>(`${BASE}/applications/${command.applicationId}/cancel`, command);
  },

  // The applicant's own list: the server answers for the token's subject.
  async getMyApplications(): Promise<VacancyApplicationEntity[]> {
    return api.get<VacancyApplicationEntity[]>(`${BASE}/applications/mine`);
  },
};