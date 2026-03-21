using AutoMapper;
using ContentBLL.DTO.SavedPost;
using ContentDomain.Entity;

namespace ContentBLL.MappingProfiles;

public class SavedPostMappingProfiles: Profile
{
    public SavedPostMappingProfiles()
    {
        CreateMap<SavedPost, SavedPostDto>()
            .ForMember(dest => dest.PostTitle,    opt => opt.MapFrom(src => src.Post.Title))
            .ForMember(dest => dest.PostMediaUrl, opt => opt.MapFrom(src => src.Post.MediaUrl));
 
        CreateMap<SavedPostCreateDto, SavedPost>()
            .ForMember(dest => dest.SavedPostId, opt => opt.Ignore())
            .ForMember(dest => dest.SavedAt,     opt => opt.Ignore())
            .ForMember(dest => dest.Post,        opt => opt.Ignore());
    }
}