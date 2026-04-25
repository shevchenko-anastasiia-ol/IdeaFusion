using AutoMapper;
using Collaboration.Domain.Entities;
using Collaboration.Domain.ValueOfObjects;
using GrpcCollab = IdeaFusion.Grpc.CollaborationRequests;
using GrpcTeams = IdeaFusion.Grpc.Teams;

namespace CollaborationGrpcService.MappingProfiles;

public class CollaborationGrpcProfile : Profile
{
    public CollaborationGrpcProfile()
    {
        // CollaborationRequest → CollaborationRequestDto
        CreateMap<CollaborationRequest, GrpcCollab.CollaborationRequestDto>()
            .ForMember(dest => dest.RequestId,   opt => opt.MapFrom(src => src.Id.ToString()))
            .ForMember(dest => dest.TeamId,      opt => opt.MapFrom(src => src.TeamId))
            .ForMember(dest => dest.FromUserId,  opt => opt.MapFrom(src => src.FromUserId))
            .ForMember(dest => dest.ToUserId,    opt => opt.MapFrom(src => src.ToUserId ?? string.Empty))
            .ForMember(dest => dest.Role,        opt => opt.MapFrom(src => src.Role))
            .ForMember(dest => dest.Message,     opt => opt.MapFrom(src => src.Message ?? string.Empty))
            .ForMember(dest => dest.Status,      opt => opt.MapFrom(src => src.Status.ToString()))
            .ForMember(dest => dest.CreatedAt,   opt => opt.MapFrom(src => src.CreatedAt.ToString("O")))
            .ForMember(dest => dest.ResolvedAt,  opt => opt.MapFrom(src =>
                src.ResolvedAt.HasValue ? src.ResolvedAt.Value.ToString("O") : string.Empty));
 
        // Team → TeamDto
        CreateMap<Team, GrpcTeams.TeamDto>()
            .ForMember(dest => dest.TeamId,      opt => opt.MapFrom(src => src.Id.ToString()))
            .ForMember(dest => dest.Name,        opt => opt.MapFrom(src => src.Name))
            .ForMember(dest => dest.Description, opt => opt.MapFrom(src => src.Description ?? string.Empty))
            .ForMember(dest => dest.Category,    opt => opt.MapFrom(src => src.Category ?? string.Empty))
            .ForMember(dest => dest.Tags,        opt => opt.MapFrom(src => src.Tags))
            .ForMember(dest => dest.Status,      opt => opt.MapFrom(src => src.Status.ToString()))
            .ForMember(dest => dest.Members,     opt => opt.MapFrom(src => src.Members))
            .ForMember(dest => dest.RequiredRoles, opt => opt.MapFrom(src => src.RequiredRoles))
            .ForMember(dest => dest.MembersCount, opt => opt.MapFrom(src => src.Members.Count))
            .ForMember(dest => dest.CreatedAt,   opt => opt.MapFrom(src => src.CreatedAt.ToString("O")));
 
        // TeamMember (UserSnapshot) → MemberDto
        CreateMap<TeamMember, GrpcTeams.MemberDto>()
            .ForMember(dest => dest.UserId,   opt => opt.MapFrom(src => src.User.UserId))
            .ForMember(dest => dest.UserName, opt => opt.MapFrom(src => src.User.Username))
            .ForMember(dest => dest.AvatarUrl, opt => opt.MapFrom(src => src.User.AvatarUrl ?? string.Empty))
            .ForMember(dest => dest.Role,     opt => opt.MapFrom(src => src.Role))
            .ForMember(dest => dest.JoinedAt, opt => opt.MapFrom(src => src.JoinedAt.ToString("O")));
 
        // RequiredRole → RequiredRoleDto
        CreateMap<RequiredRole, GrpcTeams.RequiredRoleDto>()
            .ForMember(dest => dest.Role,        opt => opt.MapFrom(src => src.Role))
            .ForMember(dest => dest.Description, opt => opt.MapFrom(src => src.Description ?? string.Empty));
    }
}