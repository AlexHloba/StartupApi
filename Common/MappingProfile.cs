using AutoMapper;
using StartupApi.DTOs;
using StartupApi.Models;

namespace StartupApi.Common;

public class MappingProfile : Profile
{
    public MappingProfile()
    {
        CreateMap<User, UserDto>();
        CreateMap<CreateUserDto, User>();
        CreateMap<UpdateUserDto, User>();
    }
}