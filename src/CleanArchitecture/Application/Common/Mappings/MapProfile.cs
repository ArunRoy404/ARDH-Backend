using AutoMapper;
using CleanArchitecture.Domain.Entities;
using CleanArchitecture.Shared.Models.User;

namespace CleanArchitecture.Application.Common.Mappings;

public class MapProfile : Profile
{
    public MapProfile()
    {
        CreateMap<User, UserSignInResponse>().ReverseMap();
        CreateMap<User, UserProfileResponse>()
            .ForMember(dest => dest.Address, opt => opt.MapFrom(src => src.Address ?? string.Empty))
            .ForMember(dest => dest.AvatarUrl, opt => opt.MapFrom(src => src.AvatarUrl ?? string.Empty))
            .ForMember(dest => dest.Permissions, opt => opt.MapFrom(src => src.Permissions ?? string.Empty))
            .ReverseMap();
    }
}
