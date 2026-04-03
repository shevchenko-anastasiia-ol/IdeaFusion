using AutoMapper;
using ContentBLL.DTO.SavedPost;
using ContentDomain.Entity;
using System.Linq;

namespace ContentBLL.MappingProfiles;

public class SavedPostMappingProfiles : Profile
{
    public SavedPostMappingProfiles()
    {
        // Мапінг SavedPost -> SavedPostDto
        CreateMap<SavedPost, SavedPostDto>()
            .ForMember(dest => dest.PostTitle, 
                opt => opt.MapFrom(src => src.Post.Title))
            // Дістаємо перший медіафайл, якщо він є, і беремо ObjectName
            .ForMember(dest => dest.PostMediaUrl, 
                opt => opt.MapFrom(src => src.Post.Media.Any() 
                    ? src.Post.Media.First().ObjectName 
                    : null));

        // Мапінг SavedPostCreateDto -> SavedPost
        CreateMap<SavedPostCreateDto, SavedPost>()
            .ForMember(dest => dest.SavedPostId, opt => opt.Ignore())
            .ForMember(dest => dest.SavedAt,     opt => opt.Ignore())
            .ForMember(dest => dest.Post,        opt => opt.Ignore());
    }
}