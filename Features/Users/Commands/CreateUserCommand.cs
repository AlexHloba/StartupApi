using StartupApi.DTOs;
using StartupApi.Services;
using AutoMapper;
using MediatR;

namespace StartupApi.Features.Users.Commands;

public class CreateUserCommand : IRequest<UserDto>
{
    public CreateUserDto CreateUserDto { get; set; } = new();
}

public class CreateUserCommandHandler : IRequestHandler<CreateUserCommand, UserDto>
{
    private readonly IAuthService _authService;
    private readonly IMapper _mapper;

    public CreateUserCommandHandler(IAuthService authService, IMapper mapper)
    {
        _authService = authService;
        _mapper = mapper;
    }

    public async Task<UserDto> Handle(CreateUserCommand request, CancellationToken cancellationToken)
    {
        var user = await _authService.RegisterAsync(request.CreateUserDto);
        return _mapper.Map<UserDto>(user);
    }
}