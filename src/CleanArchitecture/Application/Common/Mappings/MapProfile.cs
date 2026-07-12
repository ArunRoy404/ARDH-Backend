using AutoMapper;
using CleanArchitecture.Domain.Entities;
using CleanArchitecture.Shared.Models.User;

namespace CleanArchitecture.Application.Common.Mappings;

public class MapProfile : Profile
{
    public MapProfile()
    {
        CreateMap<User, UserSignInResponse>().ReverseMap();
        CreateMap<User, UserProfileResponse>().ReverseMap();
    }
}
