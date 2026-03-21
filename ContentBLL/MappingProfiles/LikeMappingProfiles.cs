using AutoMapper;
using ContentBLL.DTO.Like;
using ContentDomain.Entity;

namespace ContentBLL.MappingProfiles;

public class LikeMappingProfiles: Profile
{
    public LikeMappingProfiles()
    {
        CreateMap<Like, LikeDto>();
 
        CreateMap<LikeCreateDto, Like>()
            .ForMember(dest => dest.LikeId,    opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.Post,      opt => opt.Ignore());
    }
}