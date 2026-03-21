using AutoMapper;
using ContentBLL.DTO.Post;
using ContentDomain.Entity;

namespace ContentBLL.MappingProfiles;

public class PostMappingProfiles : Profile
{
    public PostMappingProfiles()
    {
        CreateMap<PostAuthor, AuthorDto>();
 
        CreateMap<CollaborationSnapshot, CollaborationDto>();
 
        CreateMap<Post, PostDto>()
            .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Status.ToString()))
            .ForMember(dest => dest.Tags,   opt => opt.MapFrom(src => src.PostTags.Select(pt => pt.Tag.Name).ToList()));
 
        CreateMap<PostCreateDto, Post>()
            .ForMember(dest => dest.PostId,                  opt => opt.Ignore())
            .ForMember(dest => dest.Status,                  opt => opt.MapFrom(_ => PostStatus.Published))
            .ForMember(dest => dest.CreatedAt,               opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedAt,               opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedBy,               opt => opt.Ignore())
            .ForMember(dest => dest.IsDeleted,               opt => opt.Ignore())
            .ForMember(dest => dest.RowVer,                  opt => opt.Ignore())
            .ForMember(dest => dest.Author,                  opt => opt.Ignore())
            .ForMember(dest => dest.Collaboration,           opt => opt.Ignore())
            .ForMember(dest => dest.PostTags,                opt => opt.Ignore())
            .ForMember(dest => dest.Comments,                opt => opt.Ignore())
            .ForMember(dest => dest.Likes,                   opt => opt.Ignore())
            .ForMember(dest => dest.Views,                   opt => opt.Ignore())
            .ForMember(dest => dest.SavedPosts,              opt => opt.Ignore());
 
        CreateMap<PostUpdateDto, Post>()
            .ForMember(dest => dest.PostId,                  opt => opt.Ignore())
            .ForMember(dest => dest.PostAuthorId,            opt => opt.Ignore())
            .ForMember(dest => dest.CollaborationSnapshotId, opt => opt.Ignore())
            .ForMember(dest => dest.Status,                  opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt,               opt => opt.Ignore())
            .ForMember(dest => dest.CreatedBy,               opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedAt,               opt => opt.Ignore())
            .ForMember(dest => dest.IsDeleted,               opt => opt.Ignore())
            .ForMember(dest => dest.RowVer,                  opt => opt.Ignore())
            .ForMember(dest => dest.Author,                  opt => opt.Ignore())
            .ForMember(dest => dest.Collaboration,           opt => opt.Ignore())
            .ForMember(dest => dest.PostTags,                opt => opt.Ignore())
            .ForMember(dest => dest.Comments,                opt => opt.Ignore())
            .ForMember(dest => dest.Likes,                   opt => opt.Ignore())
            .ForMember(dest => dest.Views,                   opt => opt.Ignore())
            .ForMember(dest => dest.SavedPosts,              opt => opt.Ignore());
    }
}