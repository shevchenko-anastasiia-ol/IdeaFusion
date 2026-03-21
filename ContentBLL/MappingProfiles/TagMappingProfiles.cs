using AutoMapper;
using ContentBLL.DTO.Tag;
using ContentDomain.Entity;

namespace ContentBLL.MappingProfiles;

public class TagMappingProfiles: Profile
{
    public TagMappingProfiles()
    {
        CreateMap<Tag, TagDto>();
 
        CreateMap<TagCreateDto, Tag>()
            .ForMember(dest => dest.TagId,    opt => opt.Ignore())
            .ForMember(dest => dest.PostTags, opt => opt.Ignore());
    }
}