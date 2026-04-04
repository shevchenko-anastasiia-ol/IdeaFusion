using AutoMapper;
using IdentityBLL.DTOs;
using IdentityServiceDomain.Entities;

namespace IdentityBLL.MappingProfiles;

public class IdentityProfile : Profile
{
    public IdentityProfile()
    {
        CreateMap<RegisterDto, ApplicationUser>()
            .ForMember(dest => dest.UserName, opt => opt.MapFrom(src => src.UserName))
            .ForMember(dest => dest.Email, opt => opt.MapFrom(src => src.Email));

        CreateMap<ApplicationUser, UserDto>()
            .ForMember(dest => dest.Roles, opt => opt.Ignore());

        CreateMap<RefreshToken, RefreshTokenDto>();
    }
}