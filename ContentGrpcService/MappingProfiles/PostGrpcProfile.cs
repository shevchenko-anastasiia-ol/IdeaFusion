using AutoMapper;
using ContentBLL.DTO.Post;
using GrpcPost = IdeaFusion.Grpc.Posts;

namespace GrpcService.MappingProfiles;

public class PostGrpcProfile : Profile
{
    public PostGrpcProfile()
    {
        CreateMap<PostDto, GrpcPost.PostDto>()
            .ForMember(dest => dest.PostId,       opt => opt.MapFrom(src => src.PostId.ToString()))
            .ForMember(dest => dest.Title,         opt => opt.MapFrom(src => src.Title))
            .ForMember(dest => dest.Description,   opt => opt.MapFrom(src => src.Description ?? string.Empty))
            .ForMember(dest => dest.ExternalLink,  opt => opt.MapFrom(src => src.ExternalLink ?? string.Empty))
            .ForMember(dest => dest.Status,        opt => opt.MapFrom(src => src.Status))
            .ForMember(dest => dest.Author,        opt => opt.MapFrom(src => src.Author))
            .ForMember(dest => dest.Team,          opt => opt.MapFrom(src => src.Collaboration))
            .ForMember(dest => dest.Tags,          opt => opt.MapFrom(src => src.Tags))
            // MediaUrls — це просто список рядків, мапимо в MediaDto з ObjectName
            .ForMember(dest => dest.Media,         opt => opt.MapFrom(src =>
                src.MediaUrls.Select(url => new GrpcPost.MediaDto { ObjectName = url }).ToList()))
            // LikesCount, ViewsCount, CommentsCount відсутні в BLL PostDto — залишаємо 0
            .ForMember(dest => dest.LikesCount,    opt => opt.Ignore())
            .ForMember(dest => dest.ViewsCount,    opt => opt.Ignore())
            .ForMember(dest => dest.CommentsCount, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt,     opt => opt.MapFrom(src => src.CreatedAt.ToString("O")))
            .ForMember(dest => dest.UpdatedAt,     opt => opt.Ignore());
 
        // AuthorDto → PostAuthorDto
        CreateMap<AuthorDto, GrpcPost.PostAuthorDto>()
            .ForMember(dest => dest.UserId,    opt => opt.MapFrom(src => src.UserId.ToString()))
            .ForMember(dest => dest.UserName,  opt => opt.MapFrom(src => src.UserName))
            .ForMember(dest => dest.AvatarUrl, opt => opt.MapFrom(src => src.AvatarUrl ?? string.Empty));
 
        // CollaborationDto → PostTeamDto
        CreateMap<CollaborationDto, GrpcPost.PostTeamDto>()
            .ForMember(dest => dest.TeamId,    opt => opt.MapFrom(src => src.CollaborationId.ToString()))
            .ForMember(dest => dest.Name,      opt => opt.MapFrom(src => src.Name))
            .ForMember(dest => dest.AvatarUrl, opt => opt.MapFrom(src => src.AvatarUrl ?? string.Empty));
    }
}