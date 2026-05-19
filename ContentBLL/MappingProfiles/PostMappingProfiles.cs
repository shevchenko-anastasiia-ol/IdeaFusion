using AutoMapper;
using ContentBLL.DTO.Post;
using ContentDomain.Entity;

namespace ContentBLL.MappingProfiles;

public class PostMappingProfiles : Profile
{
    private static PostStatus ParseStatus(string? status) =>
        Enum.TryParse<PostStatus>(status, ignoreCase: true, out var result) ? result : PostStatus.Published;

    public PostMappingProfiles()
    {
        // Мапінг автора
        CreateMap<PostAuthor, AuthorDto>();

        // Мапінг колаборації
        CreateMap<CollaborationSnapshot, CollaborationDto>();

        // Мапінг посту в PostDto
        CreateMap<Post, PostDto>()
            .ForMember(dest => dest.Status, 
                       opt => opt.MapFrom(src => src.Status.ToString()))
            .ForMember(dest => dest.Tags,   
                       opt => opt.MapFrom(src => src.PostTags.Select(pt => pt.Tag.Name).ToList()))
            .ForMember(dest => dest.MediaUrls, 
                       opt => opt.MapFrom(src => src.Media.Select<PostMedia, string>(m => m.ObjectName).ToList()));

        // Мапінг PostCreateDto в Post
        CreateMap<PostCreateDto, Post>()
            .ForMember(dest => dest.PostId,                  opt => opt.Ignore())
            .ForMember(dest => dest.Status,                  opt => opt.MapFrom(src => ParseStatus(src.Status)))
            .ForMember(dest => dest.CreatedAt,               opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedAt,               opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedBy,               opt => opt.Ignore())
            .ForMember(dest => dest.IsDeleted,               opt => opt.Ignore())
            .ForMember(dest => dest.Author,                  opt => opt.Ignore())
            .ForMember(dest => dest.Collaboration,           opt => opt.Ignore())
            .ForMember(dest => dest.PostTags,                opt => opt.Ignore())
            .ForMember(dest => dest.Comments,                opt => opt.Ignore())
            .ForMember(dest => dest.Likes,                   opt => opt.Ignore())
            .ForMember(dest => dest.Views,                   opt => opt.Ignore())
            .ForMember(dest => dest.SavedPosts,              opt => opt.Ignore())
            .ForMember(dest => dest.Media,                   opt => opt.Ignore()); // Media поки що окремо додається

        // Мапінг PostUpdateDto в Post
        CreateMap<PostUpdateDto, Post>()
            .ForMember(dest => dest.PostId,                  opt => opt.Ignore())
            .ForMember(dest => dest.PostAuthorId,            opt => opt.Ignore())
            .ForMember(dest => dest.CollaborationSnapshotId, opt => opt.Ignore())
            .ForMember(dest => dest.Status,                  opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt,               opt => opt.Ignore())
            .ForMember(dest => dest.CreatedBy,               opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedAt,               opt => opt.Ignore())
            .ForMember(dest => dest.IsDeleted,               opt => opt.Ignore())
            .ForMember(dest => dest.Author,                  opt => opt.Ignore())
            .ForMember(dest => dest.Collaboration,           opt => opt.Ignore())
            .ForMember(dest => dest.PostTags,                opt => opt.Ignore())
            .ForMember(dest => dest.Comments,                opt => opt.Ignore())
            .ForMember(dest => dest.Likes,                   opt => opt.Ignore())
            .ForMember(dest => dest.Views,                   opt => opt.Ignore())
            .ForMember(dest => dest.SavedPosts,              opt => opt.Ignore())
            .ForMember(dest => dest.Media,                   opt => opt.Ignore()); // Media окремо
    }
}