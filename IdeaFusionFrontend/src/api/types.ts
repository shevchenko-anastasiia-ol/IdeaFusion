// ── Auth ──────────────────────────────────────────────────────────────────
export interface LoginDto { email: string; password: string; }
export interface RegisterDto { userName: string; email: string; password: string; }
export interface AuthResponseDto { accessToken: string; refreshToken: string; }
export interface UserDto { id: string; email: string; userName?: string; avatarUrl?: string; fullName?: string; specialization?: string; roles: string[]; createdAt: string; }
export interface UpdateProfileDto { fullName?: string; specialization?: string; }
export interface ChangePasswordDto { currentPassword: string; newPassword: string; }
export interface TokenRequestDto { refreshToken: string; }

// ── Content ───────────────────────────────────────────────────────────────
export interface AuthorDto {
  postAuthorId: number;
  userId: number;
  userName: string;
  avatarUrl?: string;
}

export interface PostDto {
  postId: number;
  title: string;
  description?: string;
  externalLink?: string;
  status: string;
  author?: AuthorDto;
  collaboration?: { collaborationSnapshotId: number; collaborationId: number; name: string; avatarUrl?: string };
  tags: string[];
  mediaUrls: string[];
  likesCount: number;
  viewsCount: number;
  commentsCount: number;
  createdAt: string;
  createdBy?: string;
}

export interface CommentDto {
  commentId: number;
  postId: number;
  parentCommentId?: number;
  body: string;
  author: AuthorDto;
  createdAt: string;
  createdBy?: string;
}

export interface CommentCreateDto {
  postId: number;
  postAuthorId: number;
  parentCommentId?: number;
  body: string;
  createdBy: string;
}

export interface TagDto { tagId: number; name: string; }

export interface LikeCreateDto { postId: number; userId: number; }

// ── Aggregator ────────────────────────────────────────────────────────────
export interface AggregatorAuthorDto {
  postAuthorId: number;
  userId: number;
  userName: string;
  avatarUrl?: string;
}

export interface AggregatorCollaborationDto {
  collaborationSnapshotId: number;
  collaborationId: number;
  name: string;
  avatarUrl?: string;
  externalId?: string;
}

export interface AggregatorPostDto {
  postId: number;
  title: string;
  description?: string;
  externalLink?: string;
  status: string;
  author?: AggregatorAuthorDto;
  collaboration?: AggregatorCollaborationDto;
  tags: string[];
  mediaUrls: string[];
  likesCount: number;
  commentsCount: number;
  isLikedByCurrentUser: boolean;
  isSavedByCurrentUser: boolean;
  createdAt: string;
  createdBy?: string;
}

export interface PostWithEngagementDto {
  post: AggregatorPostDto;
  likesCount: number;
  commentsCount: number;
  viewsCount: number;
  savedCount: number;
  isLikedByCurrentUser: boolean;
  isSavedByCurrentUser: boolean;
}

export interface AggregatorCommentDto {
  commentId: number;
  postId: number;
  parentCommentId?: number;
  body: string;
  author: AggregatorAuthorDto;
  replies: AggregatorCommentDto[];
  createdAt: string;
}

export interface AggregatorTeamMemberDto {
  userId: string;
  username: string;
  avatarUrl?: string;
  role: string;
}

export interface AggregatorRequiredRoleDto { role: string; description?: string; }

export interface AggregatorTeamDto {
  teamId: string;
  name: string;
  description: string;
  category: string;
  // status comes as int (0=Active,1=Searching,2=Completed) or string
  status: number | string;
  tags: string[];
  members: AggregatorTeamMemberDto[];
  requiredRoles: AggregatorRequiredRoleDto[];
  membersCount?: number;
}

export interface AggregatorTeamSummaryDto {
  teamId: string;
  name: string;
  category: string;
  status: number | string;
  membersCount: number;
}

export interface PostFullDetailsDto {
  post: AggregatorPostDto;
  comments: AggregatorCommentDto[];
  team?: AggregatorTeamDto;
  likesCount: number;
  commentsCount: number;
  viewsCount: number;
  savedCount: number;
  isLikedByCurrentUser: boolean;
  isSavedByCurrentUser: boolean;
}

export interface PostUpdateFrontendDto {
  title: string;
  description?: string;
  externalLink?: string;
  tagIds: number[];
  updatedBy: string;
  newMediaFiles?: File[];
}

export interface AggregatorUserSnapshotDto {
  userId: string;
  username: string;
  avatarUrl?: string;
}

export interface AggregatorTeamPostDto {
  id: string;
  postId: string;
  teamId: string;
  author: AggregatorUserSnapshotDto;
  title: string;
  publishedAt: string;
  createdAt: string;
  mediaUrls: string[];
  tags: string[];
}

export interface TeamWithPostsDto {
  team: AggregatorTeamDto;
  posts: AggregatorTeamPostDto[];
  collaborationRequests: AggregatorCollaborationRequestDto[];
  totalPosts: number;
  openRolesCount: number;
  pendingRequestsCount: number;
}

export interface AggregatorCollaborationRequestDto {
  requestId: string;
  teamId: string;
  teamName: string;
  fromUserId: string;
  fromUsername: string;
  fromUserAvatarUrl?: string;
  toUserId?: string;
  role: string;
  message?: string;
  status: string;
  createdAt: string;
}

export interface AggregatorGroupInvitationDto {
  invitationId: string;
  teamId: string;
  teamName: string;
  invitedUserId: string;
  invitedByUserId: string;
  invitedByUsername: string;
  role: string;
  message?: string;
  status: string;
  expiresAt: string;
  createdAt: string;
}

export interface AggregatorSavedPostDto {
  savedPostId: number;
  userId: number;
  savedAt: string;
  postId: number;
  postTitle: string;
  postMediaUrl?: string;
}

export interface UserDashboardDto {
  userId: number;
  myPosts: AggregatorPostDto[];
  savedPosts: AggregatorSavedPostDto[];
  myTeams: AggregatorTeamSummaryDto[];
  pendingRequests: AggregatorCollaborationRequestDto[];
  pendingInvitations: AggregatorGroupInvitationDto[];
  totalPosts: number;
  totalSavedPosts: number;
  totalTeams: number;
}

// ── Collaboration (direct entity shape from API) ──────────────────────────
export const TEAM_PERMISSIONS: { key: string; label: string }[] = [
  { key: "canPublishPosts", label: "Публікувати пости" },
  { key: "canEditPosts", label: "Редагувати та видаляти пости" },
  { key: "canInviteMembers", label: "Запрошувати учасників" },
  { key: "canRemoveMembers", label: "Видаляти учасників" },
  { key: "canEditTeam", label: "Змінювати налаштування команди" },
];

export interface TeamMemberEntity {
  user: { userId: string; username: string; avatarUrl?: string };
  role: string;
  joinedAt: string;
  permissions?: string[];
}

export interface RequiredRoleEntity { role: string; description?: string; }

export function hasPermission(team: TeamEntity | null | undefined, userId: string | undefined, perm: string): boolean {
  if (!team || !userId) return false;
  const member = team.members.find(m => m.user.userId === userId);
  if (!member) return false;
  if (member.role === "Owner") return true;
  const perms = member.permissions ?? [];
  if (perms.length === 0) return perm === "canPublishPosts";
  return perms.includes(perm);
}

export function isTeamOwner(team: TeamEntity | null | undefined, userId: string | undefined): boolean {
  return !!team?.members.find(m => m.user.userId === userId && m.role === "Owner");
}

export interface TeamEntity {
  id: string;
  name: string;
  description: string;
  category: string;
  tags: string[];
  // 0=Active,1=Searching,2=Completed  (or string "Active"/"Searching"/"Completed")
  status: number | string;
  avatarUrl?: string;
  members: TeamMemberEntity[];
  requiredRoles: RequiredRoleEntity[];
  openVacancyCount: number;
  createdAt: string;
  updatedAt?: string;
}

export interface PagedResult<T> {
  items: T[];
  totalCount: number;
  pageNumber: number;
  pageSize: number;
  hasNextPage: boolean;
}

export interface CollaborationRequestEntity {
  id: string;
  teamId: string;
  fromUserId: string;
  fromUsername?: string;
  fromAvatarUrl?: string;
  toUserId?: string;
  role: string;
  message?: string;
  // 0=Pending,1=Accepted,2=Rejected,3=Cancelled
  status: number | string;
  resolvedAt?: string;
  createdAt: string;
}

export interface GroupInvitationEntity {
  id: string;
  teamId: string;
  invitedUserId: string;
  invitedByUserId: string;
  role: string;
  message?: string;
  // 0=Pending,1=Accepted,2=Declined,3=Expired,4=Revoked
  status: number | string;
  expiresAt: string;
  resolvedAt?: string;
  createdAt: string;
}

// ── Commands ──────────────────────────────────────────────────────────────
export interface UpdateTeamCommand {
  teamId: string;
  name: string;
  description: string;
  category: string;
  tags: string[];
  userId: string;
}

export interface SetTeamStatusCommand {
  teamId: string;
  status: number;
  userId: string;
}

export interface AddRequiredRoleCommand {
  teamId: string;
  role: string;
  description?: string;
  userId: string;
}

export interface SetTeamAvatarUrlCommand {
  teamId: string;
  avatarUrl?: string;
  userId: string;
}

export interface UpdateTeamMemberPermissionsCommand {
  teamId: string;
  targetMemberId: string;
  permissions: string[];
  userId: string;
}

export interface CreateCollaborationRequestCommand {
  teamId: string;
  fromUserId: string;
  fromUsername?: string;
  fromAvatarUrl?: string;
  role: string;
  message?: string;
  toUserId?: string;
}

export interface AcceptCollaborationRequestCommand { requestId: string; userId: string; }
export interface RejectCollaborationRequestCommand { requestId: string; userId: string; }

export interface CreateGroupInvitationCommand {
  teamId: string;
  invitedUserId: string;
  invitedByUserId: string;
  role: string;
  message?: string;
  expirationDays?: number;
}

export interface AcceptGroupInvitationCommand { invitationId: string; userId: string; username: string; avatarUrl?: string; }
export interface DeclineGroupInvitationCommand { invitationId: string; userId: string; }

export interface CreateTeamCommand {
  name: string;
  description: string;
  category: string;
  tags: string[];
  userId: string;
  username: string;
  avatarUrl?: string;
  teamAvatarUrl?: string;
}

// ── Chat ──────────────────────────────────────────────────────────────────
export interface ChatMessageDto {
  id: string;
  chatId: string;
  senderId: string;
  senderName: string;
  senderAvatarUrl?: string;
  text: string;
  sentAt: string;
}

export interface ChatSummaryDto {
  chatId: string;
  teamId: string;
  teamName: string;
  teamAvatarUrl?: string;
  memberCount: number;
  lastMessage?: string;
  lastSenderName?: string;
  lastMessageAt?: string;
  createdAt?: string;
}

export interface ChatHistoryPage {
  items: ChatMessageDto[];
  hasMore: boolean;
  nextCursor?: string;
}

export interface SendChatMessageCommand {
  chatId: string;
  senderId: string;
  senderName: string;
  senderAvatarUrl?: string;
  text: string;
}

// ── Team vacancies ────────────────────────────────────────────────────────
export interface TeamVacancyEntity {
  id: string;
  teamId: string;
  teamName: string;
  teamAvatarUrl?: string;
  category: string;
  tags: string[];
  role: string;
  description?: string;
  requirements?: string;
  // "Open" | "Closed"  (or numeric 0|1)
  status: number | string;
  createdAt: string;
  updatedAt?: string;
}

export interface VacancyApplicationEntity {
  id: string;
  vacancyId: string;
  teamId: string;
  teamName: string;
  applicantUserId: string;
  applicantUsername: string;
  applicantAvatarUrl?: string;
  role: string;
  message?: string;
  // "Pending" | "Accepted" | "Rejected" | "Cancelled"  (or numeric 0..3)
  status: number | string;
  resolvedAt?: string;
  createdAt: string;
}

export interface VacancyPage {
  items: TeamVacancyEntity[];
  hasMore: boolean;
  totalCount: number;
  pageNumber: number;
  pageSize: number;
}

export interface CreateTeamVacancyCommand {
  teamId: string;
  userId: string;
  role: string;
  description?: string;
  requirements?: string;
}

export interface CloseTeamVacancyCommand { vacancyId: string; userId: string; }
export interface ApplyToVacancyCommand { vacancyId: string; userId: string; message?: string; }
export interface AcceptVacancyApplicationCommand { applicationId: string; userId: string; }
export interface RejectVacancyApplicationCommand { applicationId: string; userId: string; }
export interface CancelVacancyApplicationCommand { applicationId: string; userId: string; }

// ── Helpers ───────────────────────────────────────────────────────────────
export function teamStatusLabel(status: number | string): string {
  const n = typeof status === 'string' ? parseInt(status, 10) : status;
  if (n === 0 || status === 'Active')    return 'Активна';
  if (n === 1 || status === 'Searching') return 'У пошуку';
  if (n === 2 || status === 'Completed') return 'Завершена';
  return String(status);
}

export function vacancyStatusKey(status: number | string): 'open' | 'closed' {
  const n = typeof status === 'string' ? parseInt(status, 10) : status;
  if (n === 0 || status === 'Open')  return 'open';
  return 'closed';
}

export function vacancyStatusLabel(status: number | string): string {
  return vacancyStatusKey(status) === 'open' ? 'Відкрита' : 'Закрита';
}

export function applicationStatusKey(status: number | string): 'pending' | 'accepted' | 'rejected' | 'cancelled' {
  const n = typeof status === 'string' ? parseInt(status, 10) : status;
  if (n === 0 || status === 'Pending')   return 'pending';
  if (n === 1 || status === 'Accepted')  return 'accepted';
  if (n === 2 || status === 'Rejected')  return 'rejected';
  return 'cancelled';
}

export function applicationStatusLabel(status: number | string): string {
  switch (applicationStatusKey(status)) {
    case 'pending':   return 'Очікує розгляду';
    case 'accepted':  return 'Прийнято';
    case 'rejected':  return 'Відхилено';
    case 'cancelled': return 'Скасовано';
  }
}

export function requestStatusKey(status: number | string): 'new' | 'accepted' | 'rejected' | 'cancelled' {
  const n = typeof status === 'string' ? parseInt(status, 10) : status;
  if (n === 0 || status === 'Pending')   return 'new';
  if (n === 1 || status === 'Accepted')  return 'accepted';
  if (n === 2 || status === 'Rejected')  return 'rejected';
  return 'cancelled';
}

export function invitationStatusKey(status: number | string): 'new' | 'accepted' | 'rejected' {
  const n = typeof status === 'string' ? parseInt(status, 10) : status;
  if (n === 0 || status === 'Pending')  return 'new';
  if (n === 1 || status === 'Accepted') return 'accepted';
  return 'rejected';
}

export function timeAgo(isoDate: string): string {
  const diff = Date.now() - new Date(isoDate).getTime();
  const mins = Math.floor(diff / 60000);
  if (mins < 1)   return 'щойно';
  if (mins < 60)  return `${mins} хв. тому`;
  const hrs = Math.floor(mins / 60);
  if (hrs < 24)   return `${hrs} год. тому`;
  const days = Math.floor(hrs / 24);
  if (days < 7)   return `${days} ${days === 1 ? 'день' : 'днів'} тому`;
  return new Date(isoDate).toLocaleDateString('uk-UA');
}

export function initials(name: string): string {
  return name.split(' ').map(w => w[0]).join('').toUpperCase().slice(0, 2);
}