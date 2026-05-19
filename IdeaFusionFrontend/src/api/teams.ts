import { api } from './client';
import type {
  AcceptCollaborationRequestCommand,
  AcceptGroupInvitationCommand,
  AddRequiredRoleCommand,
  CollaborationRequestEntity,
  CreateCollaborationRequestCommand,
  CreateGroupInvitationCommand,
  CreateTeamCommand,
  DeclineGroupInvitationCommand,
  GroupInvitationEntity,
  PagedResult,
  RejectCollaborationRequestCommand,
  SetTeamAvatarUrlCommand,
  SetTeamStatusCommand,
  TeamEntity,
  UpdateTeamCommand,
} from './types';

export const teamsApi = {
  getPaged: (pageNumber = 1, pageSize = 50) =>
    api.get<PagedResult<TeamEntity>>(`/api/team?pageNumber=${pageNumber}&pageSize=${pageSize}`),

  getById: (id: string) =>
    api.get<TeamEntity>(`/api/team/${id}`),

  getByMember: (userId: string) =>
    api.get<TeamEntity[]>(`/api/team/member/${userId}`),

  search: (name: string) =>
    api.get<TeamEntity[]>(`/api/team/search?name=${encodeURIComponent(name)}`),

  create: (cmd: CreateTeamCommand) =>
    api.post<TeamEntity>('/api/team', cmd),

  update: (id: string, cmd: UpdateTeamCommand) =>
    api.put<TeamEntity>(`/api/team/${id}`, cmd),

  setStatus: (id: string, cmd: SetTeamStatusCommand) =>
    api.patch<TeamEntity>(`/api/team/${id}/status`, cmd),

  removeMember: (teamId: string, memberId: string, requestedByUserId: string) =>
    api.delete<TeamEntity>(`/api/team/${teamId}/members/${memberId}?requestedByUserId=${encodeURIComponent(requestedByUserId)}`),

  addRequiredRole: (id: string, cmd: AddRequiredRoleCommand) =>
    api.post<TeamEntity>(`/api/team/${id}/required-roles`, cmd),

  setAvatarUrl: (id: string, cmd: SetTeamAvatarUrlCommand) =>
    api.patch<TeamEntity>(`/api/team/${id}/avatar-url`, cmd),

  deleteTeam: (id: string) =>
    api.delete<void>(`/api/team/${id}`),
};

export const collaborationRequestsApi = {
  getByTeam: (teamId: string) =>
    api.get<CollaborationRequestEntity[]>(`/api/collaboration-requests/team/${teamId}`),

  getByUser: (userId: string) =>
    api.get<CollaborationRequestEntity[]>(`/api/collaboration-requests/user/${userId}`),

  create: (cmd: CreateCollaborationRequestCommand) =>
    api.post<CollaborationRequestEntity>('/api/collaboration-requests', cmd),

  accept: (id: string, cmd: AcceptCollaborationRequestCommand) =>
    api.patch<CollaborationRequestEntity>(`/api/collaboration-requests/${id}/accept`, cmd),

  reject: (id: string, cmd: RejectCollaborationRequestCommand) =>
    api.patch<CollaborationRequestEntity>(`/api/collaboration-requests/${id}/reject`, cmd),
};

export const groupInvitationsApi = {
  getByUser: (userId: string) =>
    api.get<GroupInvitationEntity[]>(`/api/group-invitations/user/${userId}`),

  getByTeam: (teamId: string) =>
    api.get<GroupInvitationEntity[]>(`/api/group-invitations/team/${teamId}`),

  create: (cmd: CreateGroupInvitationCommand) =>
    api.post<GroupInvitationEntity>('/api/group-invitations', cmd),

  accept: (id: string, cmd: AcceptGroupInvitationCommand) =>
    api.patch<GroupInvitationEntity>(`/api/group-invitations/${id}/accept`, cmd),

  decline: (id: string, cmd: DeclineGroupInvitationCommand) =>
    api.patch<GroupInvitationEntity>(`/api/group-invitations/${id}/decline`, cmd),
};