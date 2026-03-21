using AutoMapper;
using ContentBLL.DTO.Comment;
using ContentBLL.DTO.Post;
using ContentDomain.Entity;

namespace ContentBLL.MappingProfiles;

public class CommentMappingProfiles : Profile
{
    public CommentMappingProfiles()
    {
        CreateMap<PostAuthor, AuthorDto>();
 
        CreateMap<Comment, CommentDto>();
 
        CreateMap<CommentCreateDto, Comment>()
            .ForMember(dest => dest.CommentId,  opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt,  opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedAt,  opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedBy,  opt => opt.Ignore())
            .ForMember(dest => dest.IsDeleted,  opt => opt.Ignore())
            .ForMember(dest => dest.Post,       opt => opt.Ignore())
            .ForMember(dest => dest.Author,     opt => opt.Ignore())
            .ForMember(dest => dest.ParentComment, opt => opt.Ignore())
            .ForMember(dest => dest.Replies,    opt => opt.Ignore());
 
        CreateMap<CommentUpdateDto, Comment>()
            .ForMember(dest => dest.CommentId,     opt => opt.Ignore())
            .ForMember(dest => dest.PostId,        opt => opt.Ignore())
            .ForMember(dest => dest.PostAuthorId,  opt => opt.Ignore())
            .ForMember(dest => dest.ParentCommentId, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt,     opt => opt.Ignore())
            .ForMember(dest => dest.CreatedBy,     opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedAt,     opt => opt.Ignore())
            .ForMember(dest => dest.IsDeleted,     opt => opt.Ignore())
            .ForMember(dest => dest.Post,          opt => opt.Ignore())
            .ForMember(dest => dest.Author,        opt => opt.Ignore())
            .ForMember(dest => dest.ParentComment, opt => opt.Ignore())
            .ForMember(dest => dest.Replies,       opt => opt.Ignore());
    }
}