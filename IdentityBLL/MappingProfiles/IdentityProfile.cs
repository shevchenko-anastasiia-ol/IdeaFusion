using AutoMapper;
using IdentityBLL.DTOs;
using IdentityServiceDomain.Entities;

namespace IdentityBLL.MappingProfiles;

public class IdentityProfile : Profile
{
    public IdentityProfile()
    {
        CreateMap<ApplicationUser, UserDto>()
            // Roles are no longer stored in the Identity role table: they are read
            // from the Keycloak token by the caller and filled in afterwards.
            .ForMember(dest => dest.Roles, opt => opt.Ignore());
    }
}
